using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using MyLib.Wpf.Interactions;
using MyPad.Properties;
using MyPad.ViewModels;
using System.Linq;

namespace MyPad.Views.Behaviors
{
    public class SaveFileActionEx : SaveFileAction
    {
        protected override void OnPreviewShowDialog(FileNotificationBase context, object dialog)
        {
            if (!(context is SaveFileNotificationEx contextEx) || !(dialog is CommonFileDialog fileDialog))
                return;

            var encodingComboBox = FileActionExtensions.ConvertToComboBox(contextEx.Encoding);
            var encodingGroupBox = new CommonFileDialogGroupBox($"{Resources.Label_Encoding}(&E):");
            encodingGroupBox.Items.Add(encodingComboBox);
            fileDialog.Controls.Add(encodingGroupBox);
            base.OnPreviewShowDialog(context, dialog);
        }

        protected override void OnDialogClosed(FileNotificationBase context, object dialog, object dialogResult)
        {
            if (!(context is SaveFileNotificationEx contextEx) || !(dialog is CommonFileDialog fileDialog))
                return;

            var encodingGroupBox = (CommonFileDialogGroupBox)fileDialog.Controls.First();
            var encodingComboBox = (CommonFileDialogComboBox)encodingGroupBox.Items.First();
            contextEx.Encoding = ((FileActionExtensions.CommonFileDialogEncodingComboBoxItem)encodingComboBox.Items[encodingComboBox.SelectedIndex]).Encoding;
            base.OnDialogClosed(context, dialog, dialogResult);
        }
    }
}
