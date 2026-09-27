//SalesService
// Find items
// Examine and store sales

using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Models.enums;

namespace Reolmarkedet.Core.Services
{
    public class SalesService(
        IItemRepository itemRepository,
        IRepository<Sale> saleRepository)
    {
        public Item FindItem(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new ArgumentException(
                    "Varenummer eller stregkode skal angives.");
            }

            string trimmedSearch = searchText.Trim();

            Item? item = itemRepository.GetByBarcode(trimmedSearch);

            if (item is null && int.TryParse(trimmedSearch, out int itemId))
            {
                item = itemRepository.GetById(itemId);
            }

            if (item is null)
            {
                throw new InvalidOperationException(
                    $"Varenummer eller stregkode '{trimmedSearch}' blev ikke fundet.");
            }
            if (itemRepository.IsSold(item.ItemId))
            {
                throw new InvalidOperationException(
                    "Varen er allerede solgt.");
            }

            return item;
        }

        public Sale RegisterSale(
            int itemId,
            decimal salePrice,
            DateOnly saleDate,
            string? notes,
            PaymentMethod paymentMethod)
        {
            ValidateSale(notes, salePrice);
            if (itemRepository.GetById(itemId) is null)
            {
                throw new InvalidOperationException(
                    $"Varenummer '{itemId}' blev ikke fundet.");
            }
            if (itemRepository.IsSold(itemId))
            {
                throw new InvalidOperationException(
                    $"Varenummer '{itemId}' er allerede solgt.");
            }
            if (!Enum.IsDefined(paymentMethod))
            {
                throw new ArgumentException(
                    "Vælg en gyldig betalingsmetode.");
            }

            Sale sale = new Sale
            {
                ItemId = itemId,
                SalePrice = salePrice,
                SaleDate = saleDate,
                Notes = notes,
                PaymentMethod = paymentMethod
            };

            saleRepository.Add(sale);

            return sale;
        }

        public static void ValidateSale(string? notes, decimal salePrice)
        {
            if (notes?.Length > 500)
            {
                throw new ArgumentException(
                    "Bemærkningen må højst indeholde 500 tegn.");
            }
            if (salePrice < 0 ||
                salePrice > 99_999_999.99m ||
                decimal.Round(salePrice, 2) != salePrice)
            {
                throw new ArgumentException(
                    "Salgsprisen skal være mellem 0 og 99.999.999,99 og må have højst to decimaler.");
            }
        }

        public void ReturnItem(int saleId)
        {
            if (saleRepository.GetById(saleId) is Sale)
            {
                saleRepository.Delete(saleId);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Salget med id '{saleId}' blev ikke fundet. Varen kan muligvis allerede være returneret.");
            }
        }
    }
}