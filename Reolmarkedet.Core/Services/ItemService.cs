using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
namespace Reolmarkedet.Core.Services;

public class ItemService(IItemRepository items, IRepository<Rental> rentals)
{
    public Item Register(int rentalId, string description, decimal price, string? barcode)
    {
        Validate(description, price);

        Rental? rental = rentals.GetById(rentalId) ??
            throw new ArgumentException("Vælg et eksisterende lejemål.");

        if (rental.StartDate.Date > DateTime.Today.Date)
        {
            throw new InvalidOperationException(
                "Lejemålet er ikke startet endnu.");
        }

        if (rental.EndDate is not null &&
            rental.EndDate.Value.Date < DateTime.Today.Date)
        {
            throw new InvalidOperationException(
                "Lejemålet er allerede afsluttet.");
        }

        if (!rental.Tenant.IsActive || !rental.Shelf.IsActive)
        {
            throw new InvalidOperationException(
                "Varen kan kun registreres på et lejemål, hvor både lejer og reol er aktive.");
        }

        string code;
        if (string.IsNullOrWhiteSpace(barcode))
        {
            code = "RM" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

            // Ensure that the generated barcode is unique.
            while (items.GetByBarcode(code) is not null)
            {
                code = "RM" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
            }
        }
        else
        {
            code = NormalizeBarcode(barcode);
            if (items.GetByBarcode(code) is not null)
                throw new InvalidOperationException("Stregkoden er allerede i brug.");
        }

        var item = new Item { RentalId = rentalId, Description = description.Trim(), Price = price, Barcode = code };
        items.Add(item);
        return item;
    }

    public void Update(int itemId, string description, decimal price)
    {
        Validate(description, price);
        Item item = items.GetById(itemId) ?? throw new InvalidOperationException("Varen findes ikke længere.");
        if (items.IsSold(itemId)) throw new InvalidOperationException("Solgte varer kan ikke redigeres.");

        // Persist a copy so a failed write cannot change the displayed object.
        items.Update(new Item
        {
            ItemId = itemId,
            RentalId = item.RentalId,
            Barcode = item.Barcode,
            Description = description.Trim(),
            Price = price
        });
    }

    public void Delete(int itemId)
    {
        if (items.GetById(itemId) is null)
        {
            throw new InvalidOperationException("Varen findes ikke længere.");
        }

        if (items.IsSold(itemId))
        {
            throw new InvalidOperationException("Solgte varer kan ikke slettes, da de skal beholdes i systemet for historik.");
        }

        items.Delete(itemId);
    }

    public static string NormalizeBarcode(string barcode)
    {
        string code = barcode.Trim(); // Removes scanner CR/LF suffixes, preserves leading zeroes and case.

        if (code.Length is < 1 or > 50 || code.Any(char.IsControl))
            throw new ArgumentException("Stregkoden skal indeholde 1–50 tegn uden kontroltegn.");

        return code;
    }
    public static void Validate(string description, decimal price)
    {
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 255)
            throw new ArgumentException("Beskrivelsen skal indeholde 1–255 tegn.");

        if (price < 0 || price > 99999999.99m || decimal.Round(price, 2) != price)
            throw new ArgumentException("Prisen skal være 0–99.999.999,99 med højst to decimaler.");
    }
}
