using Microsoft.Extensions.Configuration;
using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Data.Database;
using Reolmarkedet.Data.Repositories;
using Reolmarkedet.WPF.Services;
using Reolmarkedet.WPF.ViewModels;
using System.Data.Common;
using System.IO;
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

            try
            {
                // Build configuration from appsettings.json
                IConfigurationRoot config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                // Retrieve the connection string from the configuration
                string? connectionString =
                    config.GetConnectionString("DefaultConnection");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    MessageBox.Show(
                        "Programmet kunne ikke starte, fordi der ikke er angivet en gyldig ConnectionString i appsettings.json. " +
                        "Kontrollér databaseopsætningen, og prøv igen.",
                        "Fejl",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown();
                    return;
                }

                // Test the database connection
                DatabaseConnectionTester connectionTester =
                    new(connectionString);

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
                IConfirmationService confirmationService =
                    new MessageBoxConfirmationService();
                MainViewModel mainViewModel = new(
                    tenantRepository,
                    shelfRepository,
                    shelfTypeRepository,
                    rentalRepository,
                    itemRepository,
                    saleRepository,
                    confirmationService);

                MainWindow mainWindow = new(mainViewModel);
                mainWindow.Show();
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show(
                    "Programmet kunne ikke starte, fordi appsettings.json ikke blev fundet. " +
                    "Opret filen ud fra appsettings.example.json, og prøv igen.",
                    "Fejl",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
            catch (InvalidDataException)
            {
                MessageBox.Show(
                    "Programmet kunne ikke starte, fordi appsettings.json ikke kunne læses. " +
                    "Kontrollér, at filen indeholder gyldig JSON, og prøv igen.",
                    "Fejl",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
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
