//SalesService
// Find items
// Examine and store sales

namespace Reolmarkedet.Core.Services
{
    public class SalesService
    {



        // DATABASE IMPLEMENTATION DO NOT DELETE
        /* 
         private readonly IItemRepository _itemRepository;
         private readonly ISaleRepository _saleRepository;

         public SalesService(IItemRepository itemRepository, ISaleRepository saleRepository)
         {
             _itemRepository = itemRepository;
             _saleRepository = saleRepository;
         }

         public async Task<int> RegisterSaleAsync(string barcode, decimal salePrice, DateOnly saleDate)
         {
             if (string.IsNullOrWhiteSpace(barcode))
             {
                 throw new ArgumentException(
                     "A barcode is required",
                     nameof(barcode));
             }

             if (salePrice < 0)
             {
                 throw new ArgumentException(
                     "Sale price cannot be negative",
                     nameof(salePrice));
             }

             Item? item = await _itemRepository.GetByBarcodeAsync(barcode) ?? throw new InvalidOperationException(
                     $"No item found with barcode: {barcode}");

             bool itemHasAlreadyBeenSold =
                 await _saleRepository.ExistsForItemAsync(item.ItemId);

             if (itemHasAlreadyBeenSold)
             {
                 throw new InvalidOperationException(
                     $"Item with barcode {barcode} has already been sold.");
             }

             Sale sale = new()
             {
                 SaleDate = saleDate,
                 SalePrice = salePrice,
                 ItemId = item.ItemId
             };

             return await _saleRepository.AddAsync(sale);
         }*/
    }
}
