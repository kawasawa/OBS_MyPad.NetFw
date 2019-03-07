using Hardcodet.Wpf.TaskbarNotification;
using MyLib.Wpf.Interactions;
using Prism.Interactivity.InteractionRequest;

namespace MyPad.Views.Behaviors
{
    public class BalloonAction : MessageAction
    {
        protected override void Invoke(InteractionRequestedEventArgs args)
        {
            if (!(args.Context is MessageNotification context) || !(this.AssociatedObject is TaskbarIcon taskbarIcon))
                return;

            var kind = this.Kind.HasValue ? this.Kind : context.Kind;
            var message = string.IsNullOrEmpty(this.Message) == false ? this.Message : context.Content?.ToString() ?? string.Empty;
            var detail = string.IsNullOrEmpty(this.Detail) == false ? this.Detail : context.Detail?.ToString() ?? string.Empty;

            BalloonIcon image;
            switch (kind)
            {
                case MessageKind.Warning:
                case MessageKind.CancelableWarning:
                    image = BalloonIcon.Warning;
                    break;
                case MessageKind.Error:
                    image = BalloonIcon.Error;
                    break;
                default:
                    image = BalloonIcon.Info;
                    break;
            }

            taskbarIcon.ShowBalloonTip(message, detail, image);
            args.Callback();
        }
    }
}
