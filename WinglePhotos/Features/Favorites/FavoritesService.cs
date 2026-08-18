using System.Text.Json;
using Windows.Storage;

namespace WinglePhotos.Features.Favorites;

public sealed class FavoritesService : IFavoritesService
{
    private const string FileName = "favorites.json";

    private readonly HashSet<string> favoriteKeys = new();
    private readonly SemaphoreSlim persistLock = new(1, 1);

    public async Task LoadAsync()
    {
        if (await ApplicationData.Current.LocalFolder.TryGetItemAsync(FileName) is not StorageFile file)
        {
            return;
        }

        var json = await FileIO.ReadTextAsync(file);
        var keys = JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();

        favoriteKeys.Clear();
        foreach (var key in keys)
        {
            favoriteKeys.Add(key);
        }
    }

    public bool IsFavorite(string key) => favoriteKeys.Contains(key);

    public async Task ToggleFavoriteAsync(string key)
    {
        if (!favoriteKeys.Remove(key))
        {
            favoriteKeys.Add(key);
        }

        await PersistAsync();
    }

    private async Task PersistAsync()
    {
        await persistLock.WaitAsync();
        try
        {
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, JsonSerializer.Serialize(favoriteKeys));
        }
        finally
        {
            persistLock.Release();
        }
    }
}
