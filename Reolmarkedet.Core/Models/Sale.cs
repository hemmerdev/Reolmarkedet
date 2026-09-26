namespace Reolmarkedet.Core.Models
{
    public class Sale
    {
        public int SaleId { get; set; }
        public DateOnly SaleDate { get; set; }
        public decimal SalePrice { get; set; }
        public string? Notes { get; set; }
        public int ItemId { get; set; }
    }
}
