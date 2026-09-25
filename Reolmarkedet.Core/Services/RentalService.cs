using Reolmarkedet.Core.Models;


//RentalService
//- Create a rental
//- Check whether a shelf is available
//- Calculate correct rental price
//- Handle termination dates


namespace Reolmarkedet.Core.Services
{
    public class RentalService
    {
        public bool IsShelfAvailable(
            Shelf shelf,
            DateTime startDate,
            DateTime? endDate,
            IEnumerable<Rental> existingRentals)
        {
            // Compare calender dates only, ignoring time 
            DateTime requestedStart = startDate.Date;

            // No end date means the requested rental continues indefinitely
            // This value is only used for comparison 
            DateTime requestedEnd = endDate?.Date ?? DateTime.MaxValue.Date;

            // The end can equal the start, but cannot come before it
            if (requestedEnd < requestedStart)
            {
                throw new ArgumentException(
                    "End date cannot be before start date",
                    nameof(endDate));
            }

            if (!shelf.IsActive)
            {
                return false; // shelf is not active and cannot be rented
            }

            foreach (Rental existingRental in existingRentals)
            {
                // Rentals for other shelves cannot block this shelf
                if (existingRental.Shelf.ShelfId != shelf.ShelfId)
                {
                    continue;
                }

                DateTime existingStart = existingRental.StartDate.Date;
                DateTime existingEnd =
                    existingRental.EndDate?.Date ?? DateTime.MaxValue.Date;

                // These are the only two cases where the requested rental does not overlap with an existing rental:
                bool endsBeforeExistingStart = requestedEnd < existingStart;
                bool startsAfterExistingEnd = requestedStart > existingEnd;

                // If neither is true, the periods share at least one day
                if (!endsBeforeExistingStart && !startsAfterExistingEnd)
                {
                    return false; // Shelf is not available
                }

            }

            // Every rental was checked and none blocked the requested period
            return true;
        }

        public Rental? GetCurrentRentalByShelfAndDate(
            Shelf shelf,
            DateTime date,
            IEnumerable<Rental> rentals)
        {
            DateTime day = date.Date;

            foreach (var rental in rentals)
            {
                bool sameShelf = rental.Shelf.ShelfId == shelf.ShelfId;
                bool hasStarted = rental.StartDate.Date <= day;
                bool hasNotEnded = rental.EndDate?.Date >= day ||
                    rental.EndDate is null;

                if (sameShelf && hasStarted && hasNotEnded)
                {
                    return rental;
                }
            }

            return null;
        }

        public ShelfStatus GetShelfStatus(
            Shelf shelf,
            DateTime date,
            IEnumerable<Rental> rentals)
        {
            var currentRental = GetCurrentRentalByShelfAndDate(shelf, date, rentals);

            if (currentRental is null)
            {
                return ShelfStatus.Available;
            }
            else if (currentRental.EndDate is not null)
            {
                return ShelfStatus.TerminationPending;
            }
            else
            {
                return ShelfStatus.Rented;
            }
        }

        public bool HasRentalsForShelf(Shelf shelf, IEnumerable<Rental> rentals)
        {
            foreach (var rental in rentals)
            {
                if (rental.Shelf.ShelfId == shelf.ShelfId)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasCurrentOrFutureRentalsForShelf(
            Shelf shelf, DateTime date, IEnumerable<Rental> rentals)
        {
            foreach (var rental in rentals)
            {

                bool sameShelf = rental.Shelf.ShelfId == shelf.ShelfId;

                bool hasNotEnded =
                    rental.EndDate is null ||
                    rental.EndDate.Value.Date >= date.Date;

                if (sameShelf && hasNotEnded)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasRentalsForTenant(Tenant tenant, IEnumerable<Rental> rentals)
        {
            foreach (var rental in rentals)
            {
                if (rental.Tenant.TenantId == tenant.TenantId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasCurrentOrFutureRentalsForTenant(
            Tenant tenant, DateTime date, IEnumerable<Rental> rentals)
        {
            foreach (var rental in rentals)
            {
                bool sameTenant = rental.Tenant.TenantId == tenant.TenantId;
                bool hasNotEnded =
                    rental.EndDate is null ||
                    rental.EndDate.Value.Date >= date.Date;
                if (sameTenant && hasNotEnded)
                {
                    return true;
                }
            }
            return false;
        }

        public decimal GetStandardMonthlyRentPerShelf(int shelfCount)
        {
            return shelfCount switch
            {
                < 0 => throw new ArgumentOutOfRangeException(nameof(shelfCount)),
                0 => 0m,
                1 => 850m,
                2 or 3 => 825m,
                _ => 800m,
            };
        }

        public int GetRentedShelfCountForTenant(
            Tenant tenant,
            DateTime date,
            IEnumerable<Rental> rentals)
        {
            int shelfCount = 0;

            foreach (var rental in rentals)
            {
                bool sameTenant = rental.Tenant.TenantId == tenant.TenantId;
                bool hasStarted = rental.StartDate.Date <= date.Date;
                bool hasNotEnded = rental.EndDate is null ||
                    rental.EndDate.Value.Date >= date.Date;

                if (sameTenant && hasStarted && hasNotEnded)
                {
                    shelfCount++;
                }
            }
            return shelfCount;
        }

        public void TerminateRental(
            Rental rental,
            DateTime noticeDate,
            DateTime endDate,
            IEnumerable<Rental> existingRentals)
        {

            if (rental.EndDate.HasValue)
            {
                throw new InvalidOperationException(
                    "Lejemålet har allerede en slutdato");
            }

            if (endDate.Date <= rental.StartDate.Date)
            {
                throw new ArgumentException(
                    "Slutdatoen skal være efter startdatoen.");
            }

            if (endDate.Date < noticeDate.Date)
            {
                throw new ArgumentException(
                    "Slutdatoen må ikke være i fortiden");
            }

            List<Rental> otherRentals = new();

            foreach (Rental existingRental in existingRentals)
            {
                if (existingRental != rental)
                {
                    otherRentals.Add(existingRental);
                }
            }

            if (!IsShelfAvailable(
                rental.Shelf,
                rental.StartDate,
                endDate,
                otherRentals))
            {
                throw new InvalidOperationException(
                    "Reolen er ikke tilgængelig i den angivne periode");
            }

            // Every check has passed, so we can safely terminate the rental
            rental.EndDate = endDate.Date;
            rental.TerminationNoticeDate = noticeDate.Date;
        }

        public void ChangeTerminationEndDate(
            Rental rental,
            DateTime currentDate,
            DateTime newEndDate,
            IEnumerable<Rental> existingRentals)
        {
            if (!rental.EndDate.HasValue)
            {
                throw new InvalidOperationException(
                    "Lejemålet er ikke opsagt.");
            }
            if (rental.EndDate.Value.Date < currentDate.Date)
            {
                throw new InvalidOperationException(
                    "Lejemålet er allerede afsluttet.");
            }
            if (newEndDate.Date <= rental.StartDate.Date)
            {
                throw new ArgumentException(
                    "Slutdatoen skal være efter startdatoen.");
            }
            if (newEndDate.Date < currentDate.Date)
            {
                throw new ArgumentException(
                    "Slutdatoen må ikke være i fortiden");
            }

            List<Rental> otherRentals = new();
            foreach (Rental existingRental in existingRentals)
            {
                if (existingRental != rental)
                {
                    otherRentals.Add(existingRental);
                }
            }
            if (!IsShelfAvailable(
                rental.Shelf,
                rental.StartDate,
                newEndDate,
                otherRentals))
            {
                throw new InvalidOperationException(
                    "Reolen er ikke tilgængelig i den angivne periode");
            }
            // Every check has passed, so we can safely change the termination date
            rental.EndDate = newEndDate.Date;
        }

        public void UndoTermination(
            Rental rental,
            DateTime currentDate,
            IEnumerable<Rental> existingRentals)
        {
            if (!rental.EndDate.HasValue)
            {
                throw new InvalidOperationException(
                    "Lejemålet er ikke opsagt.");
            }
            if (rental.EndDate.Value.Date < currentDate.Date)
            {
                throw new InvalidOperationException(
                    "Lejemålet er allerede afsluttet.");
            }

            List<Rental> otherRentals = new();

            foreach (Rental existingRental in existingRentals)
            {
                if (existingRental != rental)
                {
                    otherRentals.Add(existingRental);
                }
            }

            if (!IsShelfAvailable(
                rental.Shelf,
                rental.StartDate,
                null,
                otherRentals))
            {
                throw new InvalidOperationException(
                    "Opsigelsen kan ikke fortrydes, da reolen er booket til et andet lejemål.");
            }

            // all checks pass
            rental.EndDate = null;
            rental.TerminationNoticeDate = null;
        }
    }
}
