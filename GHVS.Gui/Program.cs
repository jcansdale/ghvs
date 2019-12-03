using System;
using System.Threading.Tasks;
using GHVS.Helpers;
using McMaster.Extensions.CommandLineUtils;

namespace GHVS.Gui
{
    static class Program
    {
        [STAThread]
        public static async Task Main(string[] args)
        {
            try
            {
                await CommandLineApplication.ExecuteAsync<GHVS.Program>(args);
            }
            catch(Exception e) when (e.Source == "System.Console")
            {
                // This is harmless
            }
        }
    }
}
