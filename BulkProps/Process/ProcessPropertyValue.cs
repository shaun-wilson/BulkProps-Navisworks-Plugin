using System;
using System.Text.RegularExpressions;
using Autodesk.Navisworks.Api;

namespace BulkProps.Process
{
    public interface IProcessPropertyValue
    {
        (string formattedValue, BulkPropsException error) ProcessPropertyValue(ProcessContext context, string rawValue);
    }

    public class ProcessPropertyValueAsFolderPath : IProcessPropertyValue
    {
        // Formats a value that contains named properties within { } brackets, and escapes brackets like String.Format().
        // The named properties may be prefixed with any amount of `..\` to access the parent items in the model tree.
        // The formatter will return an error if the parent cannot be found, or the named tab/property cannot be found.
        // Example: "Parent Item's Name is '{..\Item.Name}'"

        private static readonly Regex formatValueRegex = new Regex("(?:(?:^|[^{])(?:{{)*){([^{}]+)}");
        public (string formattedValue, BulkPropsException error) ProcessPropertyValue(ProcessContext context, string rawValue)
        {
            ModelItem item = context.CurrentItem;

            string formatStr;
            object[] formatValues;

            MatchCollection matches = formatValueRegex.Matches(rawValue);
            if (matches.Count == 0)
            {
                formatStr = rawValue;
                formatValues = new object[0];
            }
            else
            {
                (int, int, string)[] inputReplace = new (int, int, string)[matches.Count]; // Must apply the replacements in reverse order to maintain the indexes.
                formatValues = new object[matches.Count];

                int i = 0;
                foreach (Match m in matches)
                {
                    string propPath = m.Groups[1].Value;
                    inputReplace[i] = (m.Groups[1].Index, m.Groups[1].Length, i.ToString());

                    ModelItem target = item;
                    string relPath = propPath;
                    while (relPath.StartsWith(@"..\") && target != null)
                    {
                        relPath = relPath.Remove(0, 3);
                        target = target.Parent;
                    }
                    if (target == null)
                    {
                        return (null, new BulkPropsProcessingException(string.Format("PropertyProcessor could not follow path '{0}' for item {1}", propPath, item.ToString()), context));
                    }

                    var parts = relPath.Split(new[] { '.' }, 2);
                    string tab = parts[0];
                    string prop = parts[1];

                    bool foundTab = false;
                    bool foundProp = false;
                    foreach (PropertyCategory pc in target.PropertyCategories)
                    {
                        if (pc.DisplayName == tab)
                        {
                            foundTab = true;
                            DataProperty p = pc.Properties.FindPropertyByDisplayName(prop);
                            if (p != null && p.Value.IsDisplayString)
                            {
                                formatValues[i] = p.Value.ToDisplayString();
                                foundProp = true;
                            }
                            break;
                        }
                    }
                    if (!foundTab)
                    {
                        return (null, new BulkPropsProcessingException(string.Format("PropertyProcessor could not find tab '{0}' for item {1}", tab, item.ToString()), context));
                    }
                    else if (!foundProp)
                    {
                        return (null, new BulkPropsProcessingException(string.Format("PropertyProcessor could not find property '{0}' for item {1}", parts, item.ToString()), context));
                    }

                    i++;
                }

                formatStr = rawValue;
                Array.Reverse(inputReplace);
                foreach (var (idx, len, str) in inputReplace)
                {
                    formatStr = formatStr.Remove(idx, len).Insert(idx, str);
                }

            }

            string output;
            try
            {
                output = string.Format(formatStr, formatValues);
            }
            catch
            {
                return (null, new BulkPropsProcessingException(string.Format("PropertyProcessor could not format string '{0}' with values '{1}' for item {2}", formatStr, formatValues.ToString(), item.ToString()), context));
            }

            return (output, null);
        }
    }
}
