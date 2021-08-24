using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BulkProps.Process
{
    public static class Helpers
    {
        public static string RemoveInvalidFilePathCharacters(string content, char replacement = '_')
        {
            if (string.IsNullOrEmpty(content))
                return content;

            var idx = content.IndexOfAny(InvalidFilePathCharacters);
            if (idx >= 0)
            {
                var sb = new StringBuilder(content);
                while (idx >= 0)
                {
                    sb[idx] = replacement;
                    idx = content.IndexOfAny(InvalidFilePathCharacters, idx + 1);
                }
                return sb.ToString();
            }
            return content;
        }

        private static char[] _invalidFilePathCharacters;
        public static char[] InvalidFilePathCharacters
        {
            get
            {
                if (_invalidFilePathCharacters == null)
                {
                    var invalidChars = new List<char>();
                    invalidChars.AddRange(Path.GetInvalidFileNameChars());
                    invalidChars.AddRange(Path.GetInvalidPathChars());
                    _invalidFilePathCharacters = invalidChars.ToArray();
                }
                return _invalidFilePathCharacters;
            }
        }
    }
}
