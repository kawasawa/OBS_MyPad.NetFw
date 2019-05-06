using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Data;
using System.Windows.Input;

namespace MyPad.ViewModels
{
    public class TerminalViewModel : ViewModelBase
    {
        private static int SEQUENSE = 0;
        private const string COMSPEC = "COMSPEC";
        private Process _terminal;
        private StreamWriter _writer;
        private bool _closing;

        public int Sequense { get; } = ++SEQUENSE;
        public string TerminalName => $"Terminal-{this.Sequense}";

        public ObservableCollection<string> DataLines { get; } = new ObservableCollection<string>();
        public string DataLinesText => string.Join(Environment.NewLine, this.DataLines);

        private string _value;
        public string Value
        {
            get => this._value;
            set => this.SetProperty(ref this._value, value);
        }

        public ICommand SendValueCommand
            => new DelegateCommand(() =>
            {
                if (this._writer.BaseStream.CanWrite && string.IsNullOrEmpty(this.Value) == false)
                {
                    this._writer.WriteLine(this.Value);
                    this.Value = string.Empty;
                }
            });

        public TerminalViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(this.DataLines, new object());

            this._terminal = new Process();
            this._terminal.StartInfo.FileName = Environment.GetEnvironmentVariable(COMSPEC);
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
            this.DataLines.Add(e.Data);
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
