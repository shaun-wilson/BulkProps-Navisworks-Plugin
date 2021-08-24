using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Interop.ComApi;
using ComApi = Autodesk.Navisworks.Api.Interop.ComApi;
using ComApiBridge = Autodesk.Navisworks.Api.ComApi;

namespace BulkProps.Process
{
    public class ProcessConfig
    {
        public string ItemsSource = null; // if null, current selection will be used.

        public bool ApplyToFiles = false;
        public bool ApplyToLayers = false;
        public bool ApplyToGroups = true;
        public bool ApplyToInserts = true;
        public bool ApplyToGeometry = true;

        public List<TabProcessor> TabProcesses { get; } = new List<TabProcessor>();
    }

    public struct ProcessContext
    {
        public readonly Processor Processor;
        public readonly ProcessConfig Config;

        public readonly Document Document;
        public readonly Transaction Transaction;
        public readonly ComApi.InwOpState10 ComState;
        public readonly ModelItemCollection ModelItemCollection;

        public int ItemIndex;
        public ModelItem CurrentItem;

        public ProcessContext(Processor processor, ProcessConfig config_, Document document, Transaction transaction, InwOpState10 comState, ModelItemCollection modelItemCollection)
        {
            Processor = processor;
            Config = config_;
            Document = document;
            Transaction = transaction;
            ComState = comState;
            ModelItemCollection = modelItemCollection;
            ItemIndex = -1;
            CurrentItem = null;
        }

        public (string displayName, string internalName, BulkPropsException error) ProcessTabName(string rawName)
        {
            return Processor.TabNameProcessor.ProcessTabName(this, rawName);
        }

        public (string displayName, string internalName, BulkPropsException error) ProcessPropertyName(string rawName)
        {
            return Processor.PropertyNameProcessor.ProcessPropertyName(this, rawName);
        }

        public (string formattedValue, BulkPropsException error) ProcessPropertyValue(string rawValue)
        {
            return Processor.PropertyValueProcessor.ProcessPropertyValue(this, rawValue);
        }
    }

}
