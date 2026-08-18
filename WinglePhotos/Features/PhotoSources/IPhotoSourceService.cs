using System.Collections.ObjectModel;

namespace WinglePhotos.Features.PhotoSources;

/// <summary>
/// Owns the set of folders the library reads photos from: persistence (FutureAccessList
/// tokens, not raw paths — required once the app is packaged), the default Pictures
/// source, and adding/removing sources via the folder picker.
/// </summary>
public interface IPhotoSourceService
{
    ObservableCollection<PhotoSource> Sources { get; }

    Task LoadAsync();

    Task<PhotoSource?> AddSourceAsync(nint ownerWindowHandle);

    Task RemoveSourceAsync(PhotoSource source);
}
