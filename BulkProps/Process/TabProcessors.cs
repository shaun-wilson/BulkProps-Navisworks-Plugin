using System.Collections.Generic;
using System.Collections.Specialized;
using ComApi = Autodesk.Navisworks.Api.Interop.ComApi;

namespace BulkProps.Process
{
    // Note that tabs cannot be edited, must be deleted and made new.
    // https://adndevblog.typepad.com/aec/2012/08/addmodifyremove-custom-attribute-using-com-api.html

    public abstract class TabProcessor
    {
        public string TabName;
        public static bool UsesPropertyProcessors { get; }
        public List<PropertyProcessor> PropertyProcesses { get; } = new List<PropertyProcessor>();
        public TabProcessor(string name)
        {
            TabName = name;
        }

        public abstract BulkPropsException Process(ProcessContext context, ComApi.InwGUIPropertyNode2 propTabs);

        internal (ComApi.InwGUIAttribute2, int) getUserDefinedTab(ComApi.InwGUIPropertyNode2 propTabs, string displayName, string internalName)
        {
            int index = 0; // Note that in the com api, index 0 is used to create a new tab. the existing tabs start from index 1.
            foreach (ComApi.InwGUIAttribute2 tab in propTabs.GUIAttributes())
            {
                if (tab.UserDefined != true) continue;
                index++;
                if (tab.ClassUserName == displayName || tab.name == internalName)
                {
                    return (tab, index);
                }
            }
            return (null, 0);
        }
    }

    public abstract class TabProcessorWithPropertyProcesses : TabProcessor
    {
        public static new bool UsesPropertyProcessors { get; } = true;
        public TabProcessorWithPropertyProcesses(string name) : base(name) { }
        public TabProcessorWithPropertyProcesses(string name, List<PropertyProcessor> propProcessors) : base(name)
        {
            PropertyProcesses.AddRange(propProcessors);
        }
    }

    public class UpsertTabProcessor : TabProcessorWithPropertyProcesses
    {
        public UpsertTabProcessor(string name) : base(name) { }
        public UpsertTabProcessor(string name, List<PropertyProcessor> propProcessors) : base(name, propProcessors) { }

        internal virtual bool confirmProcess(ComApi.InwGUIAttribute2 userDefinedTab)
        {
            // always confirm for upsert
            return true;
        }

        public override BulkPropsException Process(ProcessContext context, ComApi.InwGUIPropertyNode2 propTabs)
        {
            // format the tab name
            (string displayName, string internalName, BulkPropsException err) = context.ProcessTabName(TabName);
            if (err != null)
            {
                return err;
            }

            // try find the existing tab
            (ComApi.InwGUIAttribute2 userDefinedTab, int userDefinedTabIndex) = getUserDefinedTab(propTabs, displayName, internalName);

            // confirm the requirements are met to continue processing this tab
            // the tab index will be 0 if a tab wasn't found, which is correct for an insert and insert-for-upsert
            // IDEA should there be logging for when the process doesn't apply?
            if (!confirmProcess(userDefinedTab)) return null;

            // build an ordered dict to handle the properties
            OrderedDictionary propsForTab = new OrderedDictionary();

            // populate the dict with existing properties (only required if we have a tab)
            if (userDefinedTab != null)
            {
                foreach (ComApi.InwOaProperty oldProp in userDefinedTab.Properties())
                {
                    // create new property
                    ComApi.InwOaProperty newProp = context.ComState.ObjectFactory(ComApi.nwEObjectType.eObjectType_nwOaProperty, null, null) as ComApi.InwOaProperty;

                    // set the name, username and value of the new property
                    newProp.name = oldProp.name;
                    newProp.UserName = oldProp.UserName;
                    newProp.value = oldProp.value;

                    // ad the prop to the dict
                    propsForTab.Add(newProp.name, newProp);
                }
            }

            // process the property processors
            foreach (var propProcessor in PropertyProcesses)
            {
                err = propProcessor.Process(context, propsForTab);
                if (err != null)
                {
                    return err;
                }
            }

            // create the new property category (tab)
            ComApi.InwOaPropertyVec newPropVec = context.ComState.ObjectFactory(ComApi.nwEObjectType.eObjectType_nwOaPropertyVec, null, null) as ComApi.InwOaPropertyVec;

            // add the new properties to the property category
            foreach (var newProp in propsForTab.Values)
            {
                newPropVec.Properties().Add(newProp);
            }

            // set the new property category (tab)
            propTabs.SetUserDefined(userDefinedTabIndex, displayName, internalName, newPropVec);

            return null;
        }
    }

    public class InsertTabProcessor : UpsertTabProcessor
    {
        public InsertTabProcessor(string name) : base(name) { }
        public InsertTabProcessor(string name, List<PropertyProcessor> propProcessors) : base(name, propProcessors) { }

        internal override bool confirmProcess(ComApi.InwGUIAttribute2 userDefinedTab)
        {
            // only insert if there isn't an existing tab
            return userDefinedTab == null;
        }
    }

    public class UpdateTabProcessor : UpsertTabProcessor
    {
        public UpdateTabProcessor(string name) : base(name) { }
        public UpdateTabProcessor(string name, List<PropertyProcessor> propProcessors) : base(name, propProcessors) { }

        internal override bool confirmProcess(ComApi.InwGUIAttribute2 userDefinedTab)
        {
            // only update if there is an existing tab
            return userDefinedTab != null;
        }
    }

    public class DeleteTabProcessor : TabProcessor
    {
        public static new bool UsesPropertyProcessors { get; } = false;
        public DeleteTabProcessor(string name) : base(name) { }
        public override BulkPropsException Process(ProcessContext context, ComApi.InwGUIPropertyNode2 propTabs)
        {
            // format the tab name
            (string displayName, string internalName, BulkPropsException err) = context.ProcessTabName(TabName);
            if (err != null)
            {
                return err;
            }

            // try find the existing tab
            (_, int userDefinedTabIndex) = getUserDefinedTab(propTabs, displayName, internalName);

            // ensure we found a valid index
            // IDEA should there be logging for when the process doesn't apply?
            if (userDefinedTabIndex == 0) return null;

            // remove the tab at the index
            propTabs.RemoveUserDefined(userDefinedTabIndex);

            return null;
        }
    }

}
