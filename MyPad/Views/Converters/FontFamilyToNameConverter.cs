using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace MyPad.Views.Converters
{
    public class FontFamilyToNameConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is FontFamily fontFamily))
                return string.Empty;

            var language = XmlLanguage.GetLanguage(culture.IetfLanguageTag);
            return fontFamily.FamilyNames.FirstOrDefault(x => x.Key == language).Value ?? fontFamily.Source;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
