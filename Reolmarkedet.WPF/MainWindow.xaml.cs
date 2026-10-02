using Reolmarkedet.WPF.ViewModels;
using System.Windows;

namespace Reolmarkedet.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = mainViewModel;
        }

        private void AdminLogin_Click(object sender, RoutedEventArgs e)
        {
            string enteredPassword = AdminPasswordBox.Password;
            AdminPasswordBox.Password = string.Empty;

            MainViewModel mainViewModel = (MainViewModel)DataContext;

            bool loginSuccessful = mainViewModel.TryAdminLogin(enteredPassword);

            if (!loginSuccessful)
            {
                MessageBox.Show("Forkert adgangskode - kontakt en administrator",
                                "Forkert Adgangskode",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void AdminLogout_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel mainViewModel = (MainViewModel)DataContext;
            mainViewModel.TryAdminLogout();
        }
    }
}