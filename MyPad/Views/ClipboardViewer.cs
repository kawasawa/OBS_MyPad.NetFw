using System;
using System.Windows.Interop;
using Vanara.PInvoke;

namespace MyPad.Views
{
    public class ClipboardViewer : IDisposable
    {
        private readonly HwndSource _handleSource;
        private readonly IntPtr _handle;
        private HWND _nextHandle;

        public event EventHandler DrawClipboard;

        public ClipboardViewer(IntPtr handle)
        {
            this._handleSource = HwndSource.FromHwnd(handle);
            this._handleSource.AddHook(this.WndProc);
            this._handle = handle;
            this._nextHandle = User32.SetClipboardViewer(this._handle);
        }

        public void Dispose()
        {
            User32.ChangeClipboardChain(this._handle, this._nextHandle);
            this._handleSource.RemoveHook(this.WndProc);
            this._handleSource.Dispose();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((User32_Gdi.WindowMessage)msg)
            {
                case User32_Gdi.WindowMessage.WM_DRAWCLIPBOARD:
                {
                    User32_Gdi.SendMessage(this._nextHandle, (uint)msg, wParam, lParam);
                    this.OnDrawClipboard();
                    handled = true;
                    break;
                }
                case User32_Gdi.WindowMessage.WM_CHANGECBCHAIN:
                {
                    if (wParam == this._nextHandle)
                        this._nextHandle = lParam;
                    else
                        User32_Gdi.SendMessage(this._nextHandle, (uint)msg, wParam, lParam);
                    handled = true;
                    break;
                }
            }
            return IntPtr.Zero;
        }

        protected virtual void OnDrawClipboard()
            => this.DrawClipboard?.Invoke(this, EventArgs.Empty);
    }
}
