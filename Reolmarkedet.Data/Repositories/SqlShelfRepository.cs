using Microsoft.Data.SqlClient;
using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;

namespace Reolmarkedet.Data.Repositories
{
    public class SqlShelfRepository : IRepository<Shelf>
    {
        private readonly string _connectionString;

        public SqlShelfRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Shelf> GetAll()
        {
            var shelves = new List<Shelf>();
            string query = "SELECT shelf.ShelfId, shelf.ShelfNumber, shelf.IsActive, " +
                                   "st.ShelfTypeId, st.ShelfTypeName " +
                           "FROM dbo.SHELF AS shelf " +
                           "INNER JOIN dbo.SHELFTYPE AS st " +
                                "ON shelf.ShelfTypeId = st.ShelfTypeId";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ShelfType shelfType = new ShelfType
                        {
                            ShelfTypeId = reader.GetInt32(3),
                            Name = reader.GetString(4)
                        };
                        shelves.Add(new Shelf(shelfType)
                        {
                            ShelfId = reader.GetInt32(0),
                            ShelfNumber = reader.GetInt32(1),
                            IsActive = reader.GetBoolean(2),
                        });
                    }
                }
            }
            return shelves;
        }

        public Shelf? GetById(int id)
        {
            Shelf? shelf = null;
            string query = "SELECT shelf.ShelfId, shelf.ShelfNumber, shelf.IsActive, " +
                                   "st.ShelfTypeId, st.ShelfTypeName " +
                           "FROM dbo.SHELF AS shelf " +
                           "INNER JOIN dbo.SHELFTYPE AS st " +
                                "ON shelf.ShelfTypeId = st.ShelfTypeId " +
                           "WHERE shelf.ShelfId = @ShelfId";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);
                command.Parameters.AddWithValue("@ShelfId", id);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        ShelfType shelfType = new ShelfType
                        {
                            ShelfTypeId = reader.GetInt32(3),
                            Name = reader.GetString(4)
                        };
                        shelf = new Shelf(shelfType)
                        {
                            ShelfId = reader.GetInt32(0),
                            ShelfNumber = reader.GetInt32(1),
                            IsActive = reader.GetBoolean(2),
                        };
                    }
                }
            }
            return shelf;
        }

        public void Add(Shelf shelf)
        {
            string query = "INSERT INTO dbo.SHELF (ShelfNumber, IsActive, ShelfTypeId) " +
                           "OUTPUT INSERTED.ShelfId " +
                           "VALUES (@ShelfNumber, @IsActive, @ShelfTypeId)";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);

                command.Parameters.AddWithValue("@ShelfNumber", shelf.ShelfNumber);
                command.Parameters.AddWithValue("@IsActive", shelf.IsActive);
                command.Parameters.AddWithValue("@ShelfTypeId", shelf.ShelfType.ShelfTypeId);

                connection.Open();

                object? result = command.ExecuteScalar();
                if (result is not int shelfId)
                {
                    throw new InvalidOperationException(
                        "The database did not return a ShelfId");
                }

                shelf.ShelfId = shelfId;
            }
        }

        public void Update(Shelf shelf)
        {
            string query = "UPDATE dbo.SHELF " +
                           "SET ShelfNumber = @ShelfNumber, " +
                                "IsActive = @IsActive, " +
                                "ShelfTypeId = @ShelfTypeId " +
                           "WHERE ShelfId = @ShelfId";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);

                command.Parameters.AddWithValue("@ShelfId", shelf.ShelfId);
                command.Parameters.AddWithValue("@ShelfNumber", shelf.ShelfNumber);
                command.Parameters.AddWithValue("@IsActive", shelf.IsActive);
                command.Parameters.AddWithValue("@ShelfTypeId", shelf.ShelfType.ShelfTypeId);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException(
                        $"No shelf found with id {shelf.ShelfId}");
                }
            }
        }

        public void Delete(int id)
        {
            string query = "DELETE FROM dbo.SHELF " +
                           "WHERE ShelfId = @ShelfId";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);
                command.Parameters.AddWithValue("@ShelfId", id);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException(
                        $"No shelf found with id {id}");
                }
            }
        }
    }
}
