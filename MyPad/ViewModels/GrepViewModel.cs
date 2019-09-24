using MyLib.Wpf.Interactions;
using MyPad.Models;
using MyPad.Properties;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;

namespace MyPad.ViewModels
{
    public class GrepViewModel : ViewModelBase
    {
        public InteractionRequest<MessageNotification> MessageRequest { get; } = new InteractionRequest<MessageNotification>();
        public InteractionRequest<OpenFileNotification> OpenDirectoryRequest { get; } = new InteractionRequest<OpenFileNotification>();

        public ObservableCollection<object> Results { get; } = new ObservableCollection<object>();

        private bool _isWorking;
        public bool IsWorking
        {
            get => this._isWorking;
            set
            {
                if (this.SetProperty(ref this._isWorking, value))
                    this.RaisePropertyChanged(nameof(this.CanGrep));
            }
        }

        private string _searchText;
        [Required(ErrorMessageResourceName = nameof(Resources.Message_Required), ErrorMessageResourceType = typeof(Resources))]
        public string SearchText
        {
            get => this._searchText;
            set
            {
                if (this.SetProperty(ref this._searchText, value))
                    this.RaisePropertyChanged(nameof(this.CanGrep));
            }
        }

        private string _rootPath;
        [Required(ErrorMessageResourceName = nameof(Resources.Message_Required), ErrorMessageResourceType = typeof(Resources))]
        public string RootPath
        {
            get => this._rootPath;
            set
            {
                if (this.SetProperty(ref this._rootPath, value))
                    this.RaisePropertyChanged(nameof(this.CanGrep));
            }
        }

        private string _searchPattern = "*";
        public string SearchPattern
        {
            get => this._searchPattern;
            set => this.SetProperty(ref this._searchPattern, value);
        }

        private bool _allDirectories = true;
        public bool AllDirectories
        {
            get => this._allDirectories;
            set => this.SetProperty(ref this._allDirectories, value);
        }

        private bool _ignoreCase = true;
        public bool IgnoreCase
        {
            get => this._ignoreCase;
            set => this.SetProperty(ref this._ignoreCase, value);
        }

        private bool _useRegex;
        public bool UseRegex
        {
            get => this._useRegex;
            set => this.SetProperty(ref this._useRegex, value);
        }

        private bool _autoDetectEncoding = SettingsService.Instance.System.AutoDetectEncoding;
        public bool AutoDetectEncoding
        {
            get => this._autoDetectEncoding;
            set => this.SetProperty(ref this._autoDetectEncoding, value);
        }

        private Encoding _encoding = SettingsService.Instance.System.Encoding;
        public Encoding Encoding
        {
            get => this._encoding;
            set
            {
                if (this.SetProperty(ref this._encoding, value))
                    this.RaisePropertyChanged(nameof(this.CanGrep));
            }
        }

        public bool CanGrep
            => this.IsWorking == false &&
                string.IsNullOrEmpty(this.SearchText) == false && 
                string.IsNullOrEmpty(this.RootPath) == false &&
                this.Encoding != null;

        public ICommand GrepCommand
            => new DelegateCommand(async () =>
                {
                    if (Directory.Exists(this.RootPath) == false)
                    {
                        this.MessageRequest.Raise(new MessageNotification(Resources.Message_NotifyDirectoryNotFound, MessageKind.Error));
                        return;
                    }

                    this.IsWorking = true;
                    this.Results.Clear();
                    await TextFileHelper.Grep(
                        this.Results,
                        this.SearchText,
                        this.RootPath, 
                        this.Encoding,
                        this.SearchPattern,
                        this.AllDirectories,
                        this.IgnoreCase, 
                        this.UseRegex,
                        this.AutoDetectEncoding,
                        AppConfig.GrepBufferSize);
                    this.IsWorking = false;
                }, 
                () => this.CanGrep)
            .ObservesProperty(() => this.CanGrep);

        public ICommand SelectRootPathCommand
            => new DelegateCommand(() =>
            {
                this.OpenDirectoryRequest.Raise(
                    new OpenFileNotification(),
                    n =>
                    {
                        if (n.Result == true)
                            this.RootPath = n.FileName;
                    });
            });

        public GrepViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(this.Results, new object());
        }
    }
}
