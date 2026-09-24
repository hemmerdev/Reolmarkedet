using Reolmarkedet.Core.Models;
namespace Reolmarkedet.Core.Interfaces;

public interface IItemRepository : IRepository<Item>
{
    IEnumerable<Item> GetUnsold();
    Item? GetByBarcode(string barcode);
    bool IsSold(int itemId);
}
