using Microsoft.Data.SqlClient;
using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;

namespace Reolmarkedet.Data.Repositories
{
    public class SqlSaleRepository(string connectionString) : IRepository<Sale>
    {
        public IEnumerable<Sale> GetAll()
        {
            var sales = new List<Sale>();
            string query =
                @"SELECT SaleId, SaleDate, SalePrice, Notes, ItemId
                FROM dbo.SALE";

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(query, connection);
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sales.Add(new Sale
                        {
                            SaleId = reader.GetInt32(0),
                            SaleDate = DateOnly.FromDateTime(reader.GetDateTime(1)),
                            SalePrice = reader.GetDecimal(2),
                            Notes = reader.IsDBNull(3) ? null : reader.GetString(3),
                            ItemId = reader.GetInt32(4)
                        });
                    }
                }
            }
            return sales;
        }

        public Sale? GetById(int id)
        {
            Sale? sale = null;
            string query =
                @"SELECT SaleId, SaleDate, SalePrice, Notes, ItemId
                FROM dbo.SALE
                WHERE SaleId = @SaleId";

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(query, connection);
                command.Parameters.AddWithValue("@SaleId", id);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        sale = new Sale
                        {
                            SaleId = reader.GetInt32(0),
                            SaleDate = DateOnly.FromDateTime(reader.GetDateTime(1)),
                            SalePrice = reader.GetDecimal(2),
                            Notes = reader.IsDBNull(3) ? null : reader.GetString(3),
                            ItemId = reader.GetInt32(4)
                        };
                    }
                }
            }
            return sale;
        }

        public void Add(Sale sale)
        {
            string query =
                @"INSERT INTO dbo.SALE (SaleDate, SalePrice, Notes, ItemId)
                OUTPUT INSERTED.SaleId
                VALUES (@SaleDate, @SalePrice, @Notes, @ItemId)";

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(query, connection);
                command.Parameters.AddWithValue("@SaleDate", sale.SaleDate.ToDateTime(TimeOnly.MinValue));
                command.Parameters.AddWithValue("@SalePrice", sale.SalePrice);
                command.Parameters.AddWithValue("@Notes", (object?)sale.Notes ?? DBNull.Value);
                command.Parameters.AddWithValue("@ItemId", sale.ItemId);

                connection.Open();

                object? result = command.ExecuteScalar();
                if (result is not int saleId)
                {
                    throw new InvalidOperationException(
                        "The database did not return a SaleId");
                }

                sale.SaleId = saleId;
            }
        }
        public void Update(Sale sale)
        {
            string query =
                @"UPDATE dbo.SALE
                SET SaleDate = @SaleDate,
                    SalePrice = @SalePrice,
                    Notes = @Notes,
                    ItemId = @ItemId
                WHERE SaleId = @SaleId";

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(query, connection);

                command.Parameters.AddWithValue("@SaleDate", sale.SaleDate.ToDateTime(TimeOnly.MinValue));
                command.Parameters.AddWithValue("@SalePrice", sale.SalePrice);
                command.Parameters.AddWithValue("@Notes", (object?)sale.Notes ?? DBNull.Value);
                command.Parameters.AddWithValue("@ItemId", sale.ItemId);
                command.Parameters.AddWithValue("@SaleId", sale.SaleId);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException(
                        $"No sales found with SaleId {sale.SaleId}");
                }
            }
        }

        public void Delete(int id)
        {
            string query = @"DELETE FROM dbo.SALE
                           WHERE SaleId = @SaleId";

            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(query, connection);
                command.Parameters.AddWithValue("@SaleId", id);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException(
                        $"No sales found with SaleId {id}");
                }
            }
        }
    }
}
