using System;
using System.Data;
using Dapper;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;

namespace ClassLibrary1;

public class CardModel
{
    public int id { get; set; }
    public string pokemon { get; set; }
}

public class DataAccess
{
    public async Task<List<U>> LoadData<U, T>(string sql, T parameters, string connectionString)
    {
        using (IDbConnection connection = new MySqlConnection(connectionString))
        {
            var rows = await connection.QueryAsync<U>(sql, parameters);

            return rows.ToList();
        }
    }

    public async Task SaveData<T>(string sql, T parameters, string connectionString)
    {
        using (IDbConnection connection = new MySqlConnection(connectionString))
        {
            await connection.ExecuteAsync(sql, parameters);
        }
    }

    public async Task BulkInsert<T>(string sql, List<T> records, string connectionString)
    {
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                 await connection.ExecuteAsync(sql, records, transaction: transaction);
                 await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    public string CleanPokemonName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

            string cleanedName = Regex.Replace(name, @"[^a-zA-Z0-9\s\-]", "");

            cleanedName = Regex.Replace(cleanedName, @"\s+", " ").Trim();

            return cleanedName;
        }
        public List<T> CleanRecords<T>(List<T> records, Func<T, T> cleanFunc)
        {
            return records.Select(cleanFunc).ToList();
        }

        public async Task CleanAllPokemonNames(string connectionString)
{
    using (MySqlConnection connection = new MySqlConnection(connectionString))
    {
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();
        try
        {
            // Load all records
            var records = (await connection.QueryAsync<CardModel>(
                "SELECT * FROM cards.card_table", 
                transaction: transaction)).ToList();

            int cleanedCount = 0;

            foreach (var record in records)
            {
                string cleaned = CleanPokemonName(record.pokemon);

                if (cleaned == record.pokemon)
                    continue; // no change needed, skip

                if (string.IsNullOrEmpty(cleaned))
                {
                    // Delete rows that are entirely invalid
                    await connection.ExecuteAsync(
                        "DELETE FROM cards.card_table WHERE id = @id",
                        new { record.id },
                        transaction: transaction);
                }
                else
                {
                    // Update the row with the cleaned name
                    await connection.ExecuteAsync(
                        "UPDATE cards.card_table SET pokemon = @pokemon WHERE id = @id",
                        new { pokemon = cleaned, record.id },
                        transaction: transaction);
                }

                cleanedCount++;
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
    }

    
