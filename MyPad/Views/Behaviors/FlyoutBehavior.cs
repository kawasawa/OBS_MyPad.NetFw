using MahApps.Metro.Controls;
using MyLib.Wpf;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace MyPad.Views.Behaviors
{
    public class FlyoutBehavior : Behavior<Flyout>
    {
        public static readonly DependencyProperty CloseByEscProperty = Interactor.RegisterDependencyProperty();

        public bool CloseByEsc
        {
            get => (bool)this.GetValue(CloseByEscProperty);
            set => this.SetValue(CloseByEscProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.PreviewKeyDown += this.AssociatedObject_PreviewKeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.PreviewKeyDown -= this.AssociatedObject_PreviewKeyDown;
        }

        private void AssociatedObject_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    if (this.CloseByEsc && this.AssociatedObject.IsOpen && this.AssociatedObject.IsMouseCaptureWithin == false && Keyboard.Modifiers == ModifierKeys.None)
                    {
                        this.AssociatedObject.IsOpen = false;
                        e.Handled = true;
                    }
                    break;
            }
        }
    }
}