using Microsoft.Extensions.Configuration;
using Reolmarkedet.Data.Database;
using Reolmarkedet.WPF.ViewModels;
using System.Windows;

namespace Reolmarkedet.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Build configuration from appsettings.json
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            // Retrieve the connection string from the configuration
            string connectionString =
                config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found.");

            // Test the database connection
            DatabaseConnectionTester connectionTester =
                new(connectionString);

            connectionTester.TestConnection();

            MainViewModel mainViewModel = new();

            MainWindow mainWindow = new(mainViewModel);
            mainWindow.Show();
        }
    }

}
