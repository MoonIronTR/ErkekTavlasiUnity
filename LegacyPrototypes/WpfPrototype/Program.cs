using ErkekTavlasi.Ui;
using WpfApplication = System.Windows.Application;

namespace ErkekTavlasi;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        WpfApplication application = new WpfApplication();
        application.Run(new MainWindow());
    }
}
