using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using System.Linq;
using System.Text;

namespace MyPad.Views.Behaviors
{
    public static class FileActionExtensions
    {
        public static CommonFileDialogComboBox ConvertToComboBox(Encoding defaultEncoding)
        {
            var comboBox = new CommonFileDialogComboBox();
            var encodings = Encoding.GetEncodings().Select(e => e.GetEncoding());
            for (var i = 0; i < encodings.Count(); i++)
            {
                comboBox.Items.Add(new CommonFileDialogEncodingComboBoxItem(encodings.ElementAt(i)));
                if (encodings.ElementAt(i).Equals(defaultEncoding))
                    comboBox.SelectedIndex = i;
            }
            return comboBox;
        }

        public class CommonFileDialogEncodingComboBoxItem : CommonFileDialogComboBoxItem
        {
            public Encoding Encoding { get; }

            public CommonFileDialogEncodingComboBoxItem(Encoding encoding)
                : this(encoding, $"{encoding.CodePage} - {encoding.EncodingName}")
            {
            }

            public CommonFileDialogEncodingComboBoxItem(Encoding encoding, string text)
                : base(text)
            {
                this.Encoding = encoding;
            }
        }
    }
}
