namespace WinglePhotos.Features.Favorites;

/// <summary>
/// Tracks which photos are marked as favorites, keyed by <c>PhotoItem.Key</c>
/// (path + last-modified) so favorites survive being re-scanned from disk.
/// </summary>
public interface IFavoritesService
{
    Task LoadAsync();

    bool IsFavorite(string key);

    Task ToggleFavoriteAsync(string key);
}
