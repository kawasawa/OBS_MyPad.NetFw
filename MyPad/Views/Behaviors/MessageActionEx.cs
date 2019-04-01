using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MyLib.Wpf;
using MyLib.Wpf.Interactions;
using System;
using System.Windows;

namespace MyPad.Views.Behaviors
{
    public class MessageActionEx : MessageAction
    {
        public static readonly DependencyProperty UseOverlayMessageProperty = Interactor.RegisterDependencyProperty();

        public bool UseOverlayMessage
        {
            get => (bool)this.GetValue(UseOverlayMessageProperty);
            set => this.SetValue(UseOverlayMessageProperty, value);
        }

        protected override void Invoke(MessageNotification context, MessageKind kind, string title, string message, string detail, Window owner)
        {
            if (this.UseOverlayMessage && owner is MetroWindow metroWindow)
            {
                // ボタンを決定
                var style = MessageDialogStyle.Affirmative;
                var settings = new MetroDialogSettings();
                settings.AffirmativeButtonText = nameof(MessageBoxResult.OK);
                settings.NegativeButtonText = nameof(MessageBoxResult.Cancel);
                switch (kind)
                {
                    case MessageKind.Confirm:
                    case MessageKind.CancelableWarning:
                        style = MessageDialogStyle.AffirmativeAndNegative;
                        break;
                    case MessageKind.CancelableConfirm:
                        style = MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary;
                        settings.AffirmativeButtonText = nameof(MessageBoxResult.Yes);
                        settings.NegativeButtonText = nameof(MessageBoxResult.No);
                        settings.FirstAuxiliaryButtonText = nameof(MessageBoxResult.Cancel);
                        break;
                }

                // 既定値を決定
                settings.DefaultButtonFocus = MessageDialogResult.Affirmative;
                settings.DialogResultOnCancel = MessageDialogResult.Canceled;
                switch (kind)
                {
                    case MessageKind.Confirm:
                    case MessageKind.CancelableWarning:
                        switch (this.DefaultResult)
                        {
                            case null:
                                settings.DefaultButtonFocus = MessageDialogResult.Negative;
                                break;
                            default:
                                settings.DefaultButtonFocus = MessageDialogResult.Affirmative;
                                break;
                        }
                        break;
                    case MessageKind.CancelableConfirm:
                        switch (this.DefaultResult)
                        {
                            case false:
                                settings.DefaultButtonFocus = MessageDialogResult.Negative;
                                break;
                            case null:
                                settings.DefaultButtonFocus = MessageDialogResult.FirstAuxiliary;
                                break;
                            default:
                                settings.DefaultButtonFocus = MessageDialogResult.Affirmative;
                                break;
                        }
                        break;
                }

                // ダイアログを表示
                if (string.IsNullOrEmpty(detail) == false)
                    message += $"{Environment.NewLine}{Environment.NewLine}{detail}";
                settings.DialogTitleFontSize = 0;
                metroWindow.SetForegroundWindow();
                var result = metroWindow.ShowModalMessageExternal(string.Empty, message, style, settings);

                // 戻り値を設定
                switch (kind)
                {
                    case MessageKind.CancelableConfirm:
                        switch (result)
                        {
                            case MessageDialogResult.Affirmative:
                                context.Result = true;
                                break;
                            case MessageDialogResult.Negative:
                                context.Result = false;
                                break;
                            default:
                                context.Result = null;
                                break;
                        }
                        break;
                    default:
                        switch (result)
                        {
                            case MessageDialogResult.Affirmative:
                                context.Result = true;
                                break;
                            default:
                                context.Result = null;
                                break;
                        }
                        break;
                }
            }
            else
            {
                base.Invoke(context, kind, title, message, detail, owner);
            }
        }
    }
}
