using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using MyLib;
using MyPad.Models;
using MyPad.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace MyPad
{
    public static class Consts
    {
        public static readonly IEnumerable<object> CULTURES
            = new[] {
                new { Description = "English", Name = "en-US" },
                new { Description = "日本語", Name = "ja-JP" },
            };

        public static readonly IEnumerable<Encoding> ENCODINGS
            = Encoding.GetEncodings().Select(x => x.GetEncoding());

        public static readonly IEnumerable<FontFamily> FONT_FAMILIES
            = Fonts.SystemFontFamilies;

        public static readonly IEnumerable<double> FONT_SIZES
            = new[] { 6, 7, 8, 9, 10, 10.5, 11, 12, 13, 14, 15, 16, 18, 20, 22, 24, 26, 28, 32, 36, 42, 48, 60, 72, 96 };

        public static readonly string DEFAULT_FILE_EXPLORER_ROOT
            = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public const string DEFAULT_EXTENSION = "txt";
        public static string FILE_FILTER
            => string.Join("|", 
                new[] { $"{Resources.Label_AllFiles}|*.*" }
                .Concat(SYNTAX_DEFINITIONS.Values.Select(d => $"{d.Name}|{string.Join(";", d.Extensions)}")));

        public const string TEMPORARY_NAME_FORMAT = "yyyyMMddHHmmssfff";
        public static string CURRENT_TEMPORARY
            => Path.Combine(ProductInfo.Temporary, Process.GetCurrentProcess().StartTime.ToString(TEMPORARY_NAME_FORMAT));

        private static IDictionary<string, XshdSyntaxDefinition> _SYNTAX_DEFINITIONS;
        public static IDictionary<string, XshdSyntaxDefinition> SYNTAX_DEFINITIONS
        {
            get
            {
                if (_SYNTAX_DEFINITIONS == null)
                {
                    _SYNTAX_DEFINITIONS = new Dictionary<string, XshdSyntaxDefinition>();
                    ResourceService.Instance.EnumerateSyntaxDefinitions()
                        .Where(d => _SYNTAX_DEFINITIONS.ContainsKey(d.Name) == false)
                        .ForEach(d => _SYNTAX_DEFINITIONS.Add(d.Name, d));
                }
                return _SYNTAX_DEFINITIONS;
            }
        }
    }
}
