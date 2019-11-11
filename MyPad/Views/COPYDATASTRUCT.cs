using System;
using System.Runtime.InteropServices;

namespace MyPad.Views
{
    [StructLayout(LayoutKind.Sequential)]
    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        public string lpData;
    }
}
