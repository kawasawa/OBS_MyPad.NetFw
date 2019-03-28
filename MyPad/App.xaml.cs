using MyLib.Wpf;
using MyPad.Models;
using MyPad.Views;
using QuickConverter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Vanara.PInvoke;

namespace MyPad
{
    /// <summary>
    /// エントリーポイントを表します。
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// このクラスの新しいインスタンスを生成します。
        /// </summary>
        public App()
        {
            UnhandledExceptionObserver.Observe(this);
        }

        /// <summary>
        /// アプリケーションが開始されたときに行う処理を定義します。
        /// </summary>
        /// <param name="e">イベントの情報</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            Logger.LogWriting += this.Logger_LogWriting;

            if (this.TrySendArgs(e.Args))
            {
                // MainWindow の表示前のため、確実にアプリケーションを終了させる
                this.Shutdown(0);
                Environment.Exit(0);
                return;
            }

            if (SettingsService.Load() == false)
                Logger.Write(LogLevel.Warn, "設定ファイルの読み込みに失敗しました。");
            if (ResourceService.InitializeXshd() == false)
                Logger.Write(LogLevel.Warn, "シンタックス定義ファイルの初期化に失敗しました。");

            this.InitializeQuickConverter();
            this.MainWindow = new Workspace(e.Args);
            this.MainWindow.Closed += (_1, _2) =>
            {
                if (SettingsService.Instance.Save() == false)
                    Logger.Write(LogLevel.Warn, "設定ファイルの保存に失敗しました。");
            };
            this.MainWindow.Show();

            base.OnStartup(e);
        }

        /// <summary>
        /// ログが出力されるときに行う処理を定義します。
        /// </summary>
        /// <param name="sender">イベントの発生源</param>
        /// <param name="e">イベントの情報</param>
        private void Logger_LogWriting(object sender, LogEventArgs e)
        {
            if (e.LogLevel < AppConfig.LogLevel)
                e.Cancel = true;
        }

        /// <summary>
        /// 起動中の同一アプリケーションに対して指定されたデータを送信します。
        /// </summary>
        /// <param name="args">送信するデータ</param>
        /// <returns>処理されたかどうかを示す値</returns>
        private bool TrySendArgs(IEnumerable<string> args)
        {
            // HACK: ウィンドウハンドルの取得
            // 本アプリは MainWindow が非表示であるため Process.MainWindowHandle からハンドルを取得できない。
            // すべてのハンドルを列挙し、ウィンドウテキストから特定を試みる。
            var target = HWND.NULL;
            if (User32_Gdi.EnumWindows(new User32_Gdi.WNDENUMPROC((hWnd, _) => 
                {
                    try
                    {
                        var lpString = new StringBuilder(256);
                        User32_Gdi.GetWindowText(hWnd, lpString, lpString.Capacity);
                        if (lpString.ToString().Contains(Workspace.WINDOW_TITLE) == false)
                            return true;
                        target = hWnd;
                        return false;
                    }
                    catch
                    {
                        return true;
                    }
                }), IntPtr.Zero))
            {
                return false;
            }
            
            // 引数を送信する
            var structure = new COPYDATASTRUCT();
            structure.dwData = IntPtr.Zero;
            structure.lpData = args.Any() ? string.Join("|", args) : string.Empty;
            structure.cbData = Encoding.UTF8.GetByteCount(structure.lpData) + 1;
            var lParam = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
            Marshal.StructureToPtr(structure, lParam, false);
            User32_Gdi.SendMessage(target, (uint)User32_Gdi.WindowMessage.WM_COPYDATA, Process.GetCurrentProcess().Handle, lParam);
            return true;
        }

        /// <summary>
        /// QuickConverter の設定を初期化します。
        /// </summary>
        private void InitializeQuickConverter()
        {
            EquationTokenizer.AddNamespace(typeof(System.Object));
            EquationTokenizer.AddNamespace(typeof(System.IO.Path));
            EquationTokenizer.AddNamespace(typeof(System.Reflection.Assembly));
            EquationTokenizer.AddNamespace(typeof(System.Windows.UIElement));
            EquationTokenizer.AddNamespace(typeof(Microsoft.VisualBasic.ControlChars));
            EquationTokenizer.AddNamespace(typeof(MyLib.Wpf.Interactions.InteractionNotification));
            EquationTokenizer.AddExtensionMethods(typeof(System.Linq.Enumerable));
        }
    }
}
