using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using MyLib.Wpf;
using MyPad.Models;
using MyPad.Properties;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MyPad
{
    public static class Consts
    {
        public const string DATE_TIME_FORMAT = "yyyyMMddHHmmssfff";

        public static string FILE_FILTER
            => string.Join("|", 
                new[] { $"{Resources.Label_AllFiles}|*.*" }
                .Concat(SYNTAX_DEFINITIONS.Values.Select(d => $"{d.Name}|{string.Join(",", d.Extensions)}")));

        public static string CURRENT_TEMPORARY
            => Path.Combine(ProductInfo.Temporary, Process.GetCurrentProcess().StartTime.ToString(DATE_TIME_FORMAT));

        private static IDictionary<string, XshdSyntaxDefinition> _SYNTAX_DEFINITIONS;
        public static IDictionary<string, XshdSyntaxDefinition> SYNTAX_DEFINITIONS
        {
            get
            {
                if (_SYNTAX_DEFINITIONS == null)
                {
                    _SYNTAX_DEFINITIONS = new Dictionary<string, XshdSyntaxDefinition>();
                    ResourceService.Instance.EnumerateSyntaxDefinitions()
                        .Where(x => _SYNTAX_DEFINITIONS.ContainsKey(x.Name) == false)
                        .ForEach(definition => _SYNTAX_DEFINITIONS.Add(definition.Name, definition));
                }
                return _SYNTAX_DEFINITIONS;
            }
        }
    }
}
