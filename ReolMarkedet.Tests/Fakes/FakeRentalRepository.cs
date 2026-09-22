using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;

namespace ReolMarkedet.Tests.Fakes
{
    internal class FakeRentalRepository : IRepository<Rental>
    {
        private readonly List<Rental> _rentals = new();
        private int _nextRentalId = 1;

        public IEnumerable<Rental> GetAll()
        {
            return _rentals;
        }

        public Rental? GetById(int id)
        {
            foreach (var rental in _rentals)
            {
                if (rental.RentalId == id)
                {
                    return rental;
                }
            }

            return null;
        }

        public void Add(Rental rental)
        {
            if (rental.RentalId == 0)
            {
                rental.RentalId = _nextRentalId++;
            }
            else if (rental.RentalId >= _nextRentalId)
            {
                _nextRentalId = rental.RentalId + 1;
            }

            _rentals.Add(rental);
        }

        public void Update(Rental rental)
        {
            Rental? storedRental = GetById(rental.RentalId);
            if (storedRental is null)
            {
                throw new InvalidOperationException(
                    "No rental found with the specified RentalId");
            }

            storedRental.StartDate = rental.StartDate;
            storedRental.EndDate = rental.EndDate;
            storedRental.MonthlyRent = rental.MonthlyRent;
        }

        public void Delete(int id)
        {
            Rental? rental = GetById(id);

            if (rental is null)
            {
                throw new InvalidOperationException(
                    "No rental found with the specified RentalId");
            }

            _rentals.Remove(rental);
        }
    }
}
