using AutoMapper;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Utils;
using MyLib.Wpf;
using MyPad.Models;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace MyPad.Views.Components
{
    public class TextEditor : ICSharpCode.AvalonEdit.TextEditor
    {
        #region プロパティ

        private static readonly DependencyPropertyKey VisualLengthPropertyKey = Interactor.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey TextLengthPropertyKey = Interactor.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey SelectionLengthPropertyKey = Interactor.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey SelectionStartPropertyKey = Interactor.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey SelectionEndPropertyKey = Interactor.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey SelectionStartLinePropertyKey = Interactor.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey SelectionEndLinePropertyKey = Interactor.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey SelectionLineCountPropertyKey = Interactor.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey SelectedTextPropertyKey = Interactor.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey CharNamePropertyKey = Interactor.RegisterReadOnlyDependencyProperty(new PropertyMetadata(TextUtilities.GetControlCharacterName(char.MinValue)));
        private static readonly DependencyPropertyKey IsAtEndOfLinePropertyKey = Interactor.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey IsInVirtualSpacePropertyKey = Interactor.RegisterReadOnlyDependencyProperty();

        public static readonly DependencyProperty SettingsProperty
            = Interactor.RegisterDependencyProperty(
                new PropertyMetadata(null, (obj, e) => ((TextEditor)obj).ApplySettings((TextEditorSettings)e.NewValue)));
        public static readonly DependencyProperty SyntaxDefinitionProperty 
            = Interactor.RegisterDependencyProperty(
                new PropertyMetadata(null, (obj, e) =>
                {
                    var textEditor = (TextEditor)obj;
                    var syntaxDefinition = (XshdSyntaxDefinition)e.NewValue;
                    textEditor.SyntaxHighlighting = syntaxDefinition != null ? HighlightingLoader.Load(syntaxDefinition, HighlightingManager.Instance) : null;
                    textEditor.TextArea.ApplySyntaxDefinition(syntaxDefinition);
                }));
        public static readonly DependencyProperty ActualFontSizeProperty
            = Interactor.RegisterDependencyProperty(
                new PropertyMetadata(TextArea.ActualFontSizeProperty.DefaultMetadata.DefaultValue),
                TextArea.ActualFontSizeProperty.IsValidValue);
        public static readonly DependencyProperty ZoomIncrementProperty
            = Interactor.RegisterDependencyProperty(
                new PropertyMetadata(TextArea.ZoomIncrementProperty.DefaultMetadata.DefaultValue),
                TextArea.ZoomIncrementProperty.IsValidValue);
        public static readonly DependencyProperty LineProperty
            = Interactor.RegisterDependencyProperty(
                new PropertyMetadata(1, (obj, e) =>
                {
                    var self = (TextEditor)obj;
                    if (self.IsLoaded)
                        self.TextArea.Caret.Line = (int)e.NewValue;
                }));
        public static readonly DependencyProperty ColumnProperty
            = Interactor.RegisterDependencyProperty(
                new PropertyMetadata(1, (obj, e) =>
                {
                    var self = (TextEditor)obj;
                    if (self.IsLoaded)
                        self.TextArea.Caret.Column = (int)e.NewValue;
                }));
        public static readonly DependencyProperty VisualColumnProperty
            = Interactor.RegisterDependencyProperty(
                new PropertyMetadata(1, (obj, e) =>
                {
                    var self = (TextEditor)obj;
                    if (self.IsLoaded)
                        self.TextArea.Caret.VisualColumn = (int)e.NewValue;
                }));
        public static readonly DependencyProperty VisualLengthProperty = VisualLengthPropertyKey.DependencyProperty;
        public static readonly DependencyProperty TextLengthProperty = TextLengthPropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectionLengthProperty = SelectionLengthPropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectionStartProperty = SelectionStartPropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectionEndProperty = SelectionEndPropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectionStartLineProperty = SelectionStartLinePropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectionEndLineProperty = SelectionEndLinePropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectionLineCountProperty = SelectionLineCountPropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectedTextProperty = SelectedTextPropertyKey.DependencyProperty;
        public static readonly DependencyProperty CharNameProperty = CharNamePropertyKey.DependencyProperty;
        public static readonly DependencyProperty IsAtEndOfLineProperty = IsAtEndOfLinePropertyKey.DependencyProperty;
        public static readonly DependencyProperty IsInVirtualSpaceProperty = IsInVirtualSpacePropertyKey.DependencyProperty;
        public static readonly DependencyProperty OverstrikeModeProperty
            = Interactor.RegisterDependencyProperty(
                new PropertyMetadata(TextArea.OverstrikeModeProperty.DefaultMetadata.DefaultValue),
                TextArea.OverstrikeModeProperty.IsValidValue);
        public static readonly DependencyProperty EnableAutoCompletionProperty
            = Interactor.RegisterDependencyProperty(
                new PropertyMetadata(TextArea.EnableAutoCompletionProperty.DefaultMetadata.DefaultValue),
                TextArea.EnableAutoCompletionProperty.IsValidValue);

        public TextEditorSettings Settings
        {
            get => (TextEditorSettings)this.GetValue(SettingsProperty);
            set => this.SetValue(SettingsProperty, value);
        }

        public XshdSyntaxDefinition SyntaxDefinition
        {
            get => (XshdSyntaxDefinition)this.GetValue(SyntaxDefinitionProperty);
            set => this.SetValue(SyntaxDefinitionProperty, value);
        }

        public double ActualFontSize
        {
            get => (double)this.GetValue(ActualFontSizeProperty);
            set => this.SetValue(ActualFontSizeProperty, value);
        }

        public int ZoomIncrement
        {
            get => (int)this.GetValue(ZoomIncrementProperty);
            set => this.SetValue(ZoomIncrementProperty, value);
        }

        public int Line
        {
            get => (int)this.GetValue(LineProperty);
            set => this.SetValue(LineProperty, value);
        }

        public int Column
        {
            get => (int)this.GetValue(ColumnProperty);
            set => this.SetValue(ColumnProperty, value);
        }

        public int VisualColumn
        {
            get => (int)this.GetValue(VisualColumnProperty);
            set => this.SetValue(VisualColumnProperty, value);
        }

        public int VisualLength
        {
            get => (int)this.GetValue(VisualLengthProperty);
            private set => this.SetValue(VisualLengthPropertyKey, value);
        }

        public int TextLength
        {
            get => (int)this.GetValue(TextLengthProperty);
            private set => this.SetValue(TextLengthPropertyKey, value);
        }

        public new int SelectionLength
        {
            get => (int)this.GetValue(SelectionLengthProperty);
            private set => this.SetValue(SelectionLengthPropertyKey, value);
        }

        public new int SelectionStart
        {
            get => (int)this.GetValue(SelectionStartProperty);
            private set => this.SetValue(SelectionStartPropertyKey, value);
        }

        public int SelectionEnd
        {
            get => (int)this.GetValue(SelectionEndProperty);
            private set => this.SetValue(SelectionEndPropertyKey, value);
        }

        public int SelectionStartLine
        {
            get => (int)this.GetValue(SelectionStartLineProperty);
            private set => this.SetValue(SelectionStartLinePropertyKey, value);
        }

        public int SelectionEndLine
        {
            get => (int)this.GetValue(SelectionEndLineProperty);
            private set => this.SetValue(SelectionEndLinePropertyKey, value);
        }

        public int SelectionLineCount
        {
            get => (int)this.GetValue(SelectionLineCountProperty);
            private set => this.SetValue(SelectionLineCountPropertyKey, value);
        }

        public new string SelectedText
        {
            get => (string)this.GetValue(SelectedTextProperty);
            private set => this.SetValue(SelectedTextPropertyKey, value);
        }

        public string CharName
        {
            get => (string)this.GetValue(CharNameProperty);
            private set => this.SetValue(CharNamePropertyKey, value);
        }

        public bool IsAtEndOfLine
        {
            get => (bool)this.GetValue(IsAtEndOfLineProperty);
            private set => this.SetValue(IsAtEndOfLinePropertyKey, value);
        }

        public bool IsInVirtualSpace
        {
            get => (bool)this.GetValue(IsInVirtualSpaceProperty);
            private set => this.SetValue(IsInVirtualSpacePropertyKey, value);
        }

        public bool OverstrikeMode
        {
            get => (bool)this.GetValue(OverstrikeModeProperty);
            set => this.SetValue(OverstrikeModeProperty, value);
        }

        public bool EnableAutoCompletion
        {
            get => (bool)this.GetValue(EnableAutoCompletionProperty);
            set => this.SetValue(EnableAutoCompletionProperty, value);
        }

        public new TextArea TextArea => base.TextArea as TextArea;

        #endregion

        #region メンバ

        private int _totalDelimiterLength;
        private bool _isInCaretPositionChangedHandler;

        private readonly IMapper _mapper
            = new MapperConfiguration(e =>
            {
                e.CreateMap<TextEditorSettings, TextEditor>();
                e.CreateMap<TextEditorSettings, TextEditorOptions>();
            }
            ).CreateMapper();

        #endregion

        #region メソッド

        public TextEditor()
            : base(new TextArea())
        {
            this.TextArea.Caret.PositionChanged += this.Caret_PositionChanged;
            this.TextArea.SelectionChanged += this.TextArea_SelectionChanged;
            this.TextArea.OverstrikeModeChanged += this.TextArea_OverstrikeModeChanged;
        }

        public void Redraw()
            => this.TextArea.Redraw();

        public void ZoomIn()
            => this.TextArea.ZoomIn();

        public void ZoomOut()
            => this.TextArea.ZoomOut();

        public void ZoomReset()
            => this.TextArea.ZoomReset();

        public void ScrollToCaret()
            => this.ScrollTo(this.Line, this.Column);

        private void ApplySettings(TextEditorSettings settings)
        {
            try { this._mapper.Map(settings, this); } catch { }
            try { this._mapper.Map(settings, this.Options); } catch { }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            switch (e.Property.Name)
            {
                case nameof(this.Settings):
                    if (e.OldValue != null)
                        PropertyChangedWeakEventManager.RemoveListener((INotifyPropertyChanged)e.OldValue, this);
                    if (e.NewValue != null)
                        PropertyChangedWeakEventManager.AddListener((INotifyPropertyChanged)e.NewValue, this);
                    break;
                case nameof(this.Document):
                    if (e.OldValue != null)
                    {
                        PropertyChangedWeakEventManager.RemoveListener((INotifyPropertyChanged)e.OldValue, this);
                    }
                    if (e.NewValue != null)
                    {
                        PropertyChangedWeakEventManager.AddListener((INotifyPropertyChanged)e.NewValue, this);
                        var document = (TextDocument)e.NewValue;
                        this._totalDelimiterLength = document.Lines.Sum(line => line.DelimiterLength);
                        this.TextLength = document.TextLength;
                        this.VisualLength = this.TextLength - this._totalDelimiterLength;
                    }
                    break;
                case nameof(this.ActualFontSize):
                    this.TextArea.ActualFontSize = (double)e.NewValue;
                    break;
                case nameof(this.ZoomIncrement):
                    this.TextArea.ZoomIncrement = (int)e.NewValue;
                    break;
                case nameof(this.OverstrikeMode):
                    this.TextArea.OverstrikeMode = (bool)e.NewValue;
                    break;
                case nameof(this.EnableAutoCompletion):
                    this.TextArea.EnableAutoCompletion = (bool)e.NewValue;
                    break;
            }
            base.OnPropertyChanged(e);
        }

        protected override bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (managerType == typeof(PropertyChangedWeakEventManager) && e is PropertyChangedEventArgs args)
            {
                switch (sender)
                {
                    case TextEditorSettings settings:
                        this.ApplySettings(settings);
                        return true;

                    case TextDocument document:
                        switch (args.PropertyName)
                        {
                            case nameof(document.TextLength):
                                this.TextLength = document.TextLength;
                                this.VisualLength = this.TextLength - this._totalDelimiterLength;
                                break;
                            case nameof(document.LineCount):
                                this._totalDelimiterLength = document.Lines.Sum(line => line.DelimiterLength);
                                this.VisualLength = this.TextLength - this._totalDelimiterLength;
                                break;
                        }
                        return true;
                }
            }
            return base.ReceiveWeakEvent(managerType, sender, e);
        }

        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            if (this._isInCaretPositionChangedHandler)
                return;

            this._isInCaretPositionChangedHandler = true;

            try
            {
                var caret = (ICSharpCode.AvalonEdit.Editing.Caret)sender;
                this.Line = caret.Line;
                this.Column = caret.Column;
                this.VisualColumn = caret.VisualColumn;
                this.IsAtEndOfLine = caret.Position.IsAtEndOfLine;
                this.IsInVirtualSpace = caret.IsInVirtualSpace;

                if (this.Document != null)
                {
                    var offset = this.Document.GetOffset(caret.Line, caret.Column);
                    var character = offset < this.Document.TextLength ? this.Document.GetCharAt(offset) : char.MinValue;
                    this.CharName = this.IsInVirtualSpace ? "Virtual" : TextUtilities.GetControlCharacterName(character);
                }
                else
                {
                    this.CharName = this.IsInVirtualSpace ? "Virtual" : TextUtilities.GetControlCharacterName(char.MinValue);
                }
            }
            finally
            {
                this._isInCaretPositionChangedHandler = false;
            }
        }

        private void TextArea_SelectionChanged(object sender, EventArgs e)
        {
            var textArea = (ICSharpCode.AvalonEdit.Editing.TextArea)sender;
            var selection = textArea.Selection;
            if (selection.IsEmpty)
            {
                var caret = textArea.Caret;
                this.SelectionStart = caret.Offset;
                this.SelectionEnd = caret.Offset;
                this.SelectionLength = 0;
                this.SelectionStartLine = 0;
                this.SelectionEndLine = 0;
                this.SelectionLineCount = 0;
                this.SelectedText = string.Empty;
            }
            else
            {
                var segment = selection.SurroundingSegment;
                this.SelectionStart = segment.Offset;
                this.SelectionEnd = segment.EndOffset;
                this.SelectionLength = segment.Length;
                this.SelectionStartLine = selection.StartPosition.Line;
                this.SelectionEndLine = selection.EndPosition.Line;
                this.SelectionLineCount = Math.Abs(selection.EndPosition.Line - selection.StartPosition.Line) + 1;
                this.SelectedText = textArea.Document.GetText(segment);
            }
        }

        private void TextArea_OverstrikeModeChanged(object sender, EventArgs e)
        {
            var textArea = (ICSharpCode.AvalonEdit.Editing.TextArea)sender;
            this.OverstrikeMode = textArea.OverstrikeMode;
        }

        #endregion
    }
}