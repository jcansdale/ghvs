using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using VsixUtil;
using VsixTesting.Interop;
using VsixTesting.Utilities;
using GitHub.Primitives;

namespace GHVS
{
    class VisualStudioUtilities
    {
        public static List<string> GetApplicationPaths() =>
            InstalledVersionUtilities.GetInstalledVersions()
                .Select(iv => iv.ApplicationPath)
                .Where(ap => Path.GetFileName(ap) == "devenv.exe")
                .OrderBy(ap => ap).ToList();

        public static async Task<bool> OpenFileInFolderAsync(string application, string folder, string fullPath, int timeoutSeconds = 10)
        {
            using (var process = OpenFolder(application, folder))
            {
                for (var t = 0; t < timeoutSeconds; t++)
                {
                    if (await OpenAsync(fullPath))
                    {
                        return true;
                    }

                    await Task.Delay(1000);
                }

                return false;
            }
        }

        public static Process OpenFileOrFolder(string application, string fullPath)
        {
            if (File.Exists(fullPath))
            {
                return OpenFile(application, fullPath);
            }
            else if (Directory.Exists(fullPath))
            {
                return OpenFolder(application, fullPath);
            }
            else
            {
                throw new ArgumentException($"Couldn't find file or folder at: {fullPath}");
            }
        }

        public static Process OpenFolder(string application, string fullPath)
        {
            return Process.Start(application, $"/Command \"File.OpenFolder {fullPath}\"");
        }

        public static Process OpenFile(string application, string fullPath)
        {
            return Process.Start(application, $"/Command \"File.OpenFile {fullPath}\"");
        }

        public static Task<bool> OpenAsync(string path)
        {
            return RetryMessageFilter.Run(() =>
            {
                foreach (var dte in GetDTEsForPath(path))
                {
                    BringToFront((IntPtr)dte.MainWindow.HWnd);

                    if (File.Exists(path))
                    {
                        dte.ItemOperations.OpenFile(path);
                    }

                    return true;
                }

                return false;
            });
        }

        public static Task OpenFromUrlAsync(string path, UriString targetUrl)
        {
            return RetryMessageFilter.Run(() =>
            {
                foreach (var dte in GetDTEsForPath(path))
                {
                    BringToFront((IntPtr)dte.MainWindow.HWnd);
                    dte.ExecuteCommand("GitHub.OpenFromUrl", targetUrl);
                    return true;
                }

                return false;
            });
        }

        public static Process OpenFromUrl(string application, string url)
        {
            return Process.Start(application, $"/Command \"GitHub.OpenFromUrl {url}\"");
        }

        public static Task<IList<string>> GetSolutionPaths()
        {
            return RetryMessageFilter.Run<IList<string>>(() =>
            {
                var solutionPaths = new List<string>();
                foreach (var dte in GetDTEs())
                {
                    solutionPaths.Add(dte.Solution.FullName);
                }

                return solutionPaths;
            });
        }

        static void BringToFront(IntPtr hWnd)
        {
            if (User32.IsIconic(hWnd))
            {
                User32.ShowWindowAsync(hWnd, User32.SW_RESTORE);
            }

            User32.SetForegroundWindow(hWnd);
        }

        static IEnumerable<EnvDTE.DTE> GetDTEsForPath(string path)
        {
            foreach (var dte in GetDTEs())
            {
                string solutionDir;
                var solutionPath = dte.Solution.FileName;

                if (File.Exists(solutionPath))
                {
                    solutionDir = Path.GetDirectoryName(solutionPath);
                }
                else if (Directory.Exists(solutionPath))
                {
                    solutionDir = solutionPath;
                }
                else
                {
                    continue;
                }

                if (File.Exists(path) && path.StartsWith(solutionDir, StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return dte;
                }
                else if (Directory.Exists(path) && path.Equals(solutionDir, StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return dte;
                }
            }
        }

        static IEnumerable<EnvDTE.DTE> GetDTEs()
        {
            IEnumMoniker enumMoniker = null;
            IRunningObjectTable rot = null;
            IBindCtx bindCtx = null;
            try
            {
                Marshal.ThrowExceptionForHR(Ole32.CreateBindCtx(0, out bindCtx));
                bindCtx.GetRunningObjectTable(out rot);
                rot.EnumRunning(out enumMoniker);

                var moniker = new IMoniker[1];
                var fetched = IntPtr.Zero;

                while (enumMoniker.Next(1, moniker, fetched) == Ole32.S_OK)
                {
                    object runningObject = null;
                    try
                    {
                        var roMoniker = moniker.First();
                        if (roMoniker == null)
                            continue;
                        roMoniker.GetDisplayName(bindCtx, null, out var name);

                        if (!name.StartsWith("!VisualStudio.DTE."))
                        {
                            continue;
                        }

                        Marshal.ThrowExceptionForHR(rot.GetObject(roMoniker, out runningObject));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        continue;
                    }

                    if (runningObject is EnvDTE.DTE dte)
                    {
                        yield return dte;
                    }
                }
            }
            finally
            {
                if (enumMoniker != null)
                    Marshal.ReleaseComObject(enumMoniker);
                if (rot != null)
                    Marshal.ReleaseComObject(rot);
                if (bindCtx != null)
                    Marshal.ReleaseComObject(bindCtx);
            }
        }

        internal static class User32
        {
            [DllImport("user32.dll")]
            internal static extern bool SetForegroundWindow(IntPtr hWnd);

            internal const int SW_RESTORE = 9;
            [DllImport("user32.dll")]
            internal static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

            [DllImport("user32.dll")]
            internal static extern bool IsIconic(IntPtr hWnd);
        }
    }
}
