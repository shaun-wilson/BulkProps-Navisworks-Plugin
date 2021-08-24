using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using WinForms = System.Windows.Forms;

namespace BulkProps.UI
{
    public partial class SingleTabEditor : UserControl
    {
        private readonly ViewModel.SingleTabEditor vm;
        public SingleTabEditor()
        {
            InitializeComponent();
            vm = new ViewModel.SingleTabEditor();
            DataContext = vm;
        }

        public bool ShowSelectionSetPicker = false;

        private void ResizeUI()
        {
            MasterGridCellScrollViewer.HorizontalScrollBarVisibility = ActualWidth < 370.0 ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;

            //double viewTestWidth = Math.Max(MasterGridCellScrollViewer.ActualWidth, MasterGridCellScrollViewer.ExtentWidth);
            double viewTestWidth = MasterGridCellScrollViewer.ScrollableWidth == 0.0 ? MasterGridCellScrollViewer.ActualWidth : MasterGridCellScrollViewer.ExtentWidth;
            double widthAdjustForVertScroll = (MasterGridCellScrollViewer.ScrollableWidth == 0.0 && MasterGridCellScrollViewer.ScrollableHeight > 0.0) ? 18.0 : 0.0;

            comboboxItemsSource.MaxWidth = viewTestWidth - 109.0;

            WrapFilters.Width = WrapFilters.MaxWidth = viewTestWidth - 11.0 - widthAdjustForVertScroll;

            DockPanel.SetDock(WrapDock1, viewTestWidth < 370.0 ? Dock.Bottom : Dock.Right);
            LabelGrow1.MinWidth = viewTestWidth < 370.0 ? 80.0 : 0.0;

            DataGrid1.Width = DataGrid1.MaxWidth = viewTestWidth - 23.0 - widthAdjustForVertScroll;
        }
        private void MasterGridCellScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeUI();
        }

        private void ProcessConfig(object sender, RoutedEventArgs e)
        {
            (Autodesk.Navisworks.Api.ModelItemCollection items, BulkProps.Process.BulkPropsException err) = vm.Process();
            if (err != null)
            {
                return;
            }
            Autodesk.Navisworks.Api.Application.ActiveDocument.StartDisableUndo();
            Autodesk.Navisworks.Api.Application.ActiveDocument.CurrentSelection.CopyFrom(items);
            Autodesk.Navisworks.Api.Application.ActiveDocument.EndDisableUndo();
        }

        private void SaveConfigFile(object sender, RoutedEventArgs e)
        {
            WinForms.SaveFileDialog saveFileDialog = new WinForms.SaveFileDialog();
            saveFileDialog.Filter = "Bulk Props Config|*.bpc|Text File|*.txt|XML File|*.xml";
            saveFileDialog.Title = "Save Bulk Props Config";

            if (saveFileDialog.ShowDialog() != WinForms.DialogResult.OK || saveFileDialog.FileName == "") return;

            vm.SaveConfigFile(saveFileDialog.FileName);
        }

        private void LoadConfigFile(object sender, RoutedEventArgs e)
        {
            WinForms.OpenFileDialog openFileDialog = new WinForms.OpenFileDialog();
            openFileDialog.Filter = "BulkProps Config|*.bpc|Text File|*.txt|XML File|*.xml";
            openFileDialog.Title = "Load Bulk Properties Config";

            if (openFileDialog.ShowDialog() != WinForms.DialogResult.OK || openFileDialog.FileName == "") return;

            vm.LoadConfigFile(openFileDialog.FileName);
        }

        private void ProcessConfigFiles(object sender, RoutedEventArgs e)
        {
            string question = "It is recommended to save the model before processing bulk changes, in case something goes wrong.\nDo you wish to continue?";
            if (WinForms.MessageBox.Show(question, "Continue?", WinForms.MessageBoxButtons.YesNo, WinForms.MessageBoxIcon.Question) == WinForms.DialogResult.No) return;

            WinForms.OpenFileDialog openFileDialog = new WinForms.OpenFileDialog();
            openFileDialog.Filter = "BulkProps Config|*.bpc|Text File|*.txt|XML File|*.xml";
            openFileDialog.Title = "Process Bulk Property Configs";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() != WinForms.DialogResult.OK) return;

            _ = vm.ProcessConfigFiles(openFileDialog.FileNames);
        }

        private void comboboxItemsSourceTreeUpdate(object sender, System.EventArgs e)
        {
            // refresh the selection set tree every time it is opened
            vm.UpdateSelectionSetTree();
        }

        private void comboboxItemsSourceChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (comboboxItemsSource.SelectedIndex)
            {
                case 3: // if the treeview was selected, pass control to the text box
                case 1: // if the textbox is selected, check it isnt empty
                    {
                        comboboxItemsSource.SelectedIndex = string.IsNullOrEmpty(comboboxItemsSourceTextbox.Text) ? 0 : 1;
                        break;
                    }
                case 2: // shouldn't be able to select 2, so force to 0
                    {
                        comboboxItemsSource.SelectedIndex = 0;
                        break;
                    }
            }
        }
        private void comboboxItemsSourceTargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            // When the VM updates the value, choose the appropriate item
            if (string.IsNullOrEmpty(vm.ItemsSource))
            {
                comboboxItemsSource.SelectedIndex = 0;
            }
            else
            {
                comboboxItemsSourceTextbox.Text = vm.ItemsSource;
                comboboxItemsSource.SelectedIndex = 1;
            }
        }
        private void comboboxItemsSourceTextboxLostFocus(object sender, RoutedEventArgs e)
        {
            // update the VM, but if no text was entered, change the selection
            vm.ItemsSource = comboboxItemsSourceTextbox.Text;
            comboboxItemsSource.SelectedIndex = string.IsNullOrEmpty(vm.ItemsSource) ? 0 : 1;
        }
        private void comboboxItemsSourceTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // When a selection is made in the tree, update the text box, which will flow through to the VM
            if ((sender as TreeView).SelectedItem != null)
            {
                comboboxItemsSourceTextbox.Text = ((sender as TreeView).SelectedValue as ViewModel.SelectionSetTreeNode).Path;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            // for .NET Core you need to add UseShellExecute = true
            // see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
            System.Diagnostics.Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            ResizeUI();
            MasterGridCellScrollViewer.ScrollToBottom();
        }
    }
    public static class Icons
    {
        public static string Add = "\uE710"; // +
        public static string Delete = "\uE74D"; // trash can
        public static string Clear = "\uE894"; // X
        public static string Import = "\uE8B5"; // arrow
        public static string Rename = "\uE8AC"; // edit
        public static string Save = "\uE74E"; // Save disk
        public static string Load = "\uE8E5"; // OpenFile
        public static string BulkProcess = "\uE133"; // CheckList
        public static string Edit = "\uE70F"; // Edit pencil
        public static string Tag = "\uE8EC"; // Tag
        public static string SaveLocal = "\uE78C"; // Save disk with arrow down
        public static string OpenWith = "\uE7AC"; // item list with up arrow
    }

    public static class Help
    {
        public static string Text1 = string.Format("BulkProps v{0} by Shaun Wilson © 2021", Assembly.GetExecutingAssembly().GetName().Version);
        public static string Text2 = "For License, Instructions, Bug Reporting, or to Contact me; visit";
        public static string Url = "https://github.com/shaun-wilson/BulkProps-Navisworks-Plugin";
        public static System.Uri Uri { get { return new System.Uri(Url); } }
    }
}
