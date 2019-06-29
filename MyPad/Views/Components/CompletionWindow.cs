using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Input;

namespace MyPad.Views.Components
{
    public class CompletionWindow : ICSharpCode.AvalonEdit.CodeCompletion.CompletionWindow
    {
        public bool IsCompletionDecided
            => this.CompletionList.ListBox.Items.Count == 1 && this.CompletionList.SelectedItem?.Text.Equals(this.TextArea.Document.GetText(this.StartOffset, this.EndOffset - this.StartOffset)) == true;

        public CompletionWindow(ICSharpCode.AvalonEdit.Editing.TextArea textAera, IEnumerable<CompletionData> completionData)
            : base(textAera)
        {
            // オフセットの計算
            // 開始位置は前要素の境界位置とする
            // 終了位置は次要素の境界位置とする
            var offset = textAera.Document.GetOffset(textAera.Caret.Line, textAera.Caret.Column);
            var start = TextUtilities.GetNextCaretPosition(textAera.Document, offset, LogicalDirection.Backward, CaretPositioningMode.WordBorderOrSymbol);
            var end = TextUtilities.GetNextCaretPosition(textAera.Document, start, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol);

            // どちらかがテキストの端に到達した場合
            if (start < 0 || end < 0)
            {
                // 開始位置は現在位置と同値とする
                start = offset;
                // 開始位置がテキストの先端の場合
                if (start <= 0)
                {
                    // 終了位置は次要素の境界位置とする
                    end = TextUtilities.GetNextCaretPosition(textAera.Document, start, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol);
                    // 終了位置がテキストの終端に到達した、または次要素が空白で構成される場合
                    if (end < 0 || string.IsNullOrWhiteSpace(textAera.Document.GetText(start, end - start)))
                        // 終了位置は開始位置と同値とする
                        end = start;
                }
                // 終了位置がテキストの終端の場合
                else
                {
                    // 終了位置は開始位置と同値とする
                    end = start;
                }
            }
            // 前要素が記号で構成される場合
            else if (start + 1 == end && end == offset && TextUtilities.GetCharacterClass(textAera.Document.GetText(start, 1).First()) == CharacterClass.Other)
            {
                // 開始位置は現在位置と同値とする
                start = offset;
                // 終了位置は次要素の境界位置とする
                end = TextUtilities.GetNextCaretPosition(textAera.Document, start, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol);
                // 終了位置がテキストの終端に到達している、または次要素が空白で構成される場合
                if (end < 0 || string.IsNullOrWhiteSpace(textAera.Document.GetText(start, end - start)))
                    // 終了位置は開始位置と同値とする
                    end = start;
            }
            // 次要素が空白で構成される場合
            else if (string.IsNullOrWhiteSpace(textAera.Document.GetText(start, end - start)))
            {
                // 開始位置と終了位置は現在位置と同値とする
                start = end = offset;
            }

            // オフセットの設定
            this.StartOffset = start;
            this.EndOffset = end;

            // キーワードリストの設定
            completionData.ForEach(data => this.CompletionList.CompletionData.Add(data));
            this.CompletionList.SelectItem(start < end ? textAera.Document.GetText(start, end - start) : string.Empty);
            if (this.CompletionList.ListBox.ItemsSource.OfType<ICompletionData>().Any() == false)
                this.CompletionList.SelectItem(string.Empty);

            // プロパティの設定
            this.CloseWhenCaretAtBeginning = true;

            // イベントの購読
            this.TextArea.TextEntering += this.TextArea_TextEntering;
            this.TextArea.TextEntered += this.TextArea_TextEntered;
            this.TextArea.Caret.PositionChanged += this.Caret_PositionChanged;
        }

        protected override void OnClosed(EventArgs e)
        {
            this.TextArea.TextEntering -= this.TextArea_TextEntering;
            this.TextArea.TextEntered -= this.TextArea_TextEntered;
            this.TextArea.Caret.PositionChanged -= this.Caret_PositionChanged;
            base.OnClosed(e);
        }

        private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length != 1)
                this.Close();

            switch (TextUtilities.GetCharacterClass(e.Text.First()))
            {
                case CharacterClass.IdentifierPart:
                    break;
                // スペースで決定は便利だが邪魔になることも...？
                //case CharacterClass.Whitespace:
                //    this.CompletionList.SelectedItem?.Complete(this.TextArea, new AnchorSegment(this.TextArea.Document, this.StartOffset, this.EndOffset - this.StartOffset), e);
                //    this.Close();
                //    break;
                default:
                    this.Close();
                    break;
            }
        }

        private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (this.CompletionList.ListBox.Items.Count == 0 || this.IsCompletionDecided)
            {
                if (this.IsVisible)
                    this.Hide();
            }
        }

        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            var caret = (Caret)sender;
            if (this.StartOffset < caret.Offset && caret.Offset <= this.EndOffset)
            {
                if (0 < this.CompletionList.ListBox.Items.Count && this.IsCompletionDecided == false)
                {
                    if (this.IsVisible == false)
                        this.Show();
                }
            }
        }

        protected override void ActivateParentWindow()
        {
            // 既定の処理を打ち消す
        }
    }
}
