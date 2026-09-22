using Microsoft.Data.SqlClient;
using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;

namespace Reolmarkedet.Data.Repositories
{
    public class SqlShelfTypeRepository : IRepository<ShelfType>
    {
        private readonly string _connectionString;

        public SqlShelfTypeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<ShelfType> GetAll()
        {
            var shelfTypes = new List<ShelfType>();
            string query = "SELECT ShelfTypeId, ShelfTypeName " +
                           "FROM dbo.SHELFTYPE";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        shelfTypes.Add(new ShelfType
                        {
                            ShelfTypeId = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                    }

                }
            }
            return shelfTypes;
        }

        public ShelfType? GetById(int id)
        {
            ShelfType? shelfType = null;

            string query = "SELECT ShelfTypeId, ShelfTypeName " +
                           "FROM dbo.SHELFTYPE " +
                           "WHERE ShelfTypeId = @ShelfTypeId";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);
                command.Parameters.AddWithValue("@ShelfTypeId", id);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        shelfType = new ShelfType
                        {
                            ShelfTypeId = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        };
                    }
                }
            }

            return shelfType;
        }

        public void Add(ShelfType shelfType)
        {
            string query = "INSERT INTO dbo.SHELFTYPE (ShelfTypeName) " +
                           "OUTPUT INSERTED.ShelfTypeId " +
                           "VALUES (@ShelfTypeName)";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);

                command.Parameters.AddWithValue("@ShelfTypeName", shelfType.Name);

                connection.Open();
                object? result = command.ExecuteScalar();
                if (result is not int shelfTypeId)
                {
                    throw new InvalidOperationException(
                        "The database did not return a shelfTypeId");
                }

                shelfType.ShelfTypeId = shelfTypeId;
            }
        }

        public void Update(ShelfType shelfType)
        {
            string query = "UPDATE dbo.SHELFTYPE " +
                           "SET ShelfTypeName = @ShelfTypeName " +
                           "WHERE ShelfTypeId = @ShelfTypeId";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);

                command.Parameters.AddWithValue("@ShelfTypeId", shelfType.ShelfTypeId);
                command.Parameters.AddWithValue("@ShelfTypeName", shelfType.Name);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException(
                        $"No shelf type found with ID {shelfType.ShelfTypeId}");
                }
            }
        }

        public void Delete(int id)
        {
            string query = "DELETE FROM dbo.SHELFTYPE " +
                           "WHERE ShelfTypeId = @ShelfTypeId";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);
                command.Parameters.AddWithValue("@ShelfTypeId", id);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException(
                        $"No shelf type found with ID {id}");
                }
            }
        }

    }
}
