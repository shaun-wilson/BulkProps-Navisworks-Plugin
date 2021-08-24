using System;
using System.Windows.Forms;

namespace BulkProps.Process
{
    public abstract class BulkPropsException : Exception
    {
        public string MessageText;
        public BulkPropsException(string messageText, string title = "Error")
        {
            MessageText = messageText;
            MessageBox.Show(messageText, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public class BulkPropsConfigException : BulkPropsException
    {
        public BulkPropsConfigException(string messageText) : base(messageText, "Error whilst processing the Config")
        {
        }
    }

    public class BulkPropsProcessingException : BulkPropsException
    {
        public ProcessContext Context;
        public BulkPropsProcessingException(string messageText, ProcessContext context) : base(messageText, "Error whilst Processing a Model Item")
        {
            Context = context;
        }
    }

    public class BulkPropsProcessManyException : BulkPropsException
    {
        public string ConfigName;
        public BulkPropsException ProcessError;
        public BulkPropsProcessManyException(string messageText, string configName, BulkPropsException processError) : base(messageText, "Error during a particular Process")
        {
            ConfigName = configName;
            ProcessError = processError;
        }
    }

}
