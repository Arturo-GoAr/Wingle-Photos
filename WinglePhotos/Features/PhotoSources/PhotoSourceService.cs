using System.Collections.ObjectModel;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace WinglePhotos.Features.PhotoSources;

public sealed class PhotoSourceService : IPhotoSourceService
{
    private const string SettingsKey = "PhotoSources";
    private const string DefaultToken = "__pictures__";

    public ObservableCollection<PhotoSource> Sources { get; } = new();

    public async Task LoadAsync()
    {
        var stored = ReadStoredRecords();

        if (stored.Count == 0)
        {
            stored.Add(new StoredSource(DefaultToken, "Imágenes", true));
            WriteStoredRecords(stored);
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
        Persist();
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
        Persist();
        await Task.CompletedTask;
    }

    private static async Task<PhotoSource> ResolveAsync(StoredSource record)
    {
        try
        {
            var folder = record.IsDefault
                ? KnownFolders.PicturesLibrary
                : await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(record.Token);

            return new PhotoSource
            {
                Token = record.Token,
                DisplayPath = record.DisplayPath,
                IsDefault = record.IsDefault,
                IsAvailable = true,
                Folder = folder,
            };
        }
        catch (Exception) when (!record.IsDefault)
        {
            // Folder moved, deleted, or its permission token expired — surface as
            // unavailable instead of crashing the whole source list.
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

    private void Persist()
    {
        var records = Sources
            .Select(s => new StoredSource(s.Token, s.DisplayPath, s.IsDefault))
            .ToList();
        WriteStoredRecords(records);
    }

    private static List<StoredSource> ReadStoredRecords()
    {
        if (ApplicationData.Current.LocalSettings.Values[SettingsKey] is not string json)
        {
            return new List<StoredSource>();
        }

        return JsonSerializer.Deserialize<List<StoredSource>>(json) ?? new List<StoredSource>();
    }

    private static void WriteStoredRecords(List<StoredSource> records)
    {
        ApplicationData.Current.LocalSettings.Values[SettingsKey] = JsonSerializer.Serialize(records);
    }

    private sealed record StoredSource(string Token, string DisplayPath, bool IsDefault);
}
