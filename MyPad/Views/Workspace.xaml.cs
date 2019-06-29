using MyLib;
using MyLib.Wpf;
using MyLib.Wpf.Interactions;
using MyPad.Models;
using MyPad.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Vanara.PInvoke;

namespace MyPad.Views
{
    public partial class Workspace : Window
    {
        public static readonly string WINDOW_TITLE = $"__{ProductInfo.Product}__{nameof(Workspace)}__";

        private readonly IEnumerable<string> _initialArgs;
        private HwndSource _handleSource;
        private ClipboardViewer _clipboardViewer;

        private WorkspaceViewModel ViewModel => this.DataContext as WorkspaceViewModel;

        public Workspace()
        {
            this.InitializeComponent();
            this.ViewModel.Disposed += this.ViewModel_Disposed;
            this.Loaded += this.Window_Loaded;
            this.Closed += this.Window_Closed;
            this.TaskbarIcon.TrayMouseDoubleClick += this.TaskbarIcon_TrayMouseDoubleClick;
        }

        public Workspace(IEnumerable<string> args)
           : this()
        {
            this._initialArgs = args;
        }

        private void ViewModel_Disposed(object sender, EventArgs e)
        {
            this.ViewModel.Disposed -= this.ViewModel_Disposed;
            this.Dispatcher.InvokeAsync(() => this.Close(), DispatcherPriority.ApplicationIdle);
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((User32_Gdi.WindowMessage)msg)
            {
                case User32_Gdi.WindowMessage.WM_COPYDATA:
                    {
                        MainWindowViewModel window = null;
                        var structure = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                        if (string.IsNullOrEmpty(structure.lpData) == false)
                        {
                            var paths = structure.lpData.Split('|');
                            if (this.ViewModel.Windows.Any())
                            {
                                window = this.ViewModel.ActiveWindow;
                                window.LoadEditor(paths);
                            }
                            else
                            {
                                window = this.ViewModel.AddWindow(paths);
                            }
                        }
                        else
                        {
                            window = this.ViewModel.Windows.Any() ? this.ViewModel.ActiveWindow : this.ViewModel.AddWindow();
                        }
                        window.TransitionRequest.Raise(new TransitionNotification(TransitionKind.Activate));
                        break;
                    }
            }
            return IntPtr.Zero;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this._handleSource = (HwndSource)PresentationSource.FromVisual(this);
            this._handleSource.AddHook(this.WndProc);
            this._clipboardViewer = new ClipboardViewer(new WindowInteropHelper(this).Handle);
            this._clipboardViewer.DrawClipboard += this.ClipboardViewer_DrawClipboard;

            this.Hide();
            this.ViewModel.AddWindow(this._initialArgs);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this._handleSource.RemoveHook(this.WndProc);
            this._clipboardViewer.DrawClipboard -= this.ClipboardViewer_DrawClipboard;
            this._clipboardViewer.Dispose();
        }

        private void TaskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            var windows = Application.Current.Windows.OfType<MainWindow>();
            if (windows.Any())
                windows.ForEach(w => w.SetForegroundWindow());
            else
                this.ViewModel.AddWindow();
        }

        private void ClipboardViewer_DrawClipboard(object sender, EventArgs e)
        {
            // HACK: クリップボードへのアクセス
            // OpenClipboard に失敗する場合があるため、STA スレッド上でテキストを取得する。

            var text = string.Empty;
            var thread = new Thread(() => { try { text = Clipboard.GetText(); } catch { } });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (string.IsNullOrEmpty(text) == false)
                this.ViewModel.AddClipboardItem(text);
        }
    }
}
