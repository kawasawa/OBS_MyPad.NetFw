using MyLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Vanara.PInvoke;

namespace MyPad.ViewModels
{
    public class FileNodeViewModel : ViewModelBase
    {
        public static FileNodeViewModel Empty { get; } = new FileNodeViewModel() { IsEmpty = true };

        public bool IsEmpty { get; private set; }
        public string FileName { get; }
        public BitmapSource Image { get; }
        public FileNodeViewModel Parent { get; }
        public ObservableCollection<FileNodeViewModel> Children { get; } = new ObservableCollection<FileNodeViewModel>();

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => this._isExpanded;
            set
            {
                if (this.SetProperty(ref this._isExpanded, value) && value)
                    this.ExploreChildren();
            }
        }

        private FileNodeViewModel()
        {
        }

        public FileNodeViewModel(string fileName)
            : this(fileName, null)
        {
        }

        public FileNodeViewModel(string fileName, FileNodeViewModel parent)
        {
            this.FileName = fileName;
            this.Parent = parent;

            var psfi = new Shell32.SHFILEINFO();
            Shell32.SHGetFileInfo(
                fileName,
                Directory.Exists(fileName) ? FileAttributes.Directory : FileAttributes.Normal,
                ref psfi,
                Marshal.SizeOf(psfi),
                Shell32.SHGFI.SHGFI_ICON | Shell32.SHGFI.SHGFI_USEFILEATTRIBUTES | Shell32.SHGFI.SHGFI_SMALLICON);
            if (psfi.hIcon.IsNull == false)
                this.Image = Imaging.CreateBitmapSourceFromHIcon((IntPtr)psfi.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            if (Directory.Exists(fileName))
                this.Children.Add(Empty);
        }

        public void ExploreChildren()
        {
            bool nodeFilter(string path)
            {
                var attribute = File.GetAttributes(path);
                return attribute.HasFlag(FileAttributes.System) == false &&
                       attribute.HasFlag(FileAttributes.Hidden) == false;
            }

            bool existChild(FileNodeViewModel parent)
            {
                if (Directory.Exists(parent.FileName) == false)
                    return false;

                return Directory.EnumerateFileSystemEntries(parent.FileName, "*").Where(nodeFilter).Any();
            }

            IEnumerable<FileNodeViewModel> getChildren(FileNodeViewModel parent)
            {
                if (Directory.Exists(parent.FileName) == false)
                    return Enumerable.Empty<FileNodeViewModel>();

                var temp = Directory.EnumerateFileSystemEntries(parent.FileName, "*").Where(nodeFilter);
                var children = temp.Where(p => File.GetAttributes(p).HasFlag(FileAttributes.Directory))
                    .Union(temp.Where(p => File.GetAttributes(p).HasFlag(FileAttributes.Directory) == false))
                    .Select(p => new FileNodeViewModel(p, parent));
                return children.Any() ? children : new[] { Empty };
            }

            try
            {
                this.Children.Clear();
                this.Children.AddRange(getChildren(this));
                this.Children.Where(c => existChild(c)).ForEach(c => c.Children.Add(Empty));
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.Write(LogLevel.Warn, "ファイルの探索時にエラーが発生しました。", e);
            }
        }
    }
}
