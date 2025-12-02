using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Genies.Utilities
{
    public static class FileSystemUtils
    {
        private static readonly HashSet<char> _invalidFileNameChars = new(Path.GetInvalidFileNameChars());

        /// <summary>
        /// Removes from <see cref="name"/> any characters that are not valid for file names (i.e.: "file/name.png"
        /// could become "filename.png").
        /// </summary>
        public static string RemoveInvalidFileNameChars(string name)
        {
            var builder = new StringBuilder(name.Length);

            foreach (char character in name)
            {
                if (!_invalidFileNameChars.Contains(character))
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }
    }
}
