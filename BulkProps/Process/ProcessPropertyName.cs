namespace BulkProps.Process
{
    public interface IProcessPropertyName
    {
        (string displayName, string internalName, BulkPropsException error) ProcessPropertyName(ProcessContext context, string rawName);
    }

    public class ProcessPropertyNameAsString : IProcessPropertyName
    {
        public (string displayName, string internalName, BulkPropsException error) ProcessPropertyName(ProcessContext context, string rawName)
        {
            if (string.IsNullOrEmpty(rawName))
            {
                BulkPropsProcessingException err = new BulkPropsProcessingException(string.Format("The property name was empty, which is not valid."), context);
                return (null, null, err);
            }
            string internalName = rawName.Replace(" ", "");
            internalName = Helpers.RemoveInvalidFilePathCharacters(internalName);
            return (rawName, internalName, null);
        }
    }

}