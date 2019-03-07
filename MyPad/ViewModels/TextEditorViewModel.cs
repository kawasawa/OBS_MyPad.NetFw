namespace MyPad.ViewModels
{
    public class TextEditorViewModel : TextEditorViewModelCore
    {
        private bool _overstrikeMode;
        public bool OverstrikeMode
        {
            get => this._overstrikeMode;
            set => this.SetProperty(ref this._overstrikeMode, value);
        }

        private double _actualFontSize;
        public double ActualFontSize
        {
            get => this._actualFontSize;
            set => this.SetProperty(ref this._actualFontSize, value);
        }

        private int _zoomIncrement;
        public int ZoomIncrement
        {
            get => this._zoomIncrement;
            set => this.SetProperty(ref this._zoomIncrement, value);
        }

        private int _line;
        public int Line
        {
            get => this._line;
            set => this.SetProperty(ref this._line, value);
        }

        private int _column;
        public int Column
        {
            get => this._column;
            set => this.SetProperty(ref this._column, value);
        }

        private int _visualColumn;
        public int VisualColumn
        {
            get => this._visualColumn;
            set =>  this.SetProperty(ref this._visualColumn, value);
        }

        private int _visualLength;
        public int VisualLength
        {
            get => this._visualLength;
            set => this.SetProperty(ref this._visualLength, value);
        }

        private int _textLength;
        public int TextLength
        {
            get => this._textLength;
            set => this.SetProperty(ref this._textLength, value);
        }

        private int _selectionLength;
        public int SelectionLength
        {
            get => this._selectionLength;
            set => this.SetProperty(ref this._selectionLength, value);
        }

        private int _selectionStart;
        public int SelectionStart
        {
            get => this._selectionStart;
            set => this.SetProperty(ref this._selectionStart, value);
        }

        private int _selectionEnd;
        public int SelectionEnd
        {
            get => this._selectionEnd;
            set => this.SetProperty(ref this._selectionEnd, value);
        }

        private int _selectionStartLine;
        public int SelectionStartLine
        {
            get => this._selectionStartLine;
            set => this.SetProperty(ref this._selectionStartLine, value);
        }

        private int _selectionEndLine;
        public int SelectionEndLine
        {
            get => this._selectionEndLine;
            set => this.SetProperty(ref this._selectionEndLine, value);
        }

        private int _selectionLineCount;
        public int SelectionLineCount
        {
            get => this._selectionLineCount;
            set => this.SetProperty(ref this._selectionLineCount, value);
        }

        private string _selectedText;
        public string SelectedText
        {
            get => this._selectedText;
            set => this.SetProperty(ref this._selectedText, value);
        }

        private string _charName;
        public string CharName
        {
            get => this._charName;
            set => this.SetProperty(ref this._charName, value);
        }

        private bool _isAtEndOfLine;
        public bool IsAtEndOfLine
        {
            get => this._isAtEndOfLine;
            set => this.SetProperty(ref this._isAtEndOfLine, value);
        }

        private bool _isInVirtualSpace;
        public bool IsInVirtualSpace
        {
            get => this._isInVirtualSpace;
            set => this.SetProperty(ref this._isInVirtualSpace, value);
        }

        private bool _enableAutoCompletion;
        public bool EnableAutoCompletion
        {
            get => this._enableAutoCompletion;
            set => this.SetProperty(ref this._enableAutoCompletion, value);
        }
    }
}
