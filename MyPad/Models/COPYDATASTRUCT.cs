using System;
using System.Runtime.InteropServices;

namespace MyPad.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        public string lpData;
    }
}
