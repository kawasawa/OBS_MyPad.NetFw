using System.Windows.Controls;

namespace MyPad.Views.Components
{
    public class FlowDocumentViewer : FlowDocumentScrollViewer
    {
        protected override void OnFindCommand()
        {
            // 既定の処理を打ち消す
        }

        protected override void OnPrintCommand()
        {
            // 既定の処理を打ち消す
        }
    }
}
