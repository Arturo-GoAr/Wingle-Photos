using Microsoft.UI.Xaml.Media.Imaging;

namespace WinglePhotos.Features.PhotoLibrary;

/// <summary>
/// Loads a display-ready thumbnail for a photo, backed by an on-disk cache keyed on
/// <see cref="PhotoItem.Key"/> so repeat visits don't re-decode full images.
/// </summary>
public interface IThumbnailCacheService
{
    /// <summary>Grid-sized (320px) thumbnail, cached on disk.</summary>
    Task<BitmapImage?> GetThumbnailAsync(PhotoItem item, CancellationToken cancellationToken);

    /// <summary>Larger (1600px) preview for the full-screen viewer, cached on disk.
    /// For RAW files this is still the embedded preview, not a full RAW decode.</summary>
    Task<BitmapImage?> GetPreviewAsync(PhotoItem item, CancellationToken cancellationToken);
}
