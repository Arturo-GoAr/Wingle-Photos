using Microsoft.Data.Sqlite;
using Windows.Storage;

namespace WinglePhotos.Shared;

/// <summary>
/// Single SQLite database backing the app's configuration (settings key/value
/// pairs and the photo-source list). One file, schema created on first use —
/// callers open their own short-lived connection per operation via
/// <see cref="ConnectionString"/> after awaiting <see cref="EnsureInitializedAsync"/>.
/// </summary>
public static class AppDatabase
{
    private static readonly string DbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "wingle.db");
    private static readonly Lazy<Task> Initialization = new(InitializeAsync);

    public static string ConnectionString => $"Data Source={DbPath}";

    public static Task EnsureInitializedAsync() => Initialization.Value;

    private static async Task InitializeAsync()
    {
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS photo_sources (
                token TEXT PRIMARY KEY,
                display_path TEXT NOT NULL,
                is_default INTEGER NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();
    }
}
