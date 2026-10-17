using CrmFunctionRunner.Infrastructure;
using System;
using System.Configuration;
using System.Windows;
using System.Windows.Threading;

namespace CrmFunctionRunner
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnUnhandled;
            Loc.Instance.SetArabic(string.Equals(ConfigurationManager.AppSettings["Language"], "ar", StringComparison.OrdinalIgnoreCase));
            ShowConnect(null);
        }

        /// <summary>Shows the sign-in window, then the main window. Used at start-up and for “Change connection”.</summary>
        public void ShowConnect(Window previous)
        {
            var connect = new ConnectWindow();
            bool? ok = connect.ShowDialog();
            if (ok == true)
            {
                var main = new MainWindow(connect.Result);
                MainWindow = main;
                main.Closed += (s, a) => { if (MainWindow == main) Shutdown(); };
                main.Show();
                if (previous != null)
                {
                    previous.Close();
                }
            }
            else if (previous == null)
            {
                Shutdown();
            }
        }

        void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, Loc.T("AppTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
