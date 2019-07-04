using MyLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MyPad.ViewModels
{
    public class FileTreeNodeViewModel : ViewModelBase
    {
        public static FileTreeNodeViewModel Empty { get; } = new FileTreeNodeViewModel(string.Empty) { IsEmpty = true };

        public bool IsEmpty { get; private set; }
        public string FileName { get; }
        public FileTreeNodeViewModel Parent { get; }
        public ObservableCollection<FileTreeNodeViewModel> Children { get; } = new ObservableCollection<FileTreeNodeViewModel>();

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

        private FileTreeNodeViewModel()
        {
        }

        public FileTreeNodeViewModel(string fileName)
            : this(fileName, null)
        {
        }

        public FileTreeNodeViewModel(string fileName, FileTreeNodeViewModel parent)
        {
            this.FileName = fileName;
            this.Parent = parent;
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

            bool existChild(FileTreeNodeViewModel parent)
            {
                if (Directory.Exists(parent.FileName))
                    return Directory.EnumerateFileSystemEntries(parent.FileName, "*", SearchOption.TopDirectoryOnly)
                        .Where(nodeFilter)
                        .Any();
                else
                    return false;
            }

            IEnumerable<FileTreeNodeViewModel> getChildren(FileTreeNodeViewModel parent)
            {
                if (Directory.Exists(parent.FileName))
                {
                    var temp = Directory.EnumerateFileSystemEntries(parent.FileName, "*", SearchOption.TopDirectoryOnly)
                        .Where(nodeFilter);
                    var children = temp.Where(path => File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                        .Union(temp.Where(path => File.GetAttributes(path).HasFlag(FileAttributes.Directory) == false))
                        .Select(p => new FileTreeNodeViewModel(p, parent));
                    return children.Any() ? children : new[] { Empty };
                }
                else
                {
                    return Enumerable.Empty<FileTreeNodeViewModel>();
                }
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
