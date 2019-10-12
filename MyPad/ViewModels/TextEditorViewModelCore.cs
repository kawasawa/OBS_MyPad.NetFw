using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Utils;
using MyLib;
using MyLib.Wpf.Interactions;
using MyPad.Models;
using MyPad.Properties;
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Vanara.PInvoke;

namespace MyPad.ViewModels
{
    public abstract class TextEditorViewModelCore : ViewModelBase
    {
        private static readonly bool _ENABLED_AUTO_SAVE = SettingsService.Instance.System.EnableAutoSave;
        private static readonly TimeSpan _AUTO_SAVE_INTERVAL = new TimeSpan(0, SettingsService.Instance.System.AutoSaveInterval, 0);
        private static int _SEQUENCE = 0;

        private readonly DispatcherTimer _saveTimer = new DispatcherTimer();
        private Tuple<string, ITextSourceVersion> _temporary;

        public int Sequense { get; } = ++_SEQUENCE;
        public bool IsNewFile => this.FileStream == null;
        public FileInfo FileInfo => this.IsNewFile ? null : new FileInfo(this.FileName);
        public string FileName => this.FileStream?.Name ?? $"{AppConfig.InitialFileName}-{this.Sequense}";

        public string ShortFileName
        {
            get
            {
                var lpszShortPath = new StringBuilder(1024);
                Kernel32.GetShortPathName(this.FileName, lpszShortPath, (uint)lpszShortPath.Capacity);
                return string.Join(string.Empty, lpszShortPath).TrimEnd(char.MinValue);
            }
        }

        public string FileType
        {
            get
            {
                var psfi = new Shell32.SHFILEINFO();
                Shell32.SHGetFileInfo(this.FileName, 0, ref psfi, Marshal.SizeOf(psfi), Shell32.SHGFI.SHGFI_TYPENAME);
                return psfi.szTypeName;
            }
        }

        public string FileOwner
            => File.GetAccessControl(this.FileName).GetOwner(typeof(NTAccount)).Value;

        private FileStream _fileStream;
        public FileStream FileStream
        {
            get => this._fileStream;
            protected set
            {
                if (this.SetProperty(ref this._fileStream, value))
                {
                    this.RaisePropertyChanged(nameof(this.IsNewFile));
                    this.RaisePropertyChanged(nameof(this.FileInfo));
                    this.RaisePropertyChanged(nameof(this.FileName));
                    this.RaisePropertyChanged(nameof(this.ShortFileName));
                    this.RaisePropertyChanged(nameof(this.FileType));
                    this.RaisePropertyChanged(nameof(this.FileOwner));
                }
            }
        }

        private TextDocument _document;
        public TextDocument Document
        {
            get => this._document;
            private set => this.SetProperty(ref this._document, value);
        }

        private Encoding _encoding;
        public Encoding Encoding
        {
            get => this._encoding;
            set => this.SetProperty(ref this._encoding, value);
        }

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get => this._isReadOnly;
            set => this.SetProperty(ref this._isReadOnly, value);
        }

        private bool _isModified;
        public bool IsModified
        {
            get => this._isModified;
            set => this.SetProperty(ref this._isModified, value);
        }

        private XshdSyntaxDefinition _syntaxDefinition;
        public XshdSyntaxDefinition SyntaxDefinition
        {
            get => this._syntaxDefinition;
            set => this.SetProperty(ref this._syntaxDefinition, value);
        }

        public TextEditorViewModelCore()
        {
            this._saveTimer.Tick += this.AutoSaveTimer_Tick;
            this.Document = new TextDocument();
            this.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            this._saveTimer.Tick -= this.AutoSaveTimer_Tick;
            this.SafeDeleteTemporary();
            this.FileStream?.Dispose();
            this.FileStream = null;
            base.Dispose(disposing);
        }

        public async void Clear()
        {
            await this.SuspendAutoSaveTimer(async () =>
            {
                // ストリームを解放する
                this.FileStream?.Dispose();
                this.FileStream = null;

                // テキストをクリアする
                await Application.Current.Dispatcher.InvokeAsync(() => this.Document.Text = string.Empty);
                this.Document.UndoStack.ClearAll();

                // 関連要素をクリアする
                this.Document.FileName = string.Empty;
                this.Encoding = SettingsService.Instance.System.Encoding;
                this.IsReadOnly = false;
                this.IsModified = false;
                this.SyntaxDefinition =
                    Consts.SYNTAX_DEFINITIONS.ContainsKey(SettingsService.Instance.System.SyntaxDefinitionName) ?
                    Consts.SYNTAX_DEFINITIONS[SettingsService.Instance.System.SyntaxDefinitionName] :
                    null;

                // 一時ファイルを削除する
                await Task.Run(() => this.SafeDeleteTemporary());
            });
        }

        public async Task Load(FileStream stream, Encoding encoding = null)
        {
            if (this.FileStream?.Equals(stream) != true)
            {
                this.FileStream?.Dispose();
                this.FileStream = stream;
            }
            await this.Reload(encoding);
        }

        public async Task Reload(Encoding encoding = null)
        {
            if (this.FileStream == null)
                throw new InvalidOperationException($"{nameof(this.FileStream)} が null です。");

            await this.SuspendAutoSaveTimer(async () =>
            {
                // ストリームからバイト配列を読み取る
                var bytes = new byte[this.FileStream.Length];
                this.FileStream.Position = 0;
                await this.FileStream.ReadAsync(bytes, 0, bytes.Length);

                // 文字コードを推定する
                if (encoding == null)
                    encoding = await Task.Run(() => TextFileHelper.DetectEncodingFast(bytes) ?? SettingsService.Instance.System.Encoding);

                // バイト配列をテキストを変換する
                var text = await Task.Run(() => encoding.GetString(bytes));

                // テキストを設定する
                // HACK: UndoStack のリセット
                // TextDocument.Text へ代入後に ClearAll() を実行したところ IsModified の変更が通知されなくなった。
                // 正確には ClearAll() の実行後も UndoStack 内の未変更点が更新されず、変更済みとして扱われているのだと思われる。
                // 仕方ないので、処理前にクリアして一時的にサスペンドし、処理後にレジュームする。
                // (なお、同期処理で実装すると正常に動作する。TextDocument はスレッドを監視しているため、この辺りが怪しい気がする。)
                this.Document.UndoStack.ClearAll();
                var sizeLimit = this.Document.UndoStack.SizeLimit;
                this.Document.UndoStack.SizeLimit = 0;
                await Application.Current.Dispatcher.InvokeAsync(() => this.Document.Text = text);
                this.Document.UndoStack.SizeLimit = sizeLimit;

                // 関連要素を設定する
                this.Document.FileName = this.FileName;
                this.Encoding = encoding;
                this.IsReadOnly = !this.FileStream.CanWrite;
                this.IsModified = false;

                // 一時ファイルを削除する
                await Task.Run(() => this.SafeDeleteTemporary());
            });
        }

        public async Task Save(Encoding encoding)
        {
            if (this.FileStream == null)
                throw new InvalidOperationException($"{nameof(this.FileStream)} が null です。");

            await this.SuspendAutoSaveTimer(async () =>
            {
                // テキストをバイト配列に変換する
                var bytes = await Application.Current.Dispatcher.InvokeAsync(() => encoding.GetBytes(this.Document.Text));

                // ストリームに書き込む
                this.FileStream.Position = 0;
                this.FileStream.SetLength(0);
                await this.FileStream.WriteAsync(bytes, 0, bytes.Length);
                this.FileStream.Flush();

                // 関連要素を設定する
                this.Encoding = encoding;
                this.IsReadOnly = false;
                this.IsModified = false;

                // 一時ファイルを削除する
                await Task.Run(() => this.SafeDeleteTemporary());
            });
        }

        public async Task SaveAs(FileStream stream, Encoding encoding)
        {
            if (this.FileStream?.Equals(stream) != true)
            {
                this.FileStream?.Dispose();
                this.FileStream = stream;
            }
            await this.Save(encoding);
        }

        public FlowDocument CreateFlowDocument()
        {
            IHighlighter highlighter = null;
            if (this.SyntaxDefinition != null)
            {
                var definition = HighlightingLoader.Load(this.SyntaxDefinition, HighlightingManager.Instance);
                highlighter = new DocumentHighlighter(this.Document, definition);
            }
            var block = DocumentPrinter.ConvertTextDocumentToBlock(this.Document, highlighter);
            highlighter?.Dispose();

            var flowDocument = new FlowDocument(block);
            flowDocument.FontFamily = SettingsService.Instance.TextEditor.FontFamily;
            flowDocument.FontSize = SettingsService.Instance.TextEditor.ActualFontSize;
            flowDocument.Background = Brushes.White;
            flowDocument.Foreground = Brushes.Black;
            flowDocument.PagePadding = new Thickness(50);
            flowDocument.ColumnGap = 0;
            return flowDocument;
        }

        private string ConvertToCompressedBase64(string str)
        {
            using (var memory = new MemoryStream())
            {
                using (var deflate = new DeflateStream(memory, CompressionLevel.Optimal))
                {
                    var bytes = Encoding.UTF8.GetBytes(str);
                    deflate.Write(bytes, 0, bytes.Length);
                }
                return Convert.ToBase64String(memory.ToArray());
            }
        }

        private async void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            if (_ENABLED_AUTO_SAVE == false || this.IsModified == false || this.Document.Version == this._temporary?.Item2)
                return;

            await this.SuspendAutoSaveTimer(async () =>
            {
                var path = Path.Combine(Consts.CURRENT_TEMPORARY, this.ConvertToCompressedBase64(this.IsNewFile ? this.FileName : this.ShortFileName).Replace("/", "-"));
                var bytes = Array.Empty<byte>();

                try
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => bytes = this.Encoding.GetBytes(this.Document.Text));
                }
                catch (Exception ex)
                {
                    Logger.Write(LogLevel.Warn, $"自動保存に失敗しました。テキストをバイト配列に変換できませんでした。: Path={path}", ex);
                    return;
                }

                var result = await Task.Run(() =>
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        File.WriteAllBytes(path, bytes);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Write(LogLevel.Warn, $"自動保存に失敗しました。一時ファイルへの書き込みに失敗しました。: Path={path}", ex);
                        return false;
                    }
                });
                if (result == false)
                    return;

                this._temporary = new Tuple<string, ITextSourceVersion>(path, this.Document.Version);
                WorkspaceViewModel.Instance.NotifyRequest.Raise(new MessageNotification(Resources.Message_NotifyAutoSaved, $"{Path.GetFileName(this.FileName)}{Environment.NewLine}{Path.GetFileName(path)}"));
            });
        }

        private void SafeDeleteTemporary()
        {
            try
            {
                if (File.Exists(this._temporary?.Item1))
                    File.Delete(this._temporary.Item1);
            }
            catch (Exception e)
            {
                Logger.Write(LogLevel.Warn, $"一時ファイルの削除に失敗しました。: Path={this._temporary?.Item1}", e);
            }
        }

        private async Task SuspendAutoSaveTimer(Func<Task> func)
        {
            this._saveTimer.Stop();
            await func.Invoke();
            this._saveTimer.Interval = _AUTO_SAVE_INTERVAL;
            this._saveTimer.Start();
        }
    }
}
