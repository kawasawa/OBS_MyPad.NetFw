using MyLib;
using MyPad.Models;
using MyPad.Views;
using QuickConverter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
            Logger.MinLogLevel = AppConfig.MinLogLevel;
        }

        /// <summary>
        /// アプリケーションが開始されたときに行う処理を定義します。
        /// </summary>
        /// <param name="e">イベントの情報</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            if (this.TrySendArgs(e.Args))
            {
                // MainWindow の表示前のため、確実にアプリケーションを終了させる
                this.Shutdown(0);
                Environment.Exit(0);
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var info = new DirectoryInfo(ProductInfo.Temporary);
                    if (info.Exists == false)
                        return;

                    // 残存する一時ファイルのうち指定の日数を超えたものを削除する
                    // ファイルの場合は更新日時を、ディレクトリの場合はテンポラリの命名規則に従い日付に変換し判定を行う
                    var basis = DateTime.Now.AddDays(-1 * AppConfig.LifetimeOfTempsLeftBehind);
                    info.EnumerateFiles()
                        .Where(i => i.LastWriteTime < basis)
                        .ForEach(i => File.Delete(i.FullName));
                    info.EnumerateDirectories()
                        .Where(i => DateTime.TryParseExact(Path.GetFileName(i.FullName), Consts.TEMPORARY_NAME_FORMAT, CultureInfo.CurrentCulture, DateTimeStyles.None, out var value) == false || value < basis)
                        .ForEach(i => Directory.Delete(i.FullName, true));
                }
                catch (Exception ex)
                {
                    Logger.Write(LogLevel.Warn, "一時ファイルの削除に失敗しました。(システム起動時)", ex);
                }
                finally
                {
                    if (Directory.Exists(Consts.CURRENT_TEMPORARY) == false)
                        Directory.CreateDirectory(Consts.CURRENT_TEMPORARY);
                }
            });

            SettingsService.Load();
            ResourceService.InitializeXshd();
            this.InitializeQuickConverter();

            this.MainWindow = new Workspace(e.Args);
            this.MainWindow.Closed += (_1, _2) =>
            {
                SettingsService.Instance.Save();
                try
                {
                    if (Directory.Exists(Consts.CURRENT_TEMPORARY))
                        Directory.Delete(Consts.CURRENT_TEMPORARY, true);
                }
                catch (Exception ex)
                {
                    Logger.Write(LogLevel.Warn, $"一時フォルダの削除に失敗しました。(システム終了時) : {Consts.CURRENT_TEMPORARY}", ex);
                }
            };
            this.MainWindow.Show();

            base.OnStartup(e);
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
            if (User32_Gdi.EnumWindows(
                new User32_Gdi.WNDENUMPROC((hWnd, _) => 
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
                }),
                IntPtr.Zero))
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
            // System
            EquationTokenizer.AddNamespace(typeof(System.Object));                   // mscorlib              : System
            EquationTokenizer.AddNamespace(typeof(System.IO.Path));                  // mscorlib              : System.IO
            EquationTokenizer.AddNamespace(typeof(System.Text.Encoding));            // mscorlib              : System.Text
            EquationTokenizer.AddNamespace(typeof(System.Reflection.Assembly));      // mscorlib              : System.Reflection
            EquationTokenizer.AddNamespace(typeof(System.Windows.Point));            // WindowsBase           : System.Windows
            EquationTokenizer.AddNamespace(typeof(System.Windows.UIElement));        // PresentationCore      : System.Windows
            EquationTokenizer.AddNamespace(typeof(System.Windows.Controls.Control)); // PresentationFramework : System.Windows.Controls
            EquationTokenizer.AddExtensionMethods(typeof(System.Linq.Enumerable));   // System.Core           : System.Linq

            // Additional
            EquationTokenizer.AddNamespace(typeof(Microsoft.VisualBasic.Globals));   // Microsoft.VisualBasic : Microsoft.VisualBasic
            EquationTokenizer.AddNamespace(typeof(MyLib.Wpf.Interactions.InteractionNotification));
        }
    }
}
