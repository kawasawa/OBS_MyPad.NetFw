using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MyPad.Models
{
    public class SystemSettings : ModelBase
    {
        private bool _enableNotificationIcon = true;
        public bool EnableNotificationIcon
        {
            get => this._enableNotificationIcon;
            set => this.SetProperty(ref this._enableNotificationIcon, value);
        }

        private bool _enableResident = true;
        public bool EnableResident
        {
            get => this._enableResident;
            set => this.SetProperty(ref this._enableResident, value);
        }

        private string _culture;
        public string Culture
        {
            get => this._culture;
            set
            {
                this.SetProperty(ref this._culture, value);
                ResourceService.Instance.SetCulture(value);
            }
        }

        private bool _autoDetectEncoding = true;
        public bool AutoDetectEncoding
        {
            get => this._autoDetectEncoding;
            set => this.SetProperty(ref this._autoDetectEncoding, value);
        }

        private bool _emphasisOnQuality;
        public bool EmphasisOnQuality
        {
            get => this._emphasisOnQuality;
            set => this.SetProperty(ref this._emphasisOnQuality, value);
        }

        [JsonIgnore]
        public Encoding Encoding
        {
            get => Encoding.GetEncoding(this._encodingCodePage);
            set
            {
                if (this.SetProperty(ref this._encodingCodePage, value.CodePage, nameof(this.EncodingCodePage)))
                    this.RaisePropertyChanged(nameof(this.Encoding));
            }
        }

        private int _encodingCodePage = Encoding.UTF8.CodePage;
        public int EncodingCodePage
        {
            get => this._encodingCodePage;
            set => this.SetProperty(ref this._encodingCodePage, value);
        }

        private bool _enableAutoSave = true;
        public bool EnableAutoSave
        {
            get => this._enableAutoSave;
            set => this.SetProperty(ref this._enableAutoSave, value);
        }

        private int _autoSaveInterval = 10;
        [Range(1, int.MaxValue)]
        public int AutoSaveInterval
        {
            get => this._autoSaveInterval;
            set => this.SetProperty(ref this._autoSaveInterval, value);
        }

        private int _clipboardHistoryCount = 20;
        [Range(1, int.MaxValue)]
        public int ClipboardHistoryCount
        {
            get => this._clipboardHistoryCount;
            set => this.SetProperty(ref this._clipboardHistoryCount, value);
        }

        private string _syntaxDefinitionName = string.Empty;
        public string SyntaxDefinitionName
        {
            get => this._syntaxDefinitionName;
            set => this.SetProperty(ref this._syntaxDefinitionName, value);
        }

        private string _fileExplorerRoot = string.Empty;
        public string FileExplorerRoot
        {
            get => this._fileExplorerRoot;
            set => this.SetProperty(ref this._fileExplorerRoot, value);
        }
    }
}


