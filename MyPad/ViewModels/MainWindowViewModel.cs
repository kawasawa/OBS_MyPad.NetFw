using Dragablz;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using MyLib.Wpf;
using MyLib.Wpf.Interactions;
using MyPad.Models;
using MyPad.Properties;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace MyPad.ViewModels
{
    public class MainWindowViewModel : ContainerViewModelBase<TextEditorViewModel>
    {
        #region スタティック

        private static int SEQUENSE = 0;

        #endregion

        #region リクエスト

        public InteractionRequest<MessageNotification> MessageRequest { get; } = new InteractionRequest<MessageNotification>();
        public InteractionRequest<OpenFileNotificationEx> OpenFileRequest { get; } = new InteractionRequest<OpenFileNotificationEx>();
        public InteractionRequest<SaveFileNotificationEx> SaveFileRequest { get; } = new InteractionRequest<SaveFileNotificationEx>();
        public InteractionRequest<PrintDocumentNotification> PrintRequest { get; } = new InteractionRequest<PrintDocumentNotification>();
        public InteractionRequest<TransitionNotification> TransitionRequest { get; } = new InteractionRequest<TransitionNotification>();

        #endregion

        #region プロパティ

        public int Sequense { get; } = ++SEQUENSE;

        private bool _isWorking;
        public bool IsWorking
        {
            get => this._isWorking;
            set => this.SetProperty(ref this._isWorking, value);
        }

        private FlowDocument _flowDocument;
        public FlowDocument FlowDocument
        {
            get => this._flowDocument;
            set => this.SetProperty(ref this._flowDocument, value);
        }

        public IEnumerable Assemblies { get; } = ProductInfo.ReferencedAssemblies.OrderBy(x => x.Name);
        public IEnumerable Languages { get; } = new[] { string.Empty }.Concat(Consts.SYNTAX_DEFINITIONS.Keys);
        public IEnumerable Encodings { get; } = Encoding.GetEncodings().Select(x => x.GetEncoding());
        public IEnumerable FontFamilies { get; } = Fonts.SystemFontFamilies;
        public IEnumerable FontSizes { get; } = new[] {
            6, 7, 8, 9, 10, 10.5, 11, 12, 13, 14, 15, 16, 18, 20, 22, 24, 26, 28, 32, 36, 42, 48, 60, 72, 96,
        };
        public IEnumerable Cultures { get; } = new[] {
            new { Description = "English", Name = "en-US" },
            new { Description = "日本語", Name = "ja-JP" },
        };

        #endregion

        #region コマンド

        public ICommand AddCommand
            => new DelegateCommand(() => this.AddContent());

        public ICommand OpenCommand
            => new DelegateCommand(async () => await this.LoadContent());

        public ICommand SaveCommand
            => new DelegateCommand(async () => await this.SaveContent(this.ActiveContent));

        public ICommand SaveAsCommand
            => new DelegateCommand(async () => await this.SaveAsContent(this.ActiveContent));

        public ICommand SaveAllCommand
            => new DelegateCommand(async () =>
            {
                for (var i = 0; i < this.Contents.Count; i++)
                {
                    if (this.Contents[i].IsReadOnly)
                        continue;
                    if (await this.SaveContent(this.Contents[i]) == false)
                        return;
                }
            });

        public ICommand PrintPreviewCommand
            => new DelegateCommand(async () => this.FlowDocument = await this.ActiveContent.CreateFlowDocument());

        public ICommand PrintCommand
            => new DelegateCommand(() => this.PrintRequest.Raise(new PrintDocumentNotification(this.FlowDocument)));

        public ICommand InitializeXshdCommand
            => new DelegateCommand(() =>
            {
                this.MessageRequest.Raise(
                    new MessageNotification(Resources.Message_ConfirmInitializeXshd, MessageKind.CancelableWarning),
                    n =>
                    {
                        if (n.Result == true && ResourceService.CleanUpXshd())
                            ResourceService.InitializeXshd(true);
                    });
            });

        public ICommand CloseCommand
            => new DelegateCommand(async () =>
            {
                if (await this.SaveChangesIfAndRemove(this.ActiveContent) == false)
                    return;
                if (this.Contents.Any() == false)
                    this.AddContent();
            });

        public ICommand CloseAllCommand
            => new DelegateCommand(async () =>
            {
                if (await this.SaveChangesIfAndRemove() == false)
                    return;
                if (this.Contents.Any() == false)
                    this.AddContent();
            });

        public ICommand CloseOtherCommand
            => new DelegateCommand(async () =>
            {
                var currentContent = this.ActiveContent;
                for (var i = this.Contents.Count - 1; 0 <= i; i--)
                {
                    if (this.Contents[i].Equals(currentContent))
                        continue;
                    if (await this.SaveChangesIfAndRemove(this.Contents[i]) == false)
                        return;
                }
            });

        public ICommand ActivateCommand
            => new DelegateCommand<TextEditorViewModel>(content =>
            {
                if (content != null && this.Contents.Contains(content))
                    this.ActiveContent = content;
                else
                    this.TransitionRequest.Raise(new TransitionNotification(TransitionKind.Activate));
            });

        public ICommand ReloadCommand
            => new DelegateCommand<Tuple<Encoding, string>>(async tuple => await this.ReloadContent(this.ActiveContent, tuple.Item1, tuple.Item2));

        public ICommand ActivatedHandler
            => new DelegateCommand<EventArgs>(e => WorkspaceViewModel.Instance.ActiveContent = this);

        public ICommand DropHandler
            => new DelegateCommand<DragEventArgs>(async e =>
            {
                if (e.Data.GetData(DataFormats.FileDrop) is IEnumerable<string> paths && paths.Any())
                {
                    await this.LoadContent(paths);
                    e.Handled = true;
                }
            });

        public ICommand ClosingHandler
            => new DelegateCommand<CancelEventArgs>(async e =>
            {
                if (e.Cancel)
                    return;
                if (await this.SaveChangesIfAndRemove() == false)
                    e.Cancel = true;
            });

        public Delegate ClosingContentHandler
            => new ItemActionCallback(async e =>
            {
                if (e.IsCancelled || !(e.DragablzItem?.DataContext is TextEditorViewModel content))
                    return;
                if (await this.SaveChangesIfAndRemove(content) == false)
                    e.Cancel();
                if (this.Contents.Any() == false)
                    this.AddContent();
            });

        #endregion

        #region メソッド

        public MainWindowViewModel()
        {
        }

        protected override void Dispose(bool disposing)
        {
            this.TransitionRequest.Raise(new TransitionNotification(TransitionKind.Close));
            for (var i = this.Contents.Count - 1; 0 <= i; i--)
                this.RemoveContent(this.Contents[i]);
            base.Dispose(disposing);
        }

        public TextEditorViewModel AddContent()
        {
            var content = this.ContentFactory.Invoke();
            this.AddContent(content);
            return content;
        }

        public void AddContent(TextEditorViewModel content)
        {
            this.Contents.Add(content);
            this.ActiveContent = content;
        }

        public bool RemoveContent(TextEditorViewModel content)
        {
            if (this.Contents.Contains(content) == false)
                return false;

            this.Contents.Remove(content);
            content.Dispose();
            return true;
        }

        public async Task LoadContent(IEnumerable<string> paths = null)
        {
            bool decideConditions(string root, out IEnumerable<string> fileNames, out string filter, out Encoding encoding)
            {
                // 起点位置を設定する
                // - ディレクトリが指定されている場合は、それを起点とする
                // - ファイルが指定されている場合は、それを含む階層を起点とする
                // - いずれでも無い場合は、既定値とする
                if (Directory.Exists(root) == false)
                {
                    if (File.Exists(root))
                        root = Path.GetDirectoryName(root);
                    else
                        root = string.Empty;
                }

                // ダイアログを表示し、ファイルのパスと読み込み条件を選択させる
                var ready = false;
                IEnumerable<string> fn = null;
                string f = null;
                Encoding e = null;
                this.OpenFileRequest.Raise(
                    new OpenFileNotificationEx()
                    {
                        DefaultDirectory = root,
                        Encoding = SettingsService.Instance.System.AutoDetectEncoding ? null : SettingsService.Instance.System.Encoding,
                    },
                    n =>
                    {
                        if (n.Result == true)
                        {
                            ready = true;
                            fn = n.FileNames;
                            f = n.FilterName;
                            e = n.Encoding;
                        }
                    });

                // 戻り値を設定する
                fileNames = fn;
                filter = f;
                encoding = e;
                return ready;
            }

            if (paths == null)
            {
                // パスが指定されていない場合
                // - ファイルパスを選択させて読み込む

                if (decideConditions(null, out var fileNames, out var filter, out var encoding) == false)
                    return;

                foreach (var path in fileNames)
                {
                    var definition = Consts.SYNTAX_DEFINITIONS.ContainsKey(filter) ?
                        Consts.SYNTAX_DEFINITIONS[filter] :
                        Consts.SYNTAX_DEFINITIONS.Values.FirstOrDefault(d => d.Extensions.Contains(Path.GetExtension(path)));
                    var content = await this.ReadFile(path, encoding, definition);
                    if (content != null)
                        this.ActiveContent = content;
                }
            }
            else
            {
                // パスが指定されている場合
                // - ファイルパスの場合は、そのまま読み込む
                // - ディレクトリパスの場合は、ファイルパスを選択させて読み込む

                foreach (var path in paths.Where(path => File.Exists(path)))
                {
                    var encoding = SettingsService.Instance.System.AutoDetectEncoding ? null : SettingsService.Instance.System.Encoding;
                    var definition = Consts.SYNTAX_DEFINITIONS.Values.FirstOrDefault(d => d.Extensions.Contains(Path.GetExtension(path)));
                    var content = await this.ReadFile(path, encoding, definition);
                    if (content != null)
                        this.ActiveContent = content;
                }

                foreach (var path in paths.Where(path => Directory.Exists(path)))
                {
                    if (decideConditions(path, out var fileNames, out var filter, out var encoding) == false)
                        continue;

                    foreach (var fileName in fileNames)
                    {
                        var definition = Consts.SYNTAX_DEFINITIONS.ContainsKey(filter) ?
                            Consts.SYNTAX_DEFINITIONS[filter] :
                            Consts.SYNTAX_DEFINITIONS.Values.FirstOrDefault(d => d.Extensions.Contains(Path.GetExtension(fileName)));
                        var content = await this.ReadFile(fileName, encoding, definition);
                        if (content != null)
                            this.ActiveContent = content;
                    }
                }
            }
        }

        public async Task<bool> ReloadContent(TextEditorViewModel content, Encoding encoding, string lanugage = "")
        {
            var definition =
                string.IsNullOrEmpty(lanugage) ? null :
                Consts.SYNTAX_DEFINITIONS.ContainsKey(lanugage) ? Consts.SYNTAX_DEFINITIONS[lanugage] : null;
            if (content.IsNewFile)
            {
                content.Encoding = encoding;
                content.SyntaxDefinition = definition;
                return true;
            }
            else
            {
                return await this.ReadFile(content.FileName, encoding, definition) != null;
            }
        }

        private async Task<TextEditorViewModel> ReadFile(string path, Encoding encoding, XshdSyntaxDefinition definition)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("空のパスが指定されています。", nameof(path));

            var sameContent = this.Contents.FirstOrDefault(m => m.FileName.Equals(path));
            if (sameContent != null)
            {
                // 同名ファイルを占有しているコンテンツをアクティブにする
                this.ActiveContent = sameContent;

                // 文字コードが異なる場合はリロードする
                if (sameContent.Encoding?.Equals(encoding) != true)
                {
                    // 変更がある場合は確認する
                    if (sameContent.IsModified)
                    {
                        var r = false;
                        this.MessageRequest.Raise(
                            new MessageNotification(Resources.Message_ConfirmDiscardChanges, MessageKind.CancelableWarning),
                            n => r = n.Result == true);
                        if (r == false)
                            return null;
                    }

                    // 指定された文字コードでリロードする
                    this.IsWorking = true;
                    await sameContent.Reload(encoding);
                    this.IsWorking = false;
                }

                // シンタックス定義を設定する
                sameContent.SyntaxDefinition = definition;
                return sameContent;
            }
            else
            {
                // 他のウィンドウが同名ファイルを占有している場合は処理を委譲する
                if (WorkspaceViewModel.Instance.DelegateReloadContent(this, path, encoding))
                    return null;

                // ファイルサイズを確認する
                var info = new FileInfo(path);
                if (AppConfig.SizeThreshold <= info.Length)
                {
                    var ready = false;
                    this.MessageRequest.Raise(
                        new MessageNotification(Resources.Message_ConfirmOpenLargeFile, MessageKind.CancelableWarning),
                        n => ready = n.Result == true);
                    if (ready == false)
                        return null;
                }

                // ストリームを取得する
                FileStream stream = null;
                try
                {
                    // 可能であれば読み取りと書き込みの権限を取得する
                    stream = info.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                }
                catch
                {
                    // 失敗した場合は読み取り権限のみを取得する
                    try
                    {
                        stream = info.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        this.MessageRequest.Raise(new MessageNotification(Resources.Message_NotifyFileLocked, path, MessageKind.Warning));
                    }
                    catch (Exception e)
                    {
                        this.MessageRequest.Raise(new MessageNotification(e.Message, MessageKind.Error));
                        return null;
                    }
                }

                // ファイルを読み込む
                this.IsWorking = true;
                var content = this.AddContent();
                await content.Load(stream, encoding);
                this.IsWorking = false;

                // シンタックス定義を設定する
                content.SyntaxDefinition = definition;
                return content;
            }
        }

        private async Task<bool> SaveContent(TextEditorViewModel content)
        {
            if (content.IsNewFile)
                return await this.SaveAsContent(content);
            else
                return await this.WriteFile(content, content.FileName, content.Encoding, content.SyntaxDefinition);
        }

        private async Task<bool> SaveAsContent(TextEditorViewModel content)
        {
            this.ActiveContent = content;

            var ready = false;
            var path = content.FileName;
            var filter = content.SyntaxDefinition?.Name;
            var encoding = content.Encoding;
            this.SaveFileRequest.Raise(
                new SaveFileNotificationEx()
                {
                    DefaultDirectory = content.IsNewFile ? string.Empty : Path.GetDirectoryName(path),
                    FileName = Path.GetFileName(path),
                    FilterName = filter,
                    Encoding = encoding,
                },
                n =>
                {
                    if (n.Result == true)
                    {
                        ready = true;
                        path = n.FileName;
                        filter = n.FilterName;
                        encoding = n.Encoding;
                    }
                });
            if (ready == false)
                return false;

            var definition = Consts.SYNTAX_DEFINITIONS.Values.FirstOrDefault(d => d.Extensions.Contains(Path.GetExtension(path)));
            return await this.WriteFile(content, path, encoding, definition);
        }

        public async Task<bool> SaveChangesIfAndRemove(TextEditorViewModel content)
        {
            if (content.IsModified)
            {
                this.ActiveContent = content;

                bool? result = null;
                this.MessageRequest.Raise(
                    new MessageNotification(Resources.Message_ConfirmSaveChanges, content.FileName, MessageKind.CancelableConfirm),
                    n => result = n.Result);
                switch (result)
                {
                    case true:
                        if (await this.SaveContent(content) == false)
                            return false;
                        break;
                    case false:
                        break;
                    default:
                        return false;
                }
            }

            this.RemoveContent(content);
            return true;
        }

        public async Task<bool> SaveChangesIfAndRemove()
        {
            for (var i = this.Contents.Count - 1; 0 <= i; i--)
            {
                if (await this.SaveChangesIfAndRemove(this.Contents[i]) == false)
                    return false;
            }
            return true;
        }

        private async Task<bool> WriteFile(TextEditorViewModel content, string path, Encoding encoding, XshdSyntaxDefinition definition)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("空のパスが渡されました。", nameof(path));

            var sameContent = this.Contents.FirstOrDefault(m => m.FileName.Equals(path));
            if (sameContent != null)
            {
                // 他のコンテンツが同名ファイルを占有している場合は何もせず終了する
                if (sameContent.Equals(content) == false)
                {
                    this.ActiveContent = sameContent;
                    this.MessageRequest.Raise(new MessageNotification(Resources.Message_NotifyFileLocked, sameContent.FileName, MessageKind.Warning));
                    return false;
                }

                // ファイルに保存する
                this.IsWorking = true;
                await sameContent.Save(encoding);
                this.IsWorking = false;

                // シンタックス定義を設定する
                sameContent.SyntaxDefinition = definition;
                return true;
            }
            else
            {
                // 他のウィンドウが同名ファイルを占有している場合は何もせず終了する
                var otherSameContent = WorkspaceViewModel.Instance.DelegateActivateContent(this, path);
                if (otherSameContent != null)
                {
                    this.MessageRequest.Raise(new MessageNotification(Resources.Message_NotifyFileLocked, otherSameContent.FileName, MessageKind.Warning));
                    return false;
                }

                // ストリームを取得する
                FileStream stream = null;
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                }
                catch (Exception e)
                {
                    this.MessageRequest.Raise(new MessageNotification(e.Message, MessageKind.Error));
                    return false;
                }

                // ファイルに保存する
                this.IsWorking = true;
                await content.SaveAs(stream, encoding);
                this.IsWorking = false;

                // シンタックス定義を設定する
                content.SyntaxDefinition = definition;
                return true;
            }
        }

        #endregion
    }
}
