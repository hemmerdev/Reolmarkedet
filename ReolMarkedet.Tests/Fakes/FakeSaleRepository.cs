using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;

namespace ReolMarkedet.Tests.Fakes
{
    internal class FakeSaleRepository : IRepository<Sale>
    {
        private readonly List<Sale> _sales = new();
        private int _nextSaleId = 1;

        public IEnumerable<Sale> GetAll()
        {
            return _sales;
        }

        public Sale? GetById(int id)
        {
            foreach (var sale in _sales)
            {
                if (sale.SaleId == id)
                {
                    return sale;
                }
            }

            return null;
        }

        public void Add(Sale sale)
        {
            if (sale.SaleId == 0)
            {
                sale.SaleId = _nextSaleId++;
            }
            else if (sale.SaleId >= _nextSaleId)
            {
                _nextSaleId = sale.SaleId + 1;
            }

            _sales.Add(sale);
        }

        public void Update(Sale sale)
        {
            Sale? storedSale = GetById(sale.SaleId);
            if (storedSale is null)
            {
                throw new InvalidOperationException(
                    "No sale found with the specified SaleId");
            }

            storedSale.SaleDate = sale.SaleDate;
            storedSale.SalePrice = sale.SalePrice;
            storedSale.Notes = sale.Notes;
            storedSale.ItemId = sale.ItemId;
        }

        public void Delete(int id)
        {
            Sale? sale = GetById(id);

            if (sale is null)
            {
                throw new InvalidOperationException(
                    "No sale found with the specified SaleId");
            }

            _sales.Remove(sale);
        }
    }
}
