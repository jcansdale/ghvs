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
        internal static readonly string VSCodeName = "code";
        internal static readonly string VSCodeInsidersName = "code-insiders";

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
            var args = "-g " + (line != null ? $"{path}:{line}" : path);

            // Opening a file inside a folder doesn't currently work when using the WebUI
            if (!IsWebUI())
            {
                args = ". " + args;
            }

            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = folder,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = GetVSCodeExecutable(),
                Arguments = args
            };

            return Process.Start(startInfo);
        }

        static bool IsWebUI()
        {
            return Environment.GetEnvironmentVariable("CLOUDENV_SERVICE_ENDPOINT") ==
                "https://online.visualstudio.com/api/v1";
        }

        public static void OpenFromUrl(string url)
        {
            var repositoryUrl = new UriString(url).ToRepositoryUrl();
            var vscodeUri = $"vscode://vscode.git/clone?url={repositoryUrl}";

            Process.Start(new ProcessStartInfo
            {
                FileName= vscodeUri,
                UseShellExecute = true
            });
        }

        public static Process OpenFileOrFolder(string path)
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = GetVSCodeExecutable(),
                Arguments = $"\"{path}\""
            };

            return Process.Start(startInfo);
        }

        static string GetVSCodeExecutable()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // HACK: Always use "code" in Windows
                return VSCodeName;
            }

            if (Which(VSCodeInsidersName) is string codeInsidersFile && !string.IsNullOrEmpty(codeInsidersFile))
            {
                return codeInsidersFile;
            }

            if (Which(VSCodeName) is string codeFile && !string.IsNullOrEmpty(codeFile))
            {
                return codeFile;
            }

            throw new ApplicationException("Couldn't find VS Code or VS Code Insiders executable");
        }

        static string Which(string executableName)
        {            
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = "which",
                Arguments = executableName
            };

            var process = Process.Start(startInfo);
            return process.StandardOutput.ReadToEnd().TrimEnd();
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
