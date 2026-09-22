using Microsoft.Extensions.Configuration;
using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Data.Database;
using Reolmarkedet.Data.Repositories;
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

            IRepository<Tenant> tenantRepository =
                new SqlTenantRepository(connectionString);

            MainViewModel mainViewModel = new(tenantRepository);

            MainWindow mainWindow = new(mainViewModel);
            mainWindow.Show();
        }
    }

}
