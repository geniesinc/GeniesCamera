
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Genies.Utilities.Editor
{
    public class CompressionUtils
    {
        public static void ExtractZipFile(string zipFilePath, string outputFolder)
        {
            // Ensure the output directory exists
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
            
            using var fileStream = File.OpenRead(zipFilePath);
            using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);
            foreach (var entry in zipArchive.Entries)
            {
                if (entry.Length != 0L)
                {
                    var path = Path.Combine(outputFolder, string.Join("/", entry.FullName.Split('/').Skip(1)));
                    var directoryName = Path.GetDirectoryName(path);
                    if (!Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    using var destination = File.Create(path);
                    entry.Open().CopyTo(destination);
                }
            }
        }

        public static void ExtractTarGzFile(string tarGzFilePath, string outputFolder, string workingDirectory)
        {
            // Ensure the output directory exists
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
            
            ProcessUtils.StartProcess(
                "/bin/sh",
                workingDirectory,
                arguments: new[]
                {
                    "-c",
                    $"tar -xvf '{tarGzFilePath}' --directory '{outputFolder}' --strip-components=1"
                }
            );
        }
    }
}
