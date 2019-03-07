using ICSharpCode.AvalonEdit.Search;
using MyLib.Wpf;
using System.Windows.Input;

namespace MyPad.Views.Components
{
    public static class TextEditorCommands
    {
        public static readonly ICommand ConvertToNarrow
            = Interactor.CreateRoutedCommand<TextArea>();

        public static readonly ICommand ConvertToWide
            = Interactor.CreateRoutedCommand<TextArea>();

        public static readonly ICommand ZoomIn
            = Interactor.CreateRoutedCommand<TextArea>(new InputGestureCollection { new KeyGesture(Key.OemPlus, ModifierKeys.Control) });

        public static readonly ICommand ZoomOut
            = Interactor.CreateRoutedCommand<TextArea>(new InputGestureCollection { new KeyGesture(Key.OemMinus, ModifierKeys.Control) });

        public static readonly ICommand ZoomReset
            = Interactor.CreateRoutedCommand<TextArea>(new InputGestureCollection { new KeyGesture(Key.D0, ModifierKeys.Control) });

        public static readonly ICommand Completion
            = Interactor.CreateRoutedCommand<TextArea>(new InputGestureCollection { new KeyGesture(Key.Space, ModifierKeys.Control) });

        public static readonly ICommand ReplaceNext
            = Interactor.CreateRoutedCommand<SearchPanel>(new InputGestureCollection { new KeyGesture(Key.R, ModifierKeys.Alt) });

        public static readonly ICommand ReplaceAll
            = Interactor.CreateRoutedCommand<SearchPanel>( new InputGestureCollection { new KeyGesture(Key.A, ModifierKeys.Alt) });
    }
}
