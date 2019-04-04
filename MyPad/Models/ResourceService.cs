using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using MyLib;
using MyPad.Properties;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace MyPad.Models
{
    public sealed class ResourceService : ModelBase
    {
        public const string DEFAULT_CULTURE_NAME = "en-US";
        public static readonly Encoding FileEncoding = new UTF8Encoding(true);
        public static readonly string XshdDirectoryPath = Path.Combine(ProductInfo.Roaming, "xshd");

        public static ResourceService Instance { get; } = new ResourceService();

        public Resources Resources { get; } = new Resources();

        private ResourceService()
        {
        }

        public void SetCulture(string name)
        {
            Resources.Culture = CultureInfo.GetCultureInfo(name?.ToLower().Equals(DEFAULT_CULTURE_NAME.ToLower()) == false ? name : string.Empty);
            this.RaisePropertyChanged(nameof(this.Resources));
        }

        public static bool InitializeXshd(bool forceInitilize = false)
        {
            try
            {
                if (forceInitilize == false && Directory.Exists(XshdDirectoryPath))
                    return true;

                Directory.CreateDirectory(XshdDirectoryPath);
                if (Directory.Exists(XshdDirectoryPath) == false)
                    return false;

                typeof(Resources).GetProperties().Where(p => p.PropertyType == typeof(byte[])).ForEach(p =>
                {
                    using (var stream = new FileStream(Path.Combine(XshdDirectoryPath, $"{p.Name}.xshd"), FileMode.Create, FileAccess.Write))
                    using (var writer = new BinaryWriter(stream, FileEncoding))
                    {
                        writer.Write((byte[])p.GetValue(null));
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CleanUpXshd()
        {
            try
            {
                if (Directory.Exists(XshdDirectoryPath) == false)
                    return true;

                var info = new DirectoryInfo(XshdDirectoryPath);
                info.EnumerateFiles().ForEach(i => Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(i.FullName));
                info.EnumerateDirectories().ForEach(i => Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(i.FullName, Microsoft.VisualBasic.FileIO.DeleteDirectoryOption.DeleteAllContents));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerable<XshdSyntaxDefinition> EnumerateSyntaxDefinitions()
        {
            if (Directory.Exists(XshdDirectoryPath) == false)
                yield break;

            foreach (var path in Directory.EnumerateFiles(XshdDirectoryPath))
            {
                XshdSyntaxDefinition definition;
                try
                {
                    using (var reader = new XmlTextReader(path))
                    {
                        definition = HighlightingLoader.LoadXshd(reader);
                    }
                }
                catch
                {
                    continue;
                }
                yield return definition;
            }
        }
    }
}