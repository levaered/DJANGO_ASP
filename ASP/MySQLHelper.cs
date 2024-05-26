using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

public class MySQLHelper<T> where T : new()
{
    private readonly string _connectionString;

    public MySQLHelper(string connectionString)
    {
        _connectionString = connectionString;
    }

    private MySqlConnection GetConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    public async Task AddAsync(string insertQuery, Dictionary<string, object> parameters)
    {
        using (var connection = GetConnection())
        {
            await connection.OpenAsync();
            using (var command = new MySqlCommand(insertQuery, connection))
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value);
                }

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task UpdateAsync(string updateQuery, Dictionary<string, object> parameters)
    {
        using (var connection = GetConnection())
        {
            await connection.OpenAsync();
            using (var command = new MySqlCommand(updateQuery, connection))
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value);
                }

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task DeleteAsync(string deleteQuery, Dictionary<string, object> parameters)
    {
        using (var connection = GetConnection())
        {
            await connection.OpenAsync();
            using (var command = new MySqlCommand(deleteQuery, connection))
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value);
                }

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task<List<T>> GetAllAsync(string selectQuery)
    {
        var results = new List<T>();

        using (var connection = GetConnection())
        {
            await connection.OpenAsync();
            using (var command = new MySqlCommand(selectQuery, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var item = new T();
                    foreach (var prop in typeof(T).GetProperties())
                    {
                        if (!Equals(reader[prop.Name], DBNull.Value))
                        {
                            prop.SetValue(item, reader[prop.Name]);
                        }
                    }
                    results.Add(item);
                }
            }
        }

        return results;
    }

    public async Task<T> GetAsync(string selectQuery, Dictionary<string, object> parameters)
    {
        T item = default;

        using (var connection = GetConnection())
        {
            await connection.OpenAsync();
            using (var command = new MySqlCommand(selectQuery, connection))
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value);
                }

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        item = new T();
                        foreach (var prop in typeof(T).GetProperties())
                        {
                            if (!Equals(reader[prop.Name], DBNull.Value))
                            {
                                prop.SetValue(item, reader[prop.Name]);
                            }
                        }
                    }
                }
            }
        }

        return item;
    }
}