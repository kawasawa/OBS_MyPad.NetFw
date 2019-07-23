using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using MyLib.Wpf.Interactions;
using MyPad.ViewModels;
using System.Linq;

namespace MyPad.Views.Behaviors
{
    public class SaveFileActionEx : SaveFileAction
    {
        protected override void OnPreviewShowDialog(FileNotificationBase context, CommonFileDialog dialog)
        {
            if (!(context is SaveFileNotificationEx contextEx))
                return;

            var encodingComboBox = FileActionExtensions.ConvertToComboBox(contextEx.Encoding);
            dialog.Controls.Add(encodingComboBox);
            base.OnPreviewShowDialog(context, dialog);
        }

        protected override void OnDialogClosed(FileNotificationBase context, CommonFileDialog dialog, CommonFileDialogResult dialogResult)
        {
            if (!(context is SaveFileNotificationEx contextEx))
                return;

            var encodingComboBox = (CommonFileDialogComboBox)dialog.Controls.First();
            contextEx.Encoding = ((FileActionExtensions.CommonFileDialogEncodingComboBoxItem)encodingComboBox.Items[encodingComboBox.SelectedIndex]).Encoding;
            base.OnDialogClosed(context, dialog, dialogResult);
        }
    }
}
