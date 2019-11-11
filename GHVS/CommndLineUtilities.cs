using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GHVS
{
    public class CommndLineUtilities
    {
        public static async Task OpenFileInFolderAsync(string workingDir, string fullPath)
        {
            var application = FindVisualStudioApplication();
            if (application == null)
            {
                return;
            }

            if (IsCode(application))
            {
                if (workingDir is null)
                {
                    VSCodeUtilities.OpenFileOrFolder(fullPath);
                }
                else
                {
                    VSCodeUtilities.OpenFileInFolder(workingDir, fullPath);
                }
            }
            else
            {
                if (workingDir is null)
                {
                    VisualStudioUtilities.OpenFileOrFolder(application, fullPath);
                }
                else
                {
                    await VisualStudioUtilities.OpenFileInFolderAsync(application, workingDir, fullPath);
                }
            }
        }


        public static void OpenFromUrl(string url)
        {

            var application = FindVisualStudioApplication();
            if (application == null)
            {
                return;
            }

            if (IsCode(application))
            {
                Console.WriteLine("Opening GitHub URLs in VSCode isn't currently supported");
            }
            else
            {
                VisualStudioUtilities.OpenFromUrl(application, url);
            }
        }

        static bool IsCode(string application)
        {
            return Path.GetFileNameWithoutExtension(application).Equals("code", StringComparison.OrdinalIgnoreCase);
        }

        public static string FindVisualStudioApplication()
        {
            Console.WriteLine("Please select an application:");
            var applications = VisualStudioUtilities.GetApplicationPaths()
                .Concat(VSCodeUtilities.FindApplicationPaths())
                .ToList();
            for (var row = 0; row < applications.Count; row++)
            {
                Console.WriteLine($"{row}: {applications[row]}");
            }

            if (!int.TryParse(Console.ReadLine(), out int selectedRow))
            {
                return null;
            }

            return applications[selectedRow];
        }
    }
}
