using Microsoft.Extensions.Configuration;
using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Data.Database;
using Reolmarkedet.Data.Repositories;
using Reolmarkedet.WPF.ViewModels;
using System.Data.Common;
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

            try
            {

                connectionTester.TestConnection();

                IRepository<Tenant> tenantRepository =
                    new SqlTenantRepository(connectionString);
                IRepository<Shelf> shelfRepository =
                    new SqlShelfRepository(connectionString);
                IRepository<ShelfType> shelfTypeRepository =
                    new SqlShelfTypeRepository(connectionString);
                IRepository<Rental> rentalRepository =
                    new SqlRentalRepository(connectionString);
                IItemRepository itemRepository =
                    new SqlItemRepository(connectionString);
                IRepository<Sale> saleRepository =
                    new SqlSaleRepository(connectionString);

                MainViewModel mainViewModel = new(
                    tenantRepository,
                    shelfRepository,
                    shelfTypeRepository,
                    rentalRepository,
                    itemRepository,
                    saleRepository);

                MainWindow mainWindow = new(mainViewModel);
                mainWindow.Show();
            }
            catch (DbException)
            {
                MessageBox.Show(
                    "Programmet kunne ikke starte, fordi databasen ikke kunne kontaktes eller data ikke kunne indlæses. " +
                    "Kontrollér databaseopsætningen, og prøv igen.",
                    "Fejl",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
            }
        }
    }

}
