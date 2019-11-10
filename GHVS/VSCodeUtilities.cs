using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace GHVS
{
    class VSCodeUtilities
    {
        public static Process OpenFileInFolder(string folder, string path)
        {
            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = folder,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "code",
                Arguments = $". -g \"{path}\""
            };

            return Process.Start(startInfo);
        }

        public static Process OpenFileOrFolder(string path)
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "code",
                Arguments = $"\"{path}\""
            };

            return Process.Start(startInfo);
        }

        public static IEnumerable<string> GetFoldersForPath(string path)
        {
            foreach (var folder in GetFolders())
            {
                if (File.Exists(path) && path.StartsWith(folder, StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return folder;
                }
                else if (Directory.Exists(path) && path.Equals(folder, StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return folder;
                }
            }
        }

        public static IEnumerable<string> GetFolders()
        {
            foreach (var process in Process.GetProcessesByName("CodeHelper"))
            {
                var commandLine = ProcessUtilities.GetCommandLine(process.Id);
                var args = ProcessUtilities.SplitArgs(commandLine);

                if (args.Length == 2)
                {
                    yield return args[1];
                }
            }
        }
    }
}
