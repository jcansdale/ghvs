using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace GHVS.Gui
{
    static class Program
    {
        [STAThread]
        public static Task Main(string[] args) =>
            CommandLineApplication.ExecuteAsync<GHVS.Program>(args);
    }
}
