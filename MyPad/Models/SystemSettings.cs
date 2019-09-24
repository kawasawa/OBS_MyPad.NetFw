using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Vanara.PInvoke;

namespace MyPad.Models
{
    public class SystemSettings : ModelBase
    {
        private User32_Gdi.WINDOWPLACEMENT? _windowPlacement;
        public User32_Gdi.WINDOWPLACEMENT? WindowPlacement
        {
            get => this._windowPlacement;
            set => this.SetProperty(ref this._windowPlacement, value);
        }

        private bool _saveWindowPlacement = true;
        public bool SaveWindowPlacement
        {
            get => this._saveWindowPlacement;
            set => this.SetProperty(ref this._saveWindowPlacement, value);
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

        private bool _autoDetectEncoding = true;
        public bool AutoDetectEncoding
        {
            get => this._autoDetectEncoding;
            set => this.SetProperty(ref this._autoDetectEncoding, value);
        }

        private bool _detectEncodingStrict;
        public bool DetectEncodingStrict
        {
            get => this._detectEncodingStrict;
            set => this.SetProperty(ref this._detectEncodingStrict, value);
        }

        private string _syntaxDefinitionName = string.Empty;
        public string SyntaxDefinitionName
        {
            get => this._syntaxDefinitionName;
            set => this.SetProperty(ref this._syntaxDefinitionName, value);
        }

        private bool _useOverlayMessage = true;
        public bool UseOverlayMessage
        {
            get => this._useOverlayMessage;
            set => this.SetProperty(ref this._useOverlayMessage, value);
        }

        private bool _showFullName = true;
        public bool ShowFullName
        {
            get => this._showFullName;
            set => this.SetProperty(ref this._showFullName, value);
        }

        private bool _showSingleTab;
        public bool ShowSingleTab
        {
            get => this._showSingleTab;
            set => this.SetProperty(ref this._showSingleTab, value);
        }

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

        private int _clipboardHistorySize = 20;
        [Range(1, int.MaxValue)]
        public int ClipboardHistorySize
        {
            get => this._clipboardHistorySize;
            set => this.SetProperty(ref this._clipboardHistorySize, value);
        }

        private string _fileExplorerRoot = string.Empty;
        public string FileExplorerRoot
        {
            get => this._fileExplorerRoot;
            set => this.SetProperty(ref this._fileExplorerRoot, value);
        }

        private bool _showMenuBar = true;
        public bool ShowMenuBar
        {
            get => this._showMenuBar;
            set => this.SetProperty(ref this._showMenuBar, value);
        }

        private bool _showToolBar = true;
        public bool ShowToolBar
        {
            get => this._showToolBar;
            set => this.SetProperty(ref this._showToolBar, value);
        }

        private bool _showSideBar = true;
        public bool ShowSideBar
        {
            get => this._showSideBar;
            set => this.SetProperty(ref this._showSideBar, value);
        }

        private bool _showStatusBar = true;
        public bool ShowStatusBar
        {
            get => this._showStatusBar;
            set => this.SetProperty(ref this._showStatusBar, value);
        }
    }
}


