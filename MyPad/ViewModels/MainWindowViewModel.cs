using Dragablz;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using MyLib;
using MyLib.Wpf.Interactions;
using MyPad.Models;
using MyPad.Properties;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

namespace MyPad.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region スタティック

        private static int _SEQUENCE = 0;

        #endregion

        #region リクエスト

        public InteractionRequest<MessageNotification> MessageRequest { get; } = new InteractionRequest<MessageNotification>();
        public InteractionRequest<OpenFileNotificationEx> OpenFileRequest { get; } = new InteractionRequest<OpenFileNotificationEx>();
        public InteractionRequest<SaveFileNotificationEx> SaveFileRequest { get; } = new InteractionRequest<SaveFileNotificationEx>();
        public InteractionRequest<PrintDocumentNotification> PrintRequest { get; } = new InteractionRequest<PrintDocumentNotification>();
        public InteractionRequest<TransitionNotification> TransitionRequest { get; } = new InteractionRequest<TransitionNotification>();

        #endregion

        #region プロパティ

        public ObservableCollection<TextEditorViewModel> Editors { get; } = new ObservableCollection<TextEditorViewModel>();
        public ObservableCollection<TerminalViewModel> Terminals { get; } = new ObservableCollection<TerminalViewModel>();
        public ObservableCollection<FileNodeViewModel> FileNodes { get; } = new ObservableCollection<FileNodeViewModel>();

        public GrepViewModel Grep { get; } = new GrepViewModel();

        public int Sequense { get; } = ++_SEQUENCE;

        public bool IsModified => this.Editors.Any(c => c.IsModified);

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

        private TextEditorViewModel _activeEditor;
        public TextEditorViewModel ActiveEditor
        {
            get => this._activeEditor;
            set => this.SetProperty(ref this._activeEditor, value);
        }

        private TerminalViewModel _activeTerminal;
        public TerminalViewModel ActiveTerminal
        {
            get => this._activeTerminal;
            set => this.SetProperty(ref this._activeTerminal, value);
        }

        public Func<TextEditorViewModel> EditorFactory =>
            () => new TextEditorViewModel();

        public Func<TerminalViewModel> TerminalFactory =>
            () =>
            {
                var terminal = new TerminalViewModel();
                terminal.Start();
                terminal.Disposed += this.Terminal_Disposed;
                return terminal;
            };

        #endregion

        #region コマンド

        public ICommand ReloadCommand
            => new DelegateCommand<Tuple<Encoding, string>>(async tuple => await this.ReloadEditor(this.ActiveEditor, tuple.Item1, tuple.Item2));

        public ICommand OpenCommand
            => new DelegateCommand(async () => await this.LoadEditor());

        public ICommand SaveCommand
            => new DelegateCommand(async () => await this.SaveEditor(this.ActiveEditor));

        public ICommand SaveAsCommand
            => new DelegateCommand(async () => await this.SaveAsEditor(this.ActiveEditor));

        public ICommand SaveAllCommand
            => new DelegateCommand(async () =>
            {
                for (var i = 0; i < this.Editors.Count; i++)
                {
                    if (this.Editors[i].IsReadOnly)
                        continue;
                    if (await this.SaveEditor(this.Editors[i]) == false)
                        return;
                }
            });

        public ICommand PrintPreviewCommand
            => new DelegateCommand(() => this.FlowDocument = this.ActiveEditor.CreateFlowDocument());

        public ICommand PrintCommand
            => new DelegateCommand(() => this.PrintRequest.Raise(new PrintDocumentNotification(this.FlowDocument)));

        public ICommand InitializeXshdCommand
            => new DelegateCommand(() =>
            {
                this.MessageRequest.Raise(
                    new MessageNotification(Resources.Message_ConfirmInitializeXshd, MessageKind.CancelableWarning),
                    n =>
                    {
                        if (n.Result == true)
                            ResourceService.InitializeXshd(true);
                    });
            });

        public ICommand AddEditorCommand
            => new DelegateCommand(() => this.AddEditor());

        public ICommand CloseEditorCommand
            => new DelegateCommand(async () =>
            {
                if (await this.SaveChangesIfAndRemove(this.ActiveEditor) == false)
                    return;
                if (this.Editors.Any() == false)
                    this.AddEditor();
                this.ForceGC();
            });

        public ICommand CloseAllEditorCommand
            => new DelegateCommand(async () =>
            {
                if (await this.SaveChangesIfAndRemoveAll() == false)
                    return;
                if (this.Editors.Any() == false)
                    this.AddEditor();
                this.ForceGC();
            });

        public ICommand CloseOtherEditorCommand
            => new DelegateCommand(async () =>
            {
                var currentEditor = this.ActiveEditor;
                for (var i = this.Editors.Count - 1; 0 <= i; i--)
                {
                    if (this.Editors[i].Equals(currentEditor))
                        continue;
                    if (await this.SaveChangesIfAndRemove(this.Editors[i]) == false)
                        return;
                }
                this.ForceGC();
            });

        public ICommand ActivateEditorCommand
            => new DelegateCommand<TextEditorViewModel>(editor =>
            {
                if (editor != null && this.Editors.Contains(editor))
                    this.ActiveEditor = editor;
                else
                    this.TransitionRequest.Raise(new TransitionNotification(TransitionKind.Activate));
            });

        public ICommand AddTerminalCommand
            => new DelegateCommand(() => this.AddTerminal());

        public ICommand CloseTerminalCommand
            => new DelegateCommand(() =>
            {
                if (this.ActiveTerminal != null)
                    this.RemoveTerminal(this.ActiveTerminal);
                this.ForceGC();
            });

        public ICommand CloseAllTerminalCommand
            => new DelegateCommand(() =>
            {
                for (var i = this.Terminals.Count - 1; 0 <= i; i--)
                    this.RemoveTerminal(this.Terminals[i]);
                this.ForceGC();
            });

        public ICommand CloseOtherTerminalCommand
            => new DelegateCommand(() =>
            {
                var currentTerminal = this.ActiveTerminal;
                for (var i = this.Terminals.Count - 1; 0 <= i; i--)
                {
                    if (this.Terminals[i].Equals(currentTerminal) == false)
                        this.RemoveTerminal(this.Terminals[i]);
                }
                this.ForceGC();
            });

        public ICommand ActivateTerminalCommand
            => new DelegateCommand<TerminalViewModel>(terminal =>
            {
                if (terminal != null && this.Terminals.Contains(terminal))
                    this.ActiveTerminal = terminal;
                else
                    this.TransitionRequest.Raise(new TransitionNotification(TransitionKind.Activate));
            });

        public ICommand ActivatedHandler
            => new DelegateCommand<EventArgs>(e =>
            {
                if (WorkspaceViewModel.Instance != null)
                    WorkspaceViewModel.Instance.ActiveWindow = this;
            });

        public ICommand DropHandler
            => new DelegateCommand<DragEventArgs>(async e =>
            {
                if (e.Data.GetData(DataFormats.FileDrop) is IEnumerable<string> paths && paths.Any())
                {
                    await this.LoadEditor(paths);
                    e.Handled = true;
                }
            });

        public ICommand ClosingHandler
            => new DelegateCommand<CancelEventArgs>(async e =>
            {
                if (e.Cancel || this.IsModified == false)
                    return;

                e.Cancel = true;
                if (await this.SaveChangesIfAndRemoveAll())
                    this.Dispose();
            });

        public Delegate ClosingEditorHandler
            => new ItemActionCallback(e =>
            {
                if (e.IsCancelled || !(e.DragablzItem?.DataContext is TextEditorViewModel editor))
                    return;

                this.ActiveEditor = editor;
                this.CloseEditorCommand.Execute(null);
            });

        public Delegate ClosingTerminalHandler
            => new ItemActionCallback(e =>
            {
                if (e.IsCancelled || !(e.DragablzItem?.DataContext is TerminalViewModel terminal))
                    return;

                this.ActiveTerminal = terminal;
                this.CloseTerminalCommand.Execute(null);
            });

        #endregion

        #region メソッド

        public MainWindowViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(this.Editors, new object());
            BindingOperations.EnableCollectionSynchronization(this.Terminals, new object());
            BindingOperations.EnableCollectionSynchronization(this.FileNodes, new object());
            this.RefreshFileNodes();
        }

        protected override void Dispose(bool disposing)
        {
            for (var i = this.Editors.Count - 1; 0 <= i; i--)
                this.RemoveEditor(this.Editors[i]);
            for (var i = this.Terminals.Count - 1; 0 <= i; i--)
                this.RemoveTerminal(this.Terminals[i]);
            this.FileNodes.Clear();

            base.Dispose(disposing);
            this.ForceGC();
        }

        public TextEditorViewModel AddEditor()
        {
            var editor = this.EditorFactory.Invoke();
            this.AddEditor(editor);
            return editor;
        }

        public void AddEditor(TextEditorViewModel editor)
        {
            this.Editors.Add(editor);
            this.ActiveEditor = editor;
        }

        public void RemoveEditor(TextEditorViewModel editor)
        {
            if (this.Editors.Contains(editor) == false)
                return;

            this.Editors.Remove(editor);
            editor.Dispose();
        }
        
        public async Task LoadEditor(IEnumerable<string> paths = null)
        {
            bool decideConditions(string root, out IEnumerable<string> fileNames, out string filter, out Encoding encoding, out bool isReadOnly)
            {
                // 起点位置を設定する
                // - ディレクトリが指定されている場合は、それを起点とする
                // - ファイルが指定されている場合は、それを含む階層を起点とする
                // - いずれでも無い場合は、既定値とする
                if (Directory.Exists(root) == false)
                    root = File.Exists(root) ? Path.GetDirectoryName(root) : string.Empty;

                // ダイアログを表示し、ファイルのパスと読み込み条件を選択させる
                var ready = false;
                IEnumerable<string> fn = null;
                string f = null;
                Encoding e = null;
                var r = false;
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
                            r = n.IsReadOnly;
                        }
                    });

                // 戻り値を設定する
                fileNames = fn;
                filter = f;
                encoding = e;
                isReadOnly = r;
                return ready;
            }

            if (paths == null)
            {
                // パスが指定されていない場合
                // - ファイルパスを選択させて読み込む

                if (decideConditions(null, out var fileNames, out var filter, out var encoding, out var isReadOnly) == false)
                    return;

                foreach (var path in fileNames)
                {
                    var definition = Consts.SYNTAX_DEFINITIONS.ContainsKey(filter) ?
                        Consts.SYNTAX_DEFINITIONS[filter] :
                        Consts.SYNTAX_DEFINITIONS.Values.FirstOrDefault(d => d.Extensions.Contains(Path.GetExtension(path)));
                    var editor = await this.ReadFile(path, encoding, definition, isReadOnly);
                    if (editor != null)
                        this.ActiveEditor = editor;
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
                    var editor = await this.ReadFile(path, encoding, definition);
                    if (editor != null)
                        this.ActiveEditor = editor;
                }

                foreach (var path in paths.Where(path => Directory.Exists(path)))
                {
                    if (decideConditions(path, out var fileNames, out var filter, out var encoding, out var isReadOnly) == false)
                        continue;

                    foreach (var fileName in fileNames)
                    {
                        var definition = Consts.SYNTAX_DEFINITIONS.ContainsKey(filter) ?
                            Consts.SYNTAX_DEFINITIONS[filter] :
                            Consts.SYNTAX_DEFINITIONS.Values.FirstOrDefault(d => d.Extensions.Contains(Path.GetExtension(fileName)));
                        var editor = await this.ReadFile(fileName, encoding, definition, isReadOnly);
                        if (editor != null)
                            this.ActiveEditor = editor;
                    }
                }
            }
        }

        public async Task<bool> ReloadEditor(TextEditorViewModel editor, Encoding encoding, string lanugage = "")
        {
            var definition =
                string.IsNullOrEmpty(lanugage) ? null :
                Consts.SYNTAX_DEFINITIONS.ContainsKey(lanugage) ? Consts.SYNTAX_DEFINITIONS[lanugage] : null;
            if (editor.IsNewFile)
            {
                editor.Encoding = encoding;
                editor.SyntaxDefinition = definition;
                return true;
            }
            else
            {
                return await this.ReadFile(editor.FileName, encoding, definition, editor.IsReadOnly) != null;
            }
        }

        private async Task<TextEditorViewModel> ReadFile(string path, Encoding encoding, XshdSyntaxDefinition definition, bool isReadOnly = false)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("空のパスが指定されています。", nameof(path));

            var sameEditor = this.Editors.FirstOrDefault(m => m.FileName.Equals(path));
            if (sameEditor != null)
            {
                // 同名ファイルを占有しているコンテンツをアクティブにする
                this.ActiveEditor = sameEditor;

                // 文字コードが異なる場合はリロードする
                if (sameEditor.Encoding?.Equals(encoding) != true)
                {
                    // 変更がある場合は確認する
                    if (sameEditor.IsModified)
                    {
                        var r = false;
                        this.MessageRequest.Raise(
                            new MessageNotification(Resources.Message_ConfirmDiscardChanges, sameEditor.FileName, MessageKind.Confirm),
                            n => r = n.Result == true);
                        if (r == false)
                            return null;
                    }

                    // 指定された文字コードでリロードする
                    try
                    {
                        this.IsWorking = true;
                        await sameEditor.Reload(encoding);
                    }
                    catch (Exception e)
                    {
                        Logger.Write(LogLevel.Error, $"ファイルの再読み込みに失敗しました。: Path={path}, Encoding={encoding.EncodingName}", e);
                        this.MessageRequest.Raise(new MessageNotification(e.Message, MessageKind.Error));
                        return null;
                    }
                    finally
                    {
                        this.IsWorking = false;
                    }
                }

                // シンタックス定義を設定する
                sameEditor.SyntaxDefinition = definition;
                return sameEditor;
            }
            else
            {
                // 他のウィンドウが同名ファイルを占有している場合は処理を委譲する
                if (WorkspaceViewModel.Instance.DelegateReloadEditor(this, path, encoding))
                    return null;

                // ファイルサイズを確認する
                const long LARGE_FILE_SIZE = 100L * 1024 * 1024;
                var info = new FileInfo(path);
                if (LARGE_FILE_SIZE <= info.Length)
                {
                    var ready = false;
                    this.MessageRequest.Raise(
                        new MessageNotification(Resources.Message_ConfirmOpenLargeFile, MessageKind.CancelableWarning),
                        n => ready = n.Result == true);
                    if (ready == false)
                        return null;
                }

                // 可能であれば書き込み権限を取得する
                FileStream stream = null;
                if (isReadOnly == false)
                {
                    try
                    {
                        stream = info.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                    }
                    catch
                    {
                        // ここでのエラーは無視する
                    }
                }

                // 取得できない場合は、読み取り権限のみを取得する
                if (stream == null)
                {
                    try
                    {
                        stream = info.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        this.MessageRequest.Raise(new MessageNotification(Resources.Message_NotifyFileLocked, path, MessageKind.Warning));
                    }
                    catch (Exception e)
                    {
                        Logger.Write(LogLevel.Error, $"ファイルの読み取り権限の取得に失敗しました。: Path={path}, Encoding={encoding.EncodingName}", e);
                        this.MessageRequest.Raise(new MessageNotification(e.Message, MessageKind.Error));
                        return null;
                    }
                }

                // ファイルを読み込む
                TextEditorViewModel editor;
                try
                {
                    this.IsWorking = true;
                    editor = this.AddEditor();
                    await editor.Load(stream, encoding);
                }
                catch (Exception e)
                {
                    Logger.Write(LogLevel.Error, $"ファイルの読み込みに失敗しました。: Path={path}, Encoding={encoding.EncodingName}", e);
                    this.MessageRequest.Raise(new MessageNotification(e.Message, MessageKind.Error));
                    return null;
                }
                finally
                {
                    this.IsWorking = false;
                }

                // シンタックス定義を設定する
                editor.SyntaxDefinition = definition;
                return editor;
            }
        }

        private async Task<bool> SaveEditor(TextEditorViewModel editor)
        {
            if (editor.IsNewFile || editor.IsReadOnly)
                return await this.SaveAsEditor(editor);
            else
                return await this.WriteFile(editor, editor.FileName, editor.Encoding, editor.SyntaxDefinition);
        }

        private async Task<bool> SaveAsEditor(TextEditorViewModel editor)
        {
            this.ActiveEditor = editor;

            var ready = false;
            var path = editor.FileName;
            var filter = editor.SyntaxDefinition?.Name;
            var encoding = editor.Encoding;
            this.SaveFileRequest.Raise(
                new SaveFileNotificationEx()
                {
                    DefaultDirectory = editor.IsNewFile ? string.Empty : Path.GetDirectoryName(path),
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
            return await this.WriteFile(editor, path, encoding, definition);
        }

        public async Task<bool> SaveChangesIfAndRemove(TextEditorViewModel editor)
        {
            if (editor.IsModified)
            {
                this.ActiveEditor = editor;

                bool? result = null;
                this.MessageRequest.Raise(
                    new MessageNotification(Resources.Message_ConfirmSaveChanges, editor.FileName, MessageKind.CancelableConfirm),
                    n => result = n.Result);
                switch (result)
                {
                    case true:
                        if (await this.SaveEditor(editor) == false)
                            return false;
                        break;
                    case false:
                        break;
                    default:
                        return false;
                }
            }

            this.RemoveEditor(editor);
            return true;
        }

        public async Task<bool> SaveChangesIfAndRemoveAll()
        {
            for (var i = this.Editors.Count - 1; 0 <= i; i--)
            {
                if (await this.SaveChangesIfAndRemove(this.Editors[i]) == false)
                    return false;
            }
            return true;
        }

        private async Task<bool> WriteFile(TextEditorViewModel editor, string path, Encoding encoding, XshdSyntaxDefinition definition)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("空のパスが渡されました。", nameof(path));

            var sameEditor = this.Editors.FirstOrDefault(m => m.FileName.Equals(path));
            if (sameEditor != null)
            {
                // 他のコンテンツが同名ファイルを占有している場合は、保存せずに終了する
                if (sameEditor.Equals(editor) == false || sameEditor.IsReadOnly)
                {
                    this.ActiveEditor = sameEditor;
                    this.MessageRequest.Raise(new MessageNotification(Resources.Message_NotifyFileLocked, sameEditor.FileName, MessageKind.Warning));
                    return false;
                }

                // ファイルに保存する
                try
                {
                    this.IsWorking = true;
                    await sameEditor.Save(encoding);
                }
                catch (Exception e)
                {
                    Logger.Write(LogLevel.Error, $"ファイルの上書き保存に失敗しました。: Path={path}, Encoding={encoding.EncodingName}", e);
                    this.MessageRequest.Raise(new MessageNotification(e.Message, MessageKind.Error));
                    return false;
                }
                finally
                {
                    this.IsWorking = false;
                }

                // シンタックス定義を設定する
                sameEditor.SyntaxDefinition = definition;
                return true;
            }
            else
            {
                // 他のウィンドウが同名ファイルを占有している場合は、保存せずに終了する
                var otherSameEditor = WorkspaceViewModel.Instance.DelegateActivateEditor(this, path);
                if (otherSameEditor != null)
                {
                    this.MessageRequest.Raise(new MessageNotification(Resources.Message_NotifyFileLocked, otherSameEditor.FileName, MessageKind.Warning));
                    return false;
                }

                // ストリームを取得し、ファイルに保存する
                FileStream stream = null;
                try
                {
                    this.IsWorking = true;
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                    await editor.SaveAs(stream, encoding);
                }
                catch (Exception e)
                {
                    Logger.Write(LogLevel.Error, $"ファイルの新規保存に失敗しました。: Path={path}, Encoding={encoding.EncodingName}", e);
                    this.MessageRequest.Raise(new MessageNotification(e.Message, MessageKind.Error));
                    return false;
                }
                finally
                {
                    this.IsWorking = false;
                }

                // シンタックス定義を設定する
                editor.SyntaxDefinition = definition;
                return true;
            }
        }

        public TerminalViewModel AddTerminal()
        {
            var terminal = this.TerminalFactory.Invoke();
            this.AddTerminal(terminal);
            return terminal;
        }

        public void AddTerminal(TerminalViewModel terminal)
        {
            this.Terminals.Add(terminal);
            this.ActiveTerminal = terminal;
        }

        public void RemoveTerminal(TerminalViewModel terminal)
        {
            if (this.Terminals.Contains(terminal) == false)
                return;

            this.Terminals.Remove(terminal);
            terminal.Disposed -= this.Terminal_Disposed;
            terminal.Dispose();
        }

        private void RefreshFileNodes()
        {
            var root = SettingsService.Instance.System.FileExplorerRoot;
            if (string.IsNullOrEmpty(root) || Directory.Exists(root) == false)
                root = Consts.DEFAULT_FILE_EXPLORER_ROOT;

            var node = new FileNodeViewModel(root);
            node.IsExpanded = true;
            this.FileNodes.Clear();
            this.FileNodes.Add(node);
        }

        private void Terminal_Disposed(object sender, EventArgs e)
        {
            this.RemoveTerminal((TerminalViewModel)sender);
        }

        #endregion
    }
}
