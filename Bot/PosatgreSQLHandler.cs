using Npgsql;
using NpgsqlTypes;

class PosatgreSQLHandler
{
    private string _connectionString = "Server=localhost;Port=5432;User Id=postgres;Password=root;Database=albion_tracker;";
    private NpgsqlDataSource _dataSource;

    public PosatgreSQLHandler()
    {
        _dataSource = NpgsqlDataSource.Create(_connectionString);
    }

    public void UpdateItemData(
        Dictionary<string, dynamic> dataToUpdate,
        string sqlString = "UPDATE items SET price_black_market = @price_black_market, price_black_market_last_updated = @ts WHERE db_name = @db_name"
        )
    {
        var cmd = _dataSource.CreateCommand(sqlString);
        foreach (KeyValuePair<string, dynamic> pair in dataToUpdate)
        {
            cmd.Parameters.AddWithValue(pair.Key, pair.Value);
        }
        cmd.ExecuteNonQuery();
    }

    public void GetData()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        var testCommand = new NpgsqlCommand("SELECT * FROM items;", connection);
        var reader = testCommand.ExecuteReader();
        while (reader.Read())
        {
            Console.WriteLine($"Column1: {reader["name"]}, Column2: {reader["db_name"]}");
        }
    }
}