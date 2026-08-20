using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using WinglePhotos.Shared;
using WinRT.Interop;

namespace WinglePhotos.Features.PhotoSources;

public sealed class PhotoSourceService : IPhotoSourceService
{
    private const string DefaultPicturesToken = "__pictures__";
    private const string DefaultVideosToken = "__videos__";

    public ObservableCollection<PhotoSource> Sources { get; } = new();

    public async Task LoadAsync()
    {
        var stored = await ReadStoredRecordsAsync();

        if (stored.Count == 0)
        {
            var defaultPictures = new StoredSource(DefaultPicturesToken, "Imágenes", true);
            var defaultVideos = new StoredSource(DefaultVideosToken, "Videos", true);
            stored.Add(defaultPictures);
            stored.Add(defaultVideos);
            await UpsertAsync(defaultPictures);
            await UpsertAsync(defaultVideos);
        }

        Sources.Clear();
        foreach (var record in stored)
        {
            Sources.Add(await ResolveAsync(record));
        }
    }

    public async Task<PhotoSource?> AddSourceAsync(nint ownerWindowHandle)
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
        };
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, ownerWindowHandle);

        var folder = await picker.PickSingleFolderAsync();
        if (folder is null)
        {
            return null;
        }

        var token = StorageApplicationPermissions.FutureAccessList.Add(folder);
        var source = new PhotoSource
        {
            Token = token,
            DisplayPath = folder.Path,
            IsDefault = false,
            IsAvailable = true,
            Folder = folder,
        };

        Sources.Add(source);
        await UpsertAsync(new StoredSource(source.Token, source.DisplayPath, source.IsDefault));
        return source;
    }

    public async Task RemoveSourceAsync(PhotoSource source)
    {
        if (source.IsDefault)
        {
            throw new InvalidOperationException("The default Pictures source cannot be removed.");
        }

        if (StorageApplicationPermissions.FutureAccessList.ContainsItem(source.Token))
        {
            StorageApplicationPermissions.FutureAccessList.Remove(source.Token);
        }

        Sources.Remove(source);
        await DeleteAsync(source.Token);
    }

    private static async Task<PhotoSource> ResolveAsync(StoredSource record)
    {
        try
        {
            var folder = record.Token switch
            {
                DefaultPicturesToken => KnownFolders.PicturesLibrary,
                DefaultVideosToken => KnownFolders.VideosLibrary,
                _ => await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(record.Token),
            };

            return new PhotoSource
            {
                Token = record.Token,
                DisplayPath = record.DisplayPath,
                IsDefault = record.IsDefault,
                IsAvailable = true,
                Folder = folder,
            };
        }
        catch (Exception)
        {
            // Folder moved, deleted, or its permission token expired (or, for a
            // default source, the library capability isn't available on this
            // machine/account) — surface as unavailable instead of crashing the
            // whole source list.
            return new PhotoSource
            {
                Token = record.Token,
                DisplayPath = record.DisplayPath,
                IsDefault = record.IsDefault,
                IsAvailable = false,
                Folder = null,
            };
        }
    }

    private static async Task<List<StoredSource>> ReadStoredRecordsAsync()
    {
        await AppDatabase.EnsureInitializedAsync();

        using var connection = new SqliteConnection(AppDatabase.ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT token, display_path, is_default FROM photo_sources";

        var results = new List<StoredSource>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new StoredSource(reader.GetString(0), reader.GetString(1), reader.GetInt64(2) != 0));
        }

        return results;
    }

    private static async Task UpsertAsync(StoredSource record)
    {
        await AppDatabase.EnsureInitializedAsync();

        using var connection = new SqliteConnection(AppDatabase.ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO photo_sources (token, display_path, is_default) VALUES (@token, @path, @isDefault)
            ON CONFLICT(token) DO UPDATE SET display_path = excluded.display_path, is_default = excluded.is_default
            """;
        command.Parameters.AddWithValue("@token", record.Token);
        command.Parameters.AddWithValue("@path", record.DisplayPath);
        command.Parameters.AddWithValue("@isDefault", record.IsDefault ? 1 : 0);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteAsync(string token)
    {
        await AppDatabase.EnsureInitializedAsync();

        using var connection = new SqliteConnection(AppDatabase.ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM photo_sources WHERE token = @token";
        command.Parameters.AddWithValue("@token", token);

        await command.ExecuteNonQueryAsync();
    }

    private sealed record StoredSource(string Token, string DisplayPath, bool IsDefault);
}
