using WinglePhotos.Features.PhotoLibrary;

namespace WinglePhotos.Features.Viewer;

/// <summary>
/// Carries the clicked photo's context into the viewer: the ordered list it was
/// clicked from (for prev/next) and the owning MainViewModel, so the viewer reuses
/// MainViewModel's favorite/delete commands instead of duplicating that logic.
/// </summary>
public sealed record PhotoViewerNavigationParameter(
    MainViewModel LibraryViewModel,
    IReadOnlyList<PhotoItem> Items,
    int StartIndex);
