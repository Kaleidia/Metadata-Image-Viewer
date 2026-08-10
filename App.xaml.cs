using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ImageViewer
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //Current.Resources["AppFontSize"] = 14.0;
            string initialFilePath = null;

            // Check if a file path argument was passed via Windows Explorer / "Open With"
            if (e.Args != null && e.Args.Length > 0)
            {
                initialFilePath = e.Args[0];
            }

            // Set your default font size programmatically on startup

            var mainWindow = new MainWindow(initialFilePath);
            MainWindow = mainWindow;
            mainWindow.Show();

        }


    }
}