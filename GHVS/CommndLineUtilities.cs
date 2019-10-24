using System;

namespace GHVS
{
    public class CommndLineUtilities
    {
        public static string FindVisualStudioApplication()
        {
            Console.WriteLine("Please select an application:");
            var applications = VisualStudioUtilities.GetApplicationPaths();
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
