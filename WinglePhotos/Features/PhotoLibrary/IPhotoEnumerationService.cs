using WinglePhotos.Features.PhotoSources;

namespace WinglePhotos.Features.PhotoLibrary;

/// <summary>
/// Walks a set of source folders and streams the photos found, so a caller can
/// start populating the grid before the whole tree has been scanned.
/// </summary>
public interface IPhotoEnumerationService
{
    IAsyncEnumerable<PhotoItem> EnumerateAsync(
        IReadOnlyList<PhotoSource> sources,
        CancellationToken cancellationToken);
}
