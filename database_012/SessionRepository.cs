using System;
using System.Data;
using System.Data.SQLite;

public class MatchesRepository
{
    private SQLiteConnection connection;

    public MatchesRepository(string databaseName)
    {
        connection = new SQLiteConnection($"Data Source={databaseName};Version=3;");
        connection.Open();
        CreateMatchesTable();
    }

    private void CreateMatchesTable()
    {
        using (SQLiteCommand cmd = new SQLiteCommand(connection))
        {
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS matches (" +
                              "match_id INTEGER PRIMARY KEY AUTOINCREMENT," +
                              "date DATETIME," +
                              "discord_id INTEGER, " +
                              "joined BOOLEAN" +
                              ")";
            cmd.ExecuteNonQuery();
        }
    }

    public void InsertMatch(DateTime date, ulong discordId, bool joined)
    {
        using (SQLiteCommand cmd = new SQLiteCommand(connection))
        {
            cmd.CommandText = "INSERT INTO matches (date, discord_id, joined) " +
                              "VALUES (@date, @discord_id, @joined)";
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@discord_id", (long)discordId); // SQLite does not have ulong, so cast to long
            cmd.Parameters.AddWithValue("@joined", joined);
            cmd.ExecuteNonQuery();
        }
    }

    public void CloseConnection()
    {
        connection.Close();
    }
}
