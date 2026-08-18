using Microsoft.Data.Sqlite;
using WinglePhotos.Shared;

namespace WinglePhotos.Features.Settings;

public sealed class SqliteSettingsStore : ISettingsStore
{
    public async Task<string?> GetAsync(string key)
    {
        await AppDatabase.EnsureInitializedAsync();

        using var connection = new SqliteConnection(AppDatabase.ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM settings WHERE key = @key";
        command.Parameters.AddWithValue("@key", key);

        var result = await command.ExecuteScalarAsync();
        return result as string;
    }

    public async Task SetAsync(string key, string value)
    {
        await AppDatabase.EnsureInitializedAsync();

        using var connection = new SqliteConnection(AppDatabase.ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO settings (key, value) VALUES (@key, @value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value
            """;
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@value", value);

        await command.ExecuteNonQueryAsync();
    }
}
