using MyLib.Wpf.Interactions;
using MyPad.Models;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace MyPad.ViewModels
{
    public class WorkspaceViewModel : ViewModelBase
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

        public ObservableCollection<MainWindowViewModel> Windows { get; } = new ObservableCollection<MainWindowViewModel>();
        public ObservableCollection<string> ClipboardItems { get; } = new ObservableCollection<string>();

        private string _selectedClipboardItem;
        public string SelectedClipboardItem
        {
            get => this._selectedClipboardItem;
            set => this.SetProperty(ref this._selectedClipboardItem, value);
        }

        private MainWindowViewModel _activeWindow;
        public MainWindowViewModel ActiveWindow
        {
            get => this._activeWindow;
            set => this.SetProperty(ref this._activeWindow, value);
        }

        public Func<MainWindowViewModel> WindowFactory =>
            () =>
            {
                var window = new MainWindowViewModel();
                window.Disposed += this.Window_Disposed;
                return window;
            };

        #endregion

        #region コマンド

        public ICommand AddEditorCommand
            => new DelegateCommand(() =>
            {
                MainWindowViewModel window = null;
                if (this.Windows.Any())
                {
                    this.ActiveWindow.AddEditor();
                    window = this.ActiveWindow;
                }
                else
                {
                    window = this.AddWindow();
                }
                window.TransitionRequest.Raise(new TransitionNotification(TransitionKind.Activate));
            });

        public ICommand AddWindowCommand
           => new DelegateCommand(() => this.AddWindow().TransitionRequest.Raise(new TransitionNotification(TransitionKind.Activate)));

        public ICommand MergeWindowsCommand
            => new DelegateCommand(() => this.MergeEditors());

        public ICommand CloseNotificationIconCommand
            => new DelegateCommand(() =>
            {
                SettingsService.Instance.System.EnableNotificationIcon = false;
                if (this.Windows.Any() == false)
                    this.Dispose();
            });

        public ICommand ExitApplicationCommand
            => new DelegateCommand(async () =>
            {
                if (this.Windows.Any())
                {
                    for (var i = this.Windows.Count - 1; 0 <= i; i--)
                    {
                        if (await this.Windows[i].SaveChangesIfAndRemoveAll() == false)
                            return;
                        this.Windows[i].Dispose();
                    }
                }
                this.Dispose();
            });

        #endregion

        #region メソッド

        public WorkspaceViewModel()
        {
            Instance = Instance == null ? this : throw new InvalidOperationException("このクラスのインスタンスを同時に複数生成することはできません。");

            BindingOperations.EnableCollectionSynchronization(this.Windows, new object());
            BindingOperations.EnableCollectionSynchronization(this.ClipboardItems, new object());
        }

        protected override void Dispose(bool disposing)
        {
            for (var i = this.Windows.Count - 1; 0 <= i; i--)
                this.RemoveWindow(this.Windows[i]);
            Instance = null;
            base.Dispose(disposing);
        }

        public MainWindowViewModel AddWindow(IEnumerable<string> paths = null, bool doTransition = true, bool addEmptyEditor = true)
        {
            var window = this.WindowFactory.Invoke();
            if (paths?.Any() == true)
                window.LoadEditor(paths);
            else if (addEmptyEditor)
                window.AddEditor();
            this.AddWindow(window, doTransition);
            return window;
        }

        public MainWindowViewModel AddWindow(TextEditorViewModel editor, bool doTransition)
        {
            var window = this.WindowFactory.Invoke();
            window.AddEditor(editor);
            window.ActiveEditor = editor;
            this.AddWindow(window, doTransition);
            return window;
        }

        public void AddWindow(MainWindowViewModel window, bool doTransition)
        {
            this.Windows.Add(window);
            if (doTransition)
                this.TransitionRequest.Raise(new TransitionNotification(window));
        }

        public bool RemoveWindow(MainWindowViewModel window)
        {
            if (this.Windows.Contains(window) == false)
                return false;

            this.Windows.Remove(window);
            window.Disposed -= this.Window_Disposed;
            window.Dispose();
            return true;
        }

        public void AddClipboardItem(string text)
        {
            if (string.IsNullOrEmpty(text) || this.ClipboardItems.FirstOrDefault()?.Equals(text) == true)
                return;

            if (SettingsService.Instance.System.ClipboardHistorySize <= this.ClipboardItems.Count)
                this.ClipboardItems.RemoveAt(this.ClipboardItems.Count - 1);
            this.ClipboardItems.Insert(0, text);
        }

        public TextEditorViewModel DelegateActivateEditor(MainWindowViewModel sender, string path)
        {
            foreach (var window in this.Windows.Where(w => w.Equals(sender) == false))
            {
                var editor = window.Editors.FirstOrDefault(e => e.FileName.Equals(path));
                if (editor == null)
                    continue;

                window.ActiveEditor = editor;
                return editor;
            }
            return null;
        }

        public bool DelegateReloadEditor(MainWindowViewModel sender, string path, Encoding encoding)
        {
            foreach (var window in this.Windows.Where(w => w.Equals(sender) == false))
            {
                var editor = window.Editors.FirstOrDefault(e => e.FileName.Equals(path));
                if (editor == null)
                    continue;

                window.ReloadEditor(editor, encoding);
                window.TransitionRequest.Raise(new TransitionNotification(TransitionKind.Activate));
                return true;
            }
            return false;
        }

        private void MergeEditors()
        {
            var mainWindow = this.ActiveWindow ?? this.Windows.First();
            for (var i = this.Windows.Count - 1; 0 <= i; i--)
            {
                var window = this.Windows[i];
                if (window.Equals(mainWindow))
                    continue;

                for (var j = window.Editors.Count - 1; 0 <= j; j--)
                {
                    var editor = window.Editors[j];
                    window.Editors.RemoveAt(j);
                    mainWindow.AddEditor(editor);
                }
                window.Dispose();
            }
        }

        private void Window_Disposed(object sender, EventArgs e)
        {
            var window = (MainWindowViewModel)sender;
            this.RemoveWindow(window);
            if (this.Windows.Any() || (SettingsService.Instance.System.EnableNotificationIcon && SettingsService.Instance.System.EnableResident))
                return;

            this.Dispose();
        }

        #endregion
    }
}
