using Vanara.PInvoke;

namespace MyPad.Models
{
    public class WindowSettings : ModelBase
    {
        private User32_Gdi.WINDOWPLACEMENT? _placement;
        public User32_Gdi.WINDOWPLACEMENT? Placement
        {
            get => this._placement;
            set => this.SetProperty(ref this._placement, value);
        }

        private bool _saveWindowPosition = true;
        public bool SaveWindowPosition
        {
            get => this._saveWindowPosition;
            set => this.SetProperty(ref this._saveWindowPosition, value);
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

        private bool _showSideBar;
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

        private bool _showSingleTab;
        public bool ShowSingleTab
        {
            get => this._showSingleTab;
            set => this.SetProperty(ref this._showSingleTab, value);
        }

        private bool _showFullName = true;
        public bool ShowFullName
        {
            get => this._showFullName;
            set => this.SetProperty(ref this._showFullName, value);
        }

        private bool _useOverlayMessage = true;
        public bool UseOverlayMessage
        {
            get => this._useOverlayMessage;
            set => this.SetProperty(ref this._useOverlayMessage, value);
        }
    }
}


