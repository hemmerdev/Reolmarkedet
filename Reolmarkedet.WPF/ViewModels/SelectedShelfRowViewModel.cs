using Reolmarkedet.Core.Models;

namespace Reolmarkedet.WPF.ViewModels
{
    // Displays different shelf prices for selected shelves in the rental view
    public class SelectedShelfRowViewModel
    {
        public Shelf Shelf { get; }
        public decimal MonthlyRent { get; }
        public int ShelfNumber => Shelf.ShelfNumber;

        public SelectedShelfRowViewModel(Shelf shelf, decimal monthlyRent)
        {
            Shelf = shelf;
            MonthlyRent = monthlyRent;
        }
    }
}
