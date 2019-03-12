using MyLib.Wpf;
using MyLib.Wpf.Interactions;
using MyPad.Models;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace MyPad.ViewModels
{
    public class WorkspaceViewModel : ContainerViewModelBase<MainWindowViewModel>
    {
        #region スタティック

        private static Dispatcher _dispatcher;
        public static Dispatcher Dispatcher
            => _dispatcher ?? (_dispatcher = Application.Current.Dispatcher);

        public static WorkspaceViewModel Instance { get; private set; }

        #endregion

        #region リクエスト

        public InteractionRequest<MessageNotification> NotifyRequest { get; } = new InteractionRequest<MessageNotification>();
        public InteractionRequest<TransitionNotification> TransitionRequest { get; } = new InteractionRequest<TransitionNotification>();

        #endregion

        #region プロパティ

        private string _selectedClipboardItem;
        public string SelectedClipboardItem
        {
            get => this._selectedClipboardItem;
            set => this.SetProperty(ref this._selectedClipboardItem, value);
        }

        public ObservableCollection<string> ClipboardItems { get; } = new ObservableCollection<string>();

        public override Func<MainWindowViewModel> ContentFactory =>
            () =>
            {
                var content = new MainWindowViewModel();
                content.Disposed += this.Content_Disposed;
                return content;
            };

        #endregion

        #region コマンド

        public ICommand AddEditorCommand
            => new DelegateCommand(() =>
            {
                MainWindowViewModel content = null;
                if (this.Contents.Any())
                {
                    this.ActiveContent.AddContent();
                    content = this.ActiveContent;
                }
                else
                {
                    content = this.AddContent();
                }
                content.TransitionRequest.Raise(new TransitionNotification(TransitionKind.Activate));
            });

        public ICommand AddWindowCommand
           => new DelegateCommand(() => this.AddContent().TransitionRequest.Raise(new TransitionNotification(TransitionKind.Activate)));

        public ICommand MergeWindowsCommand
            => new DelegateCommand(() => this.MergeContents());

        public ICommand CloseNotificationIconCommand
            => new DelegateCommand(() =>
            {
                SettingsService.Instance.System.EnableNotificationIcon = false;
                if (this.Contents.Any() == false)
                    this.Dispose();
            });

        public ICommand CloseAllWindowCommand
            => new DelegateCommand(async () =>
            {
                if (this.Contents.Any())
                {
                    for (var i = this.Contents.Count - 1; 0 <= i; i--)
                    {
                        if (await this.Contents[i].SaveChangesIfAndRemove() == false)
                            return;
                        this.Contents[i].Dispose();
                    }
                }
                this.Dispose();
            });

        public ICommand ClearClipboardItemCommand
           => new DelegateCommand(() =>
           {
               if (string.IsNullOrEmpty(this.SelectedClipboardItem) == false && this.ClipboardItems.Contains(this.SelectedClipboardItem))
                   this.ClipboardItems.Remove(this.SelectedClipboardItem);
           });

        public ICommand ClearAllClipboardItemsCommand
           => new DelegateCommand(() => this.ClipboardItems.Clear());

        #endregion

        #region メソッド

        public WorkspaceViewModel()
        {
            Instance = Instance == null ? this : throw new InvalidOperationException("このクラスのインスタンスを同時に複数生成することはできません。");

            BindingOperations.EnableCollectionSynchronization(this.ClipboardItems, new object());

            // 一時フォルダのクリーンアップ
            try
            {
                Task.Run(() =>
                {
                    var info = new DirectoryInfo(ProductInfo.Temporary);
                    if (info.Exists == false)
                        return;

                    var now = DateTime.Now;
                    info.EnumerateFiles()
                        .ForEach(i => Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(i.FullName));
                    info.EnumerateDirectories()
                        .Where(i => DateTime.TryParseExact(Path.GetFileName(i.FullName), Consts.DATE_TIME_FORMAT, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dateTime) &&
                                    dateTime.AddDays(AppConfig.CacheLifetime) < now)
                        .ForEach(i => Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(i.FullName, Microsoft.VisualBasic.FileIO.DeleteDirectoryOption.DeleteAllContents));
                });

                if (Directory.Exists(Consts.CURRENT_TEMPORARY) == false)
                    Directory.CreateDirectory(Consts.CURRENT_TEMPORARY);
            }
            catch
            {
            }
        }

        protected override void Dispose(bool disposing)
        {
            // 一時フォルダを削除
            try
            {
                if (Directory.Exists(Consts.CURRENT_TEMPORARY))
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(Consts.CURRENT_TEMPORARY, Microsoft.VisualBasic.FileIO.DeleteDirectoryOption.DeleteAllContents);
            }
            catch
            {
            }

            // コンテンツを削除
            for (var i = this.Contents.Count - 1; 0 <= i; i--)
                this.RemoveContent(this.Contents[i]);

            Instance = null;
            base.Dispose(disposing);
        }

        public MainWindowViewModel AddContent(IEnumerable<string> paths = null, bool doTransition = true, bool addEmptyEditor = true)
        {
            var content = this.ContentFactory.Invoke();
            if (paths?.Any() == true)
                content.LoadContent(paths);
            else if (addEmptyEditor)
                content.AddContent();
            this.AddContent(content, doTransition);
            return content;
        }

        public MainWindowViewModel AddContent(TextEditorViewModel editor, bool doTransition)
        {
            var content = this.ContentFactory.Invoke();
            content.AddContent(editor);
            content.ActiveContent = editor;
            this.AddContent(content, doTransition);
            return content;
        }

        public void AddContent(MainWindowViewModel content, bool doTransition)
        {
            this.Contents.Add(content);
            if (doTransition)
                this.TransitionRequest.Raise(new TransitionNotification(content));
        }

        public bool RemoveContent(MainWindowViewModel content)
        {
            if (this.Contents.Contains(content) == false)
                return false;

            this.Contents.Remove(content);
            content.Disposed -= this.Content_Disposed;
            content.Dispose();
            return true;
        }

        public void AddClipboardHistory(string text)
        {
            if (string.IsNullOrEmpty(text) || this.ClipboardItems.FirstOrDefault()?.Equals(text) == true)
                return;
            if (SettingsService.Instance.System.ClipboardHistoryCount <= this.ClipboardItems.Count)
                this.ClipboardItems.RemoveAt(this.ClipboardItems.Count - 1);
            this.ClipboardItems.Insert(0, text);
        }

        public TextEditorViewModel DelegateActivateContent(MainWindowViewModel sender, string path)
        {
            foreach (var window in this.Contents.Where(c => c.Equals(sender) == false))
            {
                var editor = window.Contents.FirstOrDefault(m => m.FileName.Equals(path));
                if (editor == null)
                    continue;
                window.ActiveContent = editor;
                return editor;
            }
            return null;
        }

        public bool DelegateReloadContent(MainWindowViewModel sender, string path, Encoding encoding)
        {
            foreach (var window in this.Contents.Where(c => c.Equals(sender) == false))
            {
                var editor = window.Contents.FirstOrDefault(m => m.FileName.Equals(path));
                if (editor == null)
                    continue;
                window.ReloadContent(editor, encoding);
                window.TransitionRequest.Raise(new TransitionNotification(TransitionKind.Activate));
                return true;
            }
            return false;
        }

        private void MergeContents()
        {
            var mainWindow = this.ActiveContent ?? this.Contents.First();
            for (var i = this.Contents.Count - 1; 0 <= i; i--)
            {
                var window = this.Contents[i];
                if (window.Equals(mainWindow))
                    continue;

                for (var j = window.Contents.Count - 1; 0 <= j; j--)
                {
                    var editor = window.Contents[j];
                    window.Contents.RemoveAt(j);
                    mainWindow.AddContent(editor);
                }
                window.Dispose();
            }
        }

        private void Content_Disposed(object sender, EventArgs e)
        {
            var content = (MainWindowViewModel)sender;
            this.RemoveContent(content);
            if (this.Contents.Any() == false &&
                (SettingsService.Instance.System.EnableNotificationIcon == false || SettingsService.Instance.System.EnableResident == false))
            {
                this.Dispose();
            }
        }

        #endregion
    }
}
