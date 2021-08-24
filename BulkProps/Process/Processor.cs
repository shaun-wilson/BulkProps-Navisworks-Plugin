using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Interop.ComApi;
using ComApi = Autodesk.Navisworks.Api.Interop.ComApi;
using ComApiBridge = Autodesk.Navisworks.Api.ComApi;

namespace BulkProps.Process
{

    public class Processor
    {
        public IGetModelItemsForConfig GetModelItemsForConfig = new GetModelItemsFromSelectionSetUNCPath();
        public IProcessTabName TabNameProcessor = new ProcessTabNameAsString();
        public IProcessPropertyName PropertyNameProcessor = new ProcessPropertyNameAsString();
        public IProcessPropertyValue PropertyValueProcessor = new ProcessPropertyValueAsFolderPath();

        public Processor() { }
        public Processor(IGetModelItemsForConfig getModelItemsForConfig, IProcessTabName tabNameProcessor, IProcessPropertyName propertyNameProcessor, IProcessPropertyValue propertyValueProcessor)
        {
            GetModelItemsForConfig = getModelItemsForConfig;
            TabNameProcessor = tabNameProcessor;
            PropertyNameProcessor = propertyNameProcessor;
            PropertyValueProcessor = propertyValueProcessor;
        }

        public (ModelItemCollection, BulkPropsException) Process(ProcessConfig config_, Document document)
        {
            BulkPropsException err = null;

            // get COM state
            ComApi.InwOpState10 comState = ComApiBridge.ComApiBridge.State;

            // start a trasaction
            Transaction transaction = document.BeginTransaction("BulkProps Process started " + System.DateTime.Now.ToString("HH:mm:ss"));

            // get the model items
            ModelItemCollection modelItems;
            if (string.IsNullOrEmpty(config_.ItemsSource))
            {
                modelItems = new ModelItemCollection(document.CurrentSelection.SelectedItems);
            }
            else
            {
                (modelItems, err) = GetModelItemsForConfig.GetSelectionSet(document, config_.ItemsSource);
                if (err != null)
                {
                    // end the transaction, and rollback the changes
                    transaction.Commit();
                    document.Rollback();
                    return (null, err);
                }
            }

            // build a context
            ProcessContext context = new ProcessContext(this, config_, document, transaction, comState, modelItems);

            // process the model items
            try
            {
                err = processItems(context);
            }
            catch (Exception e)
            {
                err = new BulkPropsProcessingException("An unhandled exception occured whilst processing the items;\n\n" + e.ToString(), context);
            }

            // end the transaction
            transaction.Commit();

            // rollback the changes if error encountered
            if (err != null && !document.IsActiveTransaction)
                document.Rollback();

            return (modelItems, err);
        }

        public BulkPropsProcessManyException ProcessMany(Dictionary<string, ProcessConfig> namedConfigs, Document document)
        {
            // start a trasaction
            Transaction transaction = document.BeginTransaction("BulkProps ProcessMany started " + System.DateTime.Now.ToString("HH:mm:ss"));

            foreach (var namedConfig in namedConfigs)
            {
                BulkPropsException err = null;
                (_, err) = Process(namedConfig.Value, document);
                if (err != null)
                {
                    BulkPropsProcessManyException errMany = new BulkPropsProcessManyException(string.Format("The processor crashed whilst processing config {0}\nAll previous configs are being undone, but you should consider re-opening the model to be sure.", namedConfig.Key), namedConfig.Key, err);
                    // end the transaction, and rollback the changes
                    transaction.Commit();
                    document.Rollback();
                    return errMany;
                }
            }

            // end the transaction
            transaction.Commit();

            return null;
        }

        private BulkPropsException processItems(ProcessContext context)
        {
            // process only the necessary model items
            BulkPropsException err;
            foreach (ModelItem item in context.ModelItemCollection)
            {
                context.CurrentItem = item;
                context.ItemIndex++;
                switch (item.ClassDisplayName)
                {
                    case "File":
                        if (!context.Config.ApplyToFiles) continue;
                        break;

                    case "Layer":
                        if (!context.Config.ApplyToLayers) continue;
                        break;

                    case "Group":
                        if (!context.Config.ApplyToGroups) continue;
                        break;

                    case "Insert":
                        if (!context.Config.ApplyToGroups) continue;
                        break;

                    default:
                        if (!item.HasGeometry) // confirm it is a geometery item, not something unexpected.
                        {
                            // IDEA should we raise a warning, or write to a log?
                            continue;
                        }
                        if (!context.Config.ApplyToGeometry) continue;
                        break;
                }
                err = processItem(context);
                if (err != null) return err;
            }
            return null;
        }

        private BulkPropsException processItem(ProcessContext context)
        {
            // get the item path in COM
            ComApi.InwOaPath itemPath = ComApiBridge.ComApiBridge.ToInwOaPath(context.CurrentItem);

            // get "properties node" of the path (aka tabs, .net API = PropertyCategoryCollection)
            ComApi.InwGUIPropertyNode2 propTabs = context.ComState.GetGUIPropertyNode(itemPath, true) as ComApi.InwGUIPropertyNode2;

            // process each tab action
            BulkPropsException err;
            foreach (TabProcessor tabProcessor in context.Config.TabProcesses)
            {
                err = tabProcessor.Process(context, propTabs);
                if (err != null) return err;
            }

            // prevent errors between .NET garbage collector and COM api
            GC.KeepAlive(propTabs);

            return null;
        }

    }

}
