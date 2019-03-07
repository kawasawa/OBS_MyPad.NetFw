using System;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace MyPad.ViewModels
{
    public abstract class ContainerViewModelBase<ContentType> : ViewModelBase
        where ContentType : new()
    {
        private ContentType _activeContent;
        public ContentType ActiveContent
        {
            get => this._activeContent;
            set => this.SetProperty(ref this._activeContent, value);
        }

        public ObservableCollection<ContentType> Contents { get; } = new ObservableCollection<ContentType>();

        public virtual Func<ContentType> ContentFactory { get; } = () => new ContentType();

        public ContainerViewModelBase()
        {
            BindingOperations.EnableCollectionSynchronization(this.Contents, new object());
        }
    }
}