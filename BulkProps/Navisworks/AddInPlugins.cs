using System;

using Autodesk.Navisworks.Api.Plugins;

using WinForms = System.Windows.Forms;

namespace BulkProps.Navisworks
{
    [Plugin("BulkProps.BulkPropsEditorAddIn", "BYSW",
        DisplayName = "Bulk Properties Editor",
        ToolTip = "Bulk Properties by Shaun Wilson")]
    [AddInPluginAttribute(AddInLocation.AddIn)]
    public class BulkPropsEditorAddIn : AddInPlugin
    {
        public override int Execute(params string[] parameters)
        {
            if (Autodesk.Navisworks.Api.Application.IsAutomated)
            {
                throw new InvalidOperationException("Invalid when running using Automation");
            }

            //Find the plugin
            PluginRecord pr = Autodesk.Navisworks.Api.Application.Plugins.FindPlugin("BulkProps.BulkPropsEditorDockPane.BYSW");

            if (pr != null && pr is DockPanePluginRecord && pr.IsEnabled)
            {
                //check if it needs loading
                if (pr.LoadedPlugin == null)
                {
                    pr.LoadPlugin();
                }

                DockPanePlugin dpp = pr.LoadedPlugin as DockPanePlugin;
                if (dpp != null)
                {
                    //switch the Visible flag
                    dpp.Visible = !dpp.Visible;
                }
            }
            return 0;
        }

    }

    [Plugin("BulkProps.BulkPropsAddIn", "BYSW",
        DisplayName = "Process Bulk Property Configs",
        ToolTip = "Bulk Properties by Shaun Wilson")]
    [AddInPluginAttribute(AddInLocation.AddIn)]
    public class BulkPropsAddIn : AddInPlugin
    {
        public override int Execute(params string[] parameters)
        {
            if (Autodesk.Navisworks.Api.Application.IsAutomated)
            {
                throw new InvalidOperationException("Invalid when running using Automation");
            }

            string question = "It is recommended to save the model before processing bulk changes, in case something goes wrong.\nDo you wish to continue?";
            if (WinForms.MessageBox.Show(question, "Continue?", WinForms.MessageBoxButtons.YesNo, WinForms.MessageBoxIcon.Question) == WinForms.DialogResult.No) return 0;

            WinForms.OpenFileDialog openFileDialog = new WinForms.OpenFileDialog();
            openFileDialog.Filter = "BulkProps Config|*.bpc|Text File|*.txt|XML File|*.xml";
            openFileDialog.Title = "Process Bulk Property Configs";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() != WinForms.DialogResult.OK) return 0;

            ViewModel.SingleTabEditor vm = new ViewModel.SingleTabEditor();
            _ = vm.ProcessConfigFiles(openFileDialog.FileNames);

            return 0;
        }

    }

}