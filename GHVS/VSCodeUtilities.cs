using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using GitHub.Primitives;
using GitHub.Services;
using Microsoft.Win32;

namespace GHVS
{
    class VSCodeUtilities
    {
        public static IEnumerable<string> FindApplicationPaths()
        {
            if (Registry.GetValue(@"HKEY_CLASSES_ROOT\Applications\Code.exe\shell\open\command", null, null) is string commandLine)
            {
                var args = ProcessUtilities.SplitArgs(commandLine);
                if (args.Length > 1)
                {
                    yield return args[0];
                }
            }
        }

        public static bool OpenFromUrl(string repositoryDir, UriString targetUrl)
        {
            if (GitHubContextUtilities.FindContextFromUrl(targetUrl) is GitHubContext context && context.LinkType == LinkType.Blob)
            {
                var (_, path, _) = GitHubContextUtilities.ResolveBlob(repositoryDir, context);
                if(path != null)
                {
                    OpenFileInFolder(repositoryDir, path, context.Line);
                    return true;
                }
            }

            return false;
        }

        public static Process OpenFileInFolder(string folder, string path, int? line = null)
        {
            var gotoPath = line != null ? $"{path}:{line}" : path;
            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = folder,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "code",
                Arguments = $". -g \"{gotoPath}\""
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
