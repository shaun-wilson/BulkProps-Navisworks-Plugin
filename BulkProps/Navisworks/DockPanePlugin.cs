using System.Windows.Forms.Integration;

using Autodesk.Navisworks.Api.Plugins;

namespace BulkProps.Navisworks
{
    [Plugin("BulkProps.BulkPropsEditorDockPane", "BYSW",
       DisplayName = "Bulk Properties Editor",
       ToolTip = "Bulk Properties by Shaun Wilson")]
    [DockPanePlugin(450, 600, MinimumWidth = 250, AutoScroll = false, FixedSize = false)]
    class BulkPropsDockPane : DockPanePlugin
    {
        public override System.Windows.Forms.Control CreateControlPane()
        {
            //create an ElementHost
            ElementHost eh = new ElementHost();

            //assign the control
            eh.AutoSize = true;
            eh.Child = new BulkProps.UI.SingleTabEditor();

            eh.CreateControl();

            //return the ElementHost
            return eh;
        }

        public override void DestroyControlPane(System.Windows.Forms.Control pane)
        {
            pane.Dispose();
        }
    }
}
