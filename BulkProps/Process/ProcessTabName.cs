namespace BulkProps.Process
{
    public interface IProcessTabName
    {
        (string displayName, string internalName, BulkPropsException error) ProcessTabName(ProcessContext context, string rawName);
    }

    public class ProcessTabNameAsString : IProcessTabName
    {
        public (string displayName, string internalName, BulkPropsException error) ProcessTabName(ProcessContext context, string rawName)
        {
            if (string.IsNullOrEmpty(rawName))
            {
                BulkPropsProcessingException err = new BulkPropsProcessingException(string.Format("The tab name was empty, which is not valid."), context);
                return (null, null, err);
            }
            string internalName = rawName.Replace(" ", "_");
            internalName = Helpers.RemoveInvalidFilePathCharacters(internalName);
            return (rawName, internalName, null);
        }
    }

}