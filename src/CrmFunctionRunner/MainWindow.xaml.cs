using CrmFunctionRunner.Engine;
using CrmFunctionRunner.Infrastructure;
using CrmFunctionRunner.ViewModels;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace CrmFunctionRunner
{
    public partial class MainWindow : Window
    {
        readonly MainViewModel _vm;

        public MainWindow(Connection connection)
        {
            InitializeComponent();
            _vm = new MainViewModel(connection);
            DataContext = _vm;
            Title = Loc.T("AppTitle") + " — " + connection.Environment.Name;
            Loc.Instance.LanguageChanged += () => Title = Loc.T("AppTitle") + " — " + connection.Environment.Name;
        }

        void Language_Click(object sender, RoutedEventArgs e) => Loc.Instance.SetArabic(!Loc.IsArabic);

        void ChangeConnection_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.IsBusy) return;
            ((App)Application.Current).ShowConnect(this);
        }

        void BrowseExcel_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as FrameworkElement)?.Tag is InputField field)) return;
            var dialog = new OpenFileDialog { Filter = "Excel (*.xlsx;*.xls)|*.xlsx;*.xls|All files (*.*)|*.*" };
            if (dialog.ShowDialog(this) == true) field.Value = dialog.FileName;
        }

        async void DryRun_Click(object sender, RoutedEventArgs e) => await _vm.StartDryRunAsync();
        void Confirm_Click(object sender, RoutedEventArgs e) => _vm.GoToConfirm();
        void Back_Click(object sender, RoutedEventArgs e) => _vm.BackToInputs();
        void Stop_Click(object sender, RoutedEventArgs e) => _vm.Stop();
        void StartAgain_Click(object sender, RoutedEventArgs e) => _vm.Reset();

        async void RunLive_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.CanRunLive) return;
            await _vm.StartLiveRunAsync();
        }

        void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string folder = _vm.EvidenceFolder;
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + folder + "\"") { UseShellExecute = true });
        }

        void CopyClosure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_vm.ClosureComment());
                MessageBox.Show(this, Loc.T("Copied"), Loc.T("AppTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                // The clipboard can be locked by another program; the comment is also in summary.json.
            }
        }

        void LogBox_TextChanged(object sender, TextChangedEventArgs e) => LogBox.ScrollToEnd();

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_vm.IsBusy)
            {
                // Never close in the middle of a run: stop it first.
                e.Cancel = true;
                _vm.Stop();
            }
            base.OnClosing(e);
        }
    }
}
