using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace MyPad.ViewModels
{
    public class TerminalViewModel : ViewModelBase
    {
        private static int _SEQUENCE = 0;
        private const string _COMSPEC = "COMSPEC";
        private Process _terminal;
        private StreamWriter _writer;
        private bool _opening = true;
        private bool _closing;

        public int Sequense { get; } = ++_SEQUENCE;
        public string TerminalName => $"Terminal-{this.Sequense}";

        public ObservableCollection<string> Histories { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> DataLines { get; } = new ObservableCollection<string>();
        public string DataLinesText => string.Join(Environment.NewLine, this.DataLines);

        private string _lastLine;
        public string LastLine
        {
            get => this._lastLine;
            set => this.SetProperty(ref this._lastLine, value);
        }

        private string _value;
        public string Value
        {
            get => this._value;
            set => this.SetProperty(ref this._value, value);
        }

        public ICommand SendValueCommand
            => new DelegateCommand(() =>
            {
                if (this._writer.BaseStream.CanWrite)
                {
                    var value = this.Value?.Trim() ?? string.Empty;

                    // 前コマンドの最終行 (カレントディレクトリ表示) が重複するため削除する
                    if (this.DataLines.Any())
                        this.DataLines.RemoveAt(this.DataLines.Count - 1);

                    // HACK: OutputDataReceived で最終行を取得するにはコマンドの末尾に改行コードを付ける必要がある様子
                    this._writer.WriteLine(value + Environment.NewLine);

                    if (string.IsNullOrEmpty(value) == false)
                    {
                        var i = this.Histories.IndexOf(value);
                        if (0 <= i)
                            this.Histories.Move(i, 0);
                        else
                            this.Histories.Insert(0, value);
                    }

                    this.Value = string.Empty;
                }
            });

        public TerminalViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(this.Histories, new object());
            BindingOperations.EnableCollectionSynchronization(this.DataLines, new object());

            this._terminal = new Process();
            this._terminal.StartInfo.FileName = Environment.GetEnvironmentVariable(_COMSPEC);
            this._terminal.StartInfo.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            this._terminal.StartInfo.CreateNoWindow = true;
            this._terminal.StartInfo.UseShellExecute = false;
            this._terminal.StartInfo.RedirectStandardInput = true;
            this._terminal.StartInfo.RedirectStandardOutput = true;
            this._terminal.StartInfo.RedirectStandardError = true;
            this._terminal.EnableRaisingEvents = true;
            this._terminal.OutputDataReceived += this.Terminal_DataReceived;
            this._terminal.ErrorDataReceived += this.Terminal_DataReceived;
            this._terminal.Exited += this.Terminal_Exited;
        }

        protected override void Dispose(bool disposing)
        {
            if (this.IsDisposed || this._closing)
                return;

            if (this._terminal != null)
            {
                this._terminal.CancelOutputRead();
                this._terminal.CancelErrorRead();

                this._terminal.OutputDataReceived -= this.Terminal_DataReceived;
                this._terminal.ErrorDataReceived -= this.Terminal_DataReceived;
                this._terminal.Exited -= this.Terminal_Exited;

                if (this._terminal.HasExited == false)
                {
                    this._closing = true;
                    this._writer?.Close();
                    this._terminal.WaitForExit();
                    this._terminal.Close();
                    this._closing = false;
                }

                this._terminal = null;
            }

            base.Dispose(disposing);
        }

        public void Start()
        {
            this._terminal.Start();
            this._terminal.BeginOutputReadLine();
            this._terminal.BeginErrorReadLine();
            this._writer = this._terminal.StandardInput;
        }

        private void Terminal_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (this.LastLine != null)
                this.DataLines.Add(this.LastLine);
            this.LastLine = e.Data;
            // HACK: 初回表示でカレントディレクトリを表示
            // マジックナンバーなのでほかに良い方法があれば直したい
            if (this._opening && this.DataLines.Count == 2)
            {
                this.DataLines.Add(this.LastLine);
                this.LastLine = $"{this._terminal.StartInfo.WorkingDirectory}>";
                this._opening = false;
            }
            if (AppConfig.TerminalLineSize < this.DataLines.Count)
                this.DataLines.RemoveAt(0);
            this.RaisePropertyChanged(nameof(this.DataLinesText));
        }

        private void Terminal_Exited(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}
