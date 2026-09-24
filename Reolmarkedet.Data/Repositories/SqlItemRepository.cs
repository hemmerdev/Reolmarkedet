using System.Data;
using Microsoft.Data.SqlClient;
using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Services;

namespace Reolmarkedet.Data.Repositories;

public class SqlItemRepository(string connectionString) : IItemRepository
{
    private const string Select = "SELECT i.ItemID, i.Description, i.Price, i.Barcode, i.RentalID FROM dbo.ITEM i ";
    public IEnumerable<Item> GetAll() => Read(Select + "ORDER BY i.ItemID");
    public IEnumerable<Item> GetUnsold() => Read(Select +
        "WHERE NOT EXISTS (SELECT 1 FROM dbo.SALE s WHERE s.ItemID = i.ItemID) ORDER BY i.ItemID");
    public Item? GetById(int id) => Read(Select + "WHERE i.ItemID = @Id",
        new SqlParameter("@Id", SqlDbType.Int) { Value = id }).SingleOrDefault();
    public Item? GetByBarcode(string barcode) => Read(Select + "WHERE i.Barcode = @Barcode",
        new SqlParameter("@Barcode", SqlDbType.NVarChar, 50) { Value = ItemService.NormalizeBarcode(barcode) }).SingleOrDefault();

    public bool IsSold(int itemId)
    {
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.SALE WHERE ItemID = @Id", connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = itemId;
        connection.Open();
        return (int)command.ExecuteScalar()! != 0;
    }

    private List<Item> Read(string sql, SqlParameter? parameter = null)
    {
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand(sql, connection);
        if (parameter is not null) command.Parameters.Add(parameter);
        connection.Open();
        using var reader = command.ExecuteReader();
        var result = new List<Item>();
        while (reader.Read()) result.Add(new Item
        {
            ItemId = reader.GetInt32(0),
            Description = reader.GetString(1),
            Price = reader.GetDecimal(2),
            Barcode = reader.GetString(3),
            RentalId = reader.GetInt32(4)
        });
        return result;
    }

    public void Add(Item item)
    {
        ItemService.Validate(item.Description, item.Price);
        string barcode = ItemService.NormalizeBarcode(item.Barcode);
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand("""
            INSERT INTO dbo.ITEM (Description, Price, Barcode, RentalID)
            OUTPUT INSERTED.ItemID VALUES (@Description, @Price, @Barcode, @RentalID)
            """, connection);
        AddDetails(command, item);
        command.Parameters.Add("@Barcode", SqlDbType.NVarChar, 50).Value = barcode;
        command.Parameters.Add("@RentalID", SqlDbType.Int).Value = item.RentalId;
        connection.Open();
        try
        {
            item.ItemId = (int)command.ExecuteScalar()!;
            item.Barcode = barcode;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        { throw new InvalidOperationException("Stregkoden er allerede i brug.", ex); }
        catch (SqlException ex) when (ex.Number == 547)
        { throw new InvalidOperationException("Lejemålet findes ikke længere. Opdater listen.", ex); }
    }

    public void Update(Item item)
    {
        ItemService.Validate(item.Description, item.Price);
        ChangeUnsold("""
            UPDATE dbo.ITEM SET Description = @Description, Price = @Price
            WHERE ItemID = @Id
              AND NOT EXISTS (SELECT 1 FROM dbo.SALE WITH (UPDLOCK, HOLDLOCK) WHERE ItemID = @Id)
            """, item.ItemId, item);
    }
    public void Delete(int id) => ChangeUnsold("""
        DELETE FROM dbo.ITEM WHERE ItemID = @Id
          AND NOT EXISTS (SELECT 1 FROM dbo.SALE WITH (UPDLOCK, HOLDLOCK) WHERE ItemID = @Id)
        """, id);

    private void ChangeUnsold(string sql, int id, Item? item = null)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        if (item is not null) AddDetails(command, item);
        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Varen er solgt eller findes ikke længere. Opdater listen.");
        transaction.Commit();
    }
    private static void AddDetails(SqlCommand command, Item item)
    {
        command.Parameters.Add("@Description", SqlDbType.NVarChar, 255).Value = item.Description.Trim();
        var price = command.Parameters.Add("@Price", SqlDbType.Decimal);
        price.Precision = 10;
        price.Scale = 2;
        price.Value = item.Price;
    }
}
