using CrmFunctionRunner.Engine;
using CrmFunctionRunner.Infrastructure;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace CrmFunctionRunner
{
    public partial class ConnectWindow : Window
    {
        public Connection Result { get; private set; }

        public ConnectWindow()
        {
            InitializeComponent();
            var environments = Connection.LoadEnvironments();
            EnvironmentBox.ItemsSource = environments;
            if (environments.Count > 0) EnvironmentBox.SelectedIndex = 0;
            else ShowError(Loc.T("Connect.NoEnvironments"));
        }

        void Language_Click(object sender, RoutedEventArgs e) => Loc.Instance.SetArabic(!Loc.IsArabic);

        void Auth_Changed(object sender, RoutedEventArgs e)
        {
            if (PasswordPanel != null)
                PasswordPanel.Visibility = PasswordAuth.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        async void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (!(EnvironmentBox.SelectedItem is CrmEnvironment env)) return;
            string user = PasswordAuth.IsChecked == true ? UserBox.Text.Trim() : null;
            string password = PasswordBox.Password;
            ErrorBox.Visibility = Visibility.Collapsed;
            ConnectButton.IsEnabled = false;
            ConnectButton.Content = Loc.T("Connect.Working");
            try
            {
                Result = await Task.Run(() => Connection.Open(env, user, password));
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ShowError(ex.GetBaseException().Message);
            }
            finally
            {
                PasswordBox.Clear();
                ConnectButton.IsEnabled = true;
                ConnectButton.Content = Loc.T("Connect.Go");
            }
        }

        void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBox.Visibility = Visibility.Visible;
        }
    }
}
