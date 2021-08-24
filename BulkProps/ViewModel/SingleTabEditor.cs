using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Autodesk.Navisworks.Api;

using BulkProps.Process;

namespace BulkProps.ViewModel
{
    public class SingleTabEditor : INotifyPropertyChanged
    {
        private Processor processor;
        public SingleTabEditor() : this(new Processor()) { }
        public SingleTabEditor(Processor processor_)
        {
            processor = processor_;

            // Command Relays
            AddPropertyProcessorConfigCommand = new RelayCommand<PropertyProcessorConfig>(addPropertyProcessorConfig);
            DeletePropertyProcessorConfigCommand = new RelayCommand<PropertyProcessorConfig>((pac) => deletePropertyProcessorConfig(pac));

            // Set defaults
            SelectionSetTree = new ObservableCollection<SelectionSetTreeNode>();
            SelectedTabProcessor = TabProcessorOptions[0];
            PropertyProcessorConfigs = new ObservableCollection<PropertyProcessorConfig>();
        }

        public (ModelItemCollection, BulkPropsException) Process()
        {
            ProcessConfig config = generateProcessConfig();
            (ModelItemCollection items, BulkPropsException err) = processor.Process(config, Autodesk.Navisworks.Api.Application.ActiveDocument);
            return (items, err);
        }

        public BulkPropsProcessManyException ProcessConfigFiles(string[] fullFilePaths)
        {
            Dictionary<string, ProcessConfig> namedConfigs = new Dictionary<string, ProcessConfig>();
            foreach (string fullFilePath in fullFilePaths)
            {
                LoadConfigFile(fullFilePath);
                ProcessConfig config = generateProcessConfig();
                namedConfigs.Add(fullFilePath, config);
            }
            BulkPropsProcessManyException err = processor.ProcessMany(namedConfigs, Autodesk.Navisworks.Api.Application.ActiveDocument);
            return err;
        }

        private ProcessConfig generateProcessConfig()
        {
            ProcessConfig config = new ProcessConfig();

            // set the model items source
            config.ItemsSource = ItemsSource;

            // set the item type filters
            foreach (SelectionFilter sf in SelectionFilters)
            {
                config.GetType().GetField(sf.ConfigProperty).SetValue(config, sf.IsChecked);
            }

            // make the tab processor
            TabProcessor tabProcessor = (TabProcessor)Activator.CreateInstance(SelectedTabProcessor.ProcessorClass, TabName);
            // add the property processors
            if (SelectedTabProcessor.UsesPropertyProcessors)
            {
                foreach (PropertyProcessorConfig ppc in PropertyProcessorConfigs)
                {
                    PropertyProcessor pa = ppc.PropertyProcessor.GenerateProcessor(ppc.PropertyName, ppc.PropertyValue);
                    tabProcessor.PropertyProcesses.Add(pa);
                }
            }
            // set the tab processor
            config.TabProcesses.Add(tabProcessor);

            return config;
        }

        public ObservableCollection<SelectionSetTreeNode> SelectionSetTree { get; set; }

        public void UpdateSelectionSetTree()
        {
            SelectionSetTree.Clear();
            foreach (SavedItem item in Autodesk.Navisworks.Api.Application.ActiveDocument.SelectionSets.RootItem.Children)
            {
                SelectionSetTree.Add(SelectionSetToTreeNode(item, @"\\"));
            }
        }

        private SelectionSetTreeNode SelectionSetToTreeNode(SavedItem savedItem, string inPath)
        {
            SelectionSetTreeNode treeNode = new SelectionSetTreeNode(savedItem);
            string path = inPath + savedItem.DisplayName;
            if (savedItem.IsGroup)
            {
                path = path + @"\";
                foreach (SavedItem child in (savedItem as GroupItem).Children)
                {
                    treeNode.Children.Add(SelectionSetToTreeNode(child, path));
                }
            }
            treeNode.Path = path;
            return treeNode;
        }

        private string itemsSource;
        public string ItemsSource
        {
            get { return itemsSource; }
            set
            {
                itemsSource = value;
                OnPropertyChanged("ItemsSource");
            }
        }

        private string selectionSet;
        public string SelectionSet
        {
            get { return selectionSet; }
            set
            {
                selectionSet = value;
                OnPropertyChanged("SelectionSet");
            }
        }

        private TabProcessorOption selectedTabProcessor;
        public TabProcessorOption SelectedTabProcessor
        {
            get { return selectedTabProcessor; }
            set
            {
                selectedTabProcessor = value;
                OnPropertyChanged("SelectedTabProcessor");
            }
        }

        private string tabName;
        public string TabName
        {
            get { return tabName; }
            set
            {
                tabName = value;
                OnPropertyChanged("TabName");
            }
        }

        public ObservableCollection<PropertyProcessorConfig> PropertyProcessorConfigs { get; set; }

        public ICommand AddPropertyProcessorConfigCommand { get; private set; }

        private void addPropertyProcessorConfig(object obj)
        {
            PropertyProcessorConfigs.Add(new PropertyProcessorConfig("", PropertyProcessorOptions[0], ""));
        }

        public ICommand DeletePropertyProcessorConfigCommand { get; private set; }

        private void deletePropertyProcessorConfig(PropertyProcessorConfig ppc)
        {
            PropertyProcessorConfigs.Remove(ppc);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public class SelectionFilter : INotifyPropertyChanged
        {
            public string Name { get; private set; }
            public bool Default { get; private set; }
            private bool isChecked;
            internal string ConfigProperty;
            public bool IsChecked
            {
                get { return isChecked; }
                set
                {
                    isChecked = value;
                    OnPropertyChanged("IsChecked");
                }
            }

            public SelectionFilter(string name, bool default_, string configProperty)
            {
                Name = name;
                Default = default_;
                IsChecked = default_;
                ConfigProperty = configProperty;
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public List<SelectionFilter> SelectionFilters { get; private set; } = new List<SelectionFilter>()
        {
            new SelectionFilter("Files", false, "ApplyToFiles"),
            new SelectionFilter("Layers", false, "ApplyToLayers"),
            new SelectionFilter("Groups", true, "ApplyToGroups"),
            new SelectionFilter("Inserts", true, "ApplyToInserts"),
            new SelectionFilter("Geometry", true, "ApplyToGeometry"),
        };

        public class TabProcessorConfig
        {
            public string TabName { get; set; }
            public TabProcessorOption TabProcessor { get; set; }
        }

        public class TabProcessorOption
        {
            public string ProcessorName { get; private set; }
            public Type ProcessorClass { get; private set; }
            public bool UsesPropertyProcessors { get; private set; }
            public TabProcessorOption(string name, Type processorClass)
            {
                ProcessorName = name;
                ProcessorClass = processorClass;
                UsesPropertyProcessors = (bool)processorClass.GetProperty("UsesPropertyProcessors", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
            }
        }
        public static List<TabProcessorOption> TabProcessorOptions { get; private set; } = new List<TabProcessorOption>()
        {
            new TabProcessorOption("Upsert", typeof(UpsertTabProcessor)),
            new TabProcessorOption("Insert", typeof(InsertTabProcessor)),
            new TabProcessorOption("Update", typeof(UpdateTabProcessor)),
            new TabProcessorOption("Delete", typeof(DeleteTabProcessor)),
        };
        public class PropertyProcessorConfig : INotifyPropertyChanged
        {
            private string propertyName;
            public string PropertyName
            {
                get { return propertyName; }
                set
                {
                    propertyName = value;
                    OnPropertyChanged("PropertyName");
                }
            }

            private PropertyProcessorOption propertyProcessor;
            public PropertyProcessorOption PropertyProcessor
            {
                get { return propertyProcessor; }
                set
                {
                    propertyProcessor = value;
                    OnPropertyChanged("PropertyProcessor");
                }
            }

            private string propertyValue;
            public string PropertyValue
            {
                get { return propertyValue; }
                set
                {
                    propertyValue = value;
                    OnPropertyChanged("PropertyValue");
                }
            }

            public PropertyProcessorConfig(string propertyName, PropertyProcessorOption propertyProcessor)
            {
                PropertyName = propertyName;
                PropertyProcessor = propertyProcessor;
            }

            public PropertyProcessorConfig(string propertyName, PropertyProcessorOption propertyProcessor, string propertyValue)
            {
                PropertyName = propertyName;
                PropertyProcessor = propertyProcessor;
                PropertyValue = propertyValue;
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public abstract class PropertyProcessorOption
        {
            public string ProcessorName { get; internal set; }
            public bool UsesValue { get; internal set; }
            public Type ProcessorClass { get; internal set; }
            public abstract PropertyProcessor GenerateProcessor(string propName, string propValue);
        }

        public class PropertyProcessorOption<TProcessor> : PropertyProcessorOption where TProcessor : PropertyProcessor, new()
        {
            public PropertyProcessorOption(string name)
            {
                ProcessorName = name;
                UsesValue = (bool)typeof(TProcessor).GetProperty("UsesValue", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
                ProcessorClass = typeof(TProcessor);
            }
            public override PropertyProcessor GenerateProcessor(string propName, string propValue)
            {
                var processor = new TProcessor();
                processor.PropertyName = propName;
                if (UsesValue)
                {
                    processor.Value = propValue;
                }
                return processor;
            }
        }
        public static List<PropertyProcessorOption> PropertyProcessorOptions { get; private set; } = new List<PropertyProcessorOption>()
        {
            new PropertyProcessorOption<UpsertPropertyProcessor>("Upsert"),
            new PropertyProcessorOption<InsertPropertyProcessor>("Insert"),
            new PropertyProcessorOption<UpdatePropertyProcessor>("Update"),
            new PropertyProcessorOption<DeletePropertyProcessor>("Delete"),
        };

        public void SaveConfigFile(string fullFilePath)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = ("\t");
            settings.OmitXmlDeclaration = true;

            try
            {
                using (XmlWriter writer = XmlWriter.Create(fullFilePath, settings))
                {
                    writer.WriteStartElement("BulkPropertiesConfig");

                    xmlWriteElementStringAlways(writer, "ItemsSource", ItemsSource);

                    writer.WriteStartElement("ItemFilters");
                    foreach (var sf in SelectionFilters)
                        writer.WriteElementString(sf.Name, sf.IsChecked.ToString());
                    writer.WriteEndElement();

                    writer.WriteStartElement("TabConfig");
                    xmlWriteElementStringAlways(writer, "Name", TabName);
                    writer.WriteElementString("Process", SelectedTabProcessor.ProcessorName);
                    writer.WriteEndElement();

                    if (SelectedTabProcessor.UsesPropertyProcessors)
                    {
                        writer.WriteStartElement("PropertyConfigs");
                        foreach (var ppc in PropertyProcessorConfigs)
                        {
                            writer.WriteStartElement("PropertyConfig");
                            xmlWriteElementStringAlways(writer, "Name", ppc.PropertyName);
                            writer.WriteElementString("Process", ppc.PropertyProcessor.ProcessorName);
                            if (ppc.PropertyProcessor.UsesValue)
                                xmlWriteElementStringAlways(writer, "Value", ppc.PropertyValue);
                            writer.WriteEndElement();
                        }
                        writer.WriteFullEndElement();
                    }

                    writer.WriteEndElement();
                    writer.Flush();
                }
            } catch (Exception e)
            {
                MessageBox.Show("Problem whilst saving the config file. The .NET error is as follows;\n\n" + e.ToString(), "Error Saving Config", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void xmlWriteElementStringAlways(XmlWriter writer, string name, string value)
        {
            writer.WriteStartElement(name);
            writer.WriteValue(value);
            writer.WriteFullEndElement();
        }

        public void LoadConfigFile(string fullFilePath)
        {
            XDocument xmlDoc;
            try
            {
                xmlDoc = XDocument.Load(fullFilePath);
            } catch (Exception e)
            {
                MessageBox.Show("Problem loading the config file. The .NET error is as follows;\n\n" + e.ToString(), "Error Loading Config", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            XElement root = xmlDoc.XPathSelectElement("//BulkPropertiesConfig");

            ItemsSource = root.XPathSelectElement("./ItemsSource")?.Value ?? "";

            foreach (var sf in SelectionFilters)
            {
                sf.IsChecked = Convert.ToBoolean(root.XPathSelectElement("./ItemFilters/" + sf.Name)?.Value);
            }

            TabName = root.XPathSelectElement("./TabConfig/Name")?.Value ?? "";
            string processorName = root.XPathSelectElement("./TabConfig/Process")?.Value;
            processorName = string.IsNullOrEmpty(processorName) ? "Upsert" : processorName; // Force to upsert if a processor name wasn't specified.
            foreach (var tpo in TabProcessorOptions)
            {
                if (processorName == tpo.ProcessorName)
                {
                    SelectedTabProcessor = tpo;
                    break;
                }
            }

            PropertyProcessorConfigs.Clear();
            if (SelectedTabProcessor.UsesPropertyProcessors)
            {
                foreach (XElement elem in root.XPathSelectElements("./PropertyConfigs/PropertyConfig"))
                {
                    string name = elem.XPathSelectElement("./Name")?.Value ?? "";
                    processorName = elem.XPathSelectElement("./Process")?.Value;
                    processorName = string.IsNullOrEmpty(processorName) ? "Upsert" : processorName; // Force to upsert if a processor name wasn't specified.
                    foreach (PropertyProcessorOption ppo in PropertyProcessorOptions)
                    {
                        if (ppo.ProcessorName == processorName)
                        {
                            PropertyProcessorConfig ppc;
                            if (ppo.UsesValue)
                            {
                                string value = elem.XPathSelectElement("./Value")?.Value ?? "";
                                ppc = new PropertyProcessorConfig(name, ppo, value);
                            }
                            else
                            {
                                ppc = new PropertyProcessorConfig(name, ppo);
                            }
                            PropertyProcessorConfigs.Add(ppc);
                            break;
                        }
                    }
                }
            }
        }

    }

    public static class Tooltips
    {

        public static string Processors = @"Upsert will Insert the tab or property if it does not exist, or Update ones that do. Thus, it will always occur.
Insert will only occur if a tab or property with the same name does not already exist.
Update will only occur if a tab or property with the same name already exists.
Delete will remove the tab or property, but can only affect user-defined tabs and their properties (not imported ones).";

        public static string PropertyValue = @"Property values can be copied from other tabs and ancestor items, and mixed with regular text.
The tool will look for instructions in { } brackets, of the fromat 'Tab.Property'.
To select from an ancestor, prefix with as many '..\' as required.
If a tab, property, or ancestor cannot be found, the current process will error, stop, and undo its changes.
If you want to use curly-brackets in your value, escape them with another bracket, ie `{{Text}}`, as per C#.NET string.Format() rules.
An example value is: 'This item is {Item.Layer}, and its parent is called {..\Item.Name}'";

    }

    public class SelectionSetTreeNode
    {
        public SavedItem SavedItem;
        public string Path;
        public ObservableCollection<SelectionSetTreeNode> Children { get; set; }

        public SelectionSetTreeNode(SavedItem savedItem)
        {
            SavedItem = savedItem;
            Children = new ObservableCollection<SelectionSetTreeNode>();
        }

        public string DisplayName { get { return SavedItem.DisplayName; } }
        public bool IsGroup { get { return SavedItem.IsGroup; } }
    }


}
