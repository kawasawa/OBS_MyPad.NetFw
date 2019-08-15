using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using MyLib.Wpf.Interactions;
using MyPad.Properties;
using MyPad.ViewModels;
using System.Linq;

namespace MyPad.Views.Behaviors
{
    public class OpenFileActionEx : OpenFileAction
    {
        protected override void OnPreviewShowDialog(FileNotificationBase context, object dialog)
        {
            if (!(context is OpenFileNotificationEx contextEx) || !(dialog is CommonFileDialog fileDialog))
                return;

            var encodingComboBox = FileActionExtensions.ConvertToComboBox(contextEx.Encoding);
            encodingComboBox.Items.Insert(0, new FileActionExtensions.CommonFileDialogEncodingComboBoxItem(null, Resources.Label_AutoDetect));
            encodingComboBox.SelectedIndex++;
            fileDialog.Controls.Add(encodingComboBox);
            base.OnPreviewShowDialog(context, dialog);
        }

        protected override void OnDialogClosed(FileNotificationBase context, object dialog, object dialogResult)
        {
            if (!(context is OpenFileNotificationEx contextEx) || !(dialog is CommonFileDialog fileDialog))
                return;

            var encodingComboBox = (CommonFileDialogComboBox)fileDialog.Controls.First();
            contextEx.Encoding = ((FileActionExtensions.CommonFileDialogEncodingComboBoxItem)encodingComboBox.Items[encodingComboBox.SelectedIndex]).Encoding;
            base.OnDialogClosed(context, dialog, dialogResult);
        }
    }
}
