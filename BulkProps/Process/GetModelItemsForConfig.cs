
using Autodesk.Navisworks.Api;

namespace BulkProps.Process
{
    public interface IGetModelItemsForConfig
    {
        (ModelItemCollection modelItems, BulkPropsConfigException error) GetSelectionSet(Document document, string spec);
    }

    public class GetModelItemsFromSelectionSetUNCPath : IGetModelItemsForConfig
    {
        public (ModelItemCollection modelItems, BulkPropsConfigException error) GetSelectionSet(Document document, string spec)
        {
            string modPath = spec;
            if (modPath.StartsWith(@"\\")) modPath = modPath.Remove(0, 2);
            var paths = modPath.Split(@"\".ToCharArray());

            FolderItem parent = document.SelectionSets.RootItem;
            for (int i = 0; i < (paths.Length - 1); i++)
            {
                bool foundFolder = false;
                foreach (SavedItem item in parent.Children)
                {
                    if (item.IsGroup && item.DisplayName == paths[i])
                    {
                        parent = item as FolderItem;
                        foundFolder = true;
                        break;
                    }
                }
                if (!foundFolder)
                {
                    return (null, new BulkPropsConfigException("Folder not found in Selection Set tree with name " + paths[i]));
                }
            }

            SelectionSet sSet = null;

            string lastPath = paths[paths.Length - 1];
            if (lastPath.Length > 0)
            {
                foreach (SavedItem item in parent.Children)
                {
                    if (item.DisplayName == lastPath && !item.IsGroup)
                    {
                        sSet = item as SelectionSet;
                        break;
                    }
                }
                if (sSet == null)
                {
                    return (null, new BulkPropsConfigException("Selection Set not found with name " + lastPath));
                }
            }
            else
            {
                // If the last path item is 0 length, the string must have ended with a slash, indicating a folder.
                // It is possible to select items by clicking on a folder directly in nav.
                // It cannot be done in code by simply treating the Folder as a Selection, like;
                // sSet = (parent as SavedItem) as SelectionSet;
                // Would have to loop over all the folders children and add them to a collection manually.
                // Too much effort right now. Instead, just raise an error.
                return (null, new BulkPropsConfigException("A Folder of Selection Sets cannot be used as a source for this processor"));
            }

            ModelItemCollection modelItems = sSet.GetSelectedItems();

            return (modelItems, null);
        }
    }
}
