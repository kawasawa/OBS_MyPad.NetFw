using MyLib.Wpf.Interactions;
using System.Text;

namespace MyPad.ViewModels
{
    public class SaveFileNotificationEx : SaveFileNotification
    {
        public Encoding Encoding { get; set; } = null;
    }
}
