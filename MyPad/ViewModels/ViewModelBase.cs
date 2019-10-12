using MyLib.Wpf;
using System;

namespace MyPad.ViewModels
{
    public abstract class ViewModelBase : ValidatableBase
    {
        public void ForceGC(bool finalize = true)
        {
            GC.Collect();
            if (finalize)
            {
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
    }
}
