using Hardcodet.Wpf.TaskbarNotification;
using MyLib.Wpf.Interactions;
using System.Windows;

namespace MyPad.Views.Behaviors
{
    public class BalloonAction : MessageAction
    {
        protected override void Invoke(MessageNotification context, MessageKind kind, string title, string message, string detail, Window owner)
        {
            if (!(this.AssociatedObject is TaskbarIcon taskbarIcon))
                return;

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
        }
    }
}
