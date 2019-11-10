using System.Diagnostics;

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
    }
}
