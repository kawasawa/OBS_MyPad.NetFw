using MyLib.Wpf;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;
using System.Text;

namespace MyPad.Models
{
    public sealed class SettingsService : ModelBase
    {
        public static readonly string SettingsFilePath = Path.Combine(ProductInfo.Roaming, $"Settings.json");
        public static readonly Encoding FILE_ENCODING = new UTF8Encoding(true);

        public static SettingsService Instance { get; } = new SettingsService();

        private SystemSettings _system = new SystemSettings();
        private WindowSettings _window = new WindowSettings();
        private TextEditorSettings _textEditor = new TextEditorSettings();

        public SystemSettings System
        {
            get => this._system;
            set => this.SetProperty(ref this._system, value);
        }

        public WindowSettings Window
        {
            get => this._window;
            set => this.SetProperty(ref this._window, value);
        }

        public TextEditorSettings TextEditor
        {
            get => this._textEditor;
            set => this.SetProperty(ref this._textEditor, value);
        }

        private SettingsService()
        {
        }

        public static bool Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = string.Empty;
                    using (var reader = new StreamReader(SettingsFilePath, FILE_ENCODING))
                    {
                        json= reader.ReadToEnd();
                    }
                    JsonConvert.PopulateObject(json, Instance);
                }
                else
                {
                    Instance.System.Culture = CultureInfo.CurrentCulture.Name;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                using (var writer = new StreamWriter(SettingsFilePath, false, FILE_ENCODING))
                {
                    writer.Write(json);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
