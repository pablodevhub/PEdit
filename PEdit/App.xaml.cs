using System.Windows;

namespace PEdit
{
    public partial class App : Application
    {
        public static string[] StartupArgs { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // FONDAMENTALE: Salva gli argomenti PRIMA di richiamare base.OnStartup
            // altrimenti la MainWindow viene creata prima di conoscere i file da aprire!
            StartupArgs = e.Args;
            base.OnStartup(e);
        }
    }
}