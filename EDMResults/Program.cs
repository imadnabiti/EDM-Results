using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EDMResults.UI;

[assembly: AssemblyTitle("EDM Results")]
[assembly: AssemblyProduct("EDM Results")]
[assembly: AssemblyDescription("EDM Results — Sistema professionale di acquisizione e calcolo misure per atletica leggera via stazione totale")]
[assembly: AssemblyVersion("2.1.0.0")]
[assembly: AssemblyFileVersion("2.1.0.0")]

namespace EDMResults
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        private static void Main()
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
        }
    }
}
