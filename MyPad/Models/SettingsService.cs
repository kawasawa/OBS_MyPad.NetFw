using MyLib;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MyPad.Models
{
    public sealed class SettingsService : ModelBase
    {
        public static readonly Encoding FileEncoding = new UTF8Encoding(true);
        public static readonly string SettingsFilePath = Path.Combine(ProductInfo.Roaming, $"Settings.json");

        public static SettingsService Instance { get; } = new SettingsService();

        private SystemSettings _system = new SystemSettings();
        public SystemSettings System
        {
            get => this._system;
            set => this.SetProperty(ref this._system, value);
        }

        private TextEditorSettings _textEditor = new TextEditorSettings();
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
                    using (var reader = new StreamReader(SettingsFilePath, FileEncoding))
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
            catch (Exception e)
            {
                Logger.Write(LogLevel.Warn, $"設定ファイルの読み込みに失敗しました。: Path={SettingsFilePath}", e);
                return false;
            }
        }

        public bool Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                using (var writer = new StreamWriter(SettingsFilePath, false, FileEncoding))
                {
                    writer.Write(json);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Write(LogLevel.Warn, $"設定ファイルの保存に失敗しました。: Path={SettingsFilePath}", e);
                return false;
            }
        }
    }
}
