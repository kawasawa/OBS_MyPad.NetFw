using MyLib;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

/// <summary>
/// ハンドルされていない例外の発生を監視します。
/// </summary>
public static class UnhandledExceptionObserver
{
    /// <summary>
    /// 指定されたアプリケーションに対してハンドルされていない例外の発生を監視します。
    /// </summary>
    /// <param name="application">アプリケーション</param>
    public static void Observe(Application application)
    {
        AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        application.DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    /// <summary>
    /// UIスレッドで例外が発生したときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        HandleException(e.Exception);
        e.Handled = true;
    }

    /// <summary>
    /// タスク内で例外が発生したときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleException(e.Exception?.InnerException as Exception);
        e.SetObserved();
    }

    /// <summary>
    /// ドメイン内で例外が発生したときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    private static void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        HandleException(e.ExceptionObject as Exception);
    }

    /// <summary>
    /// 例外の内容を通知し、アプリケーションを終了します。
    /// </summary>
    /// <param name="e">例外の情報</param>
    private static void HandleException(Exception e)
    {
        try { Logger.Write(LogLevel.Error, "ハンドルされていない例外が発生しました。", e); } catch { }
        try { MessageBox.Show($"{e.GetType().Name}{Environment.NewLine}{Environment.NewLine}{e.Message}", ProductInfo.Product, MessageBoxButton.OK, MessageBoxImage.Error); } catch { }
        Environment.Exit(1);
    }
}
