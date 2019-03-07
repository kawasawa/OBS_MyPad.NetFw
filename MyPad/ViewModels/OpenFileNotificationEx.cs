using MyLib.Wpf.Interactions;
using System.Text;

namespace MyPad.ViewModels
{
    public class OpenFileNotificationEx : OpenFileNotification
    {
        public Encoding Encoding { get; set; } = null;
    }
}
