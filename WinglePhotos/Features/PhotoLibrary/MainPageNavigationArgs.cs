using WinglePhotos.Features.PhotoSources;

namespace WinglePhotos.Features.PhotoLibrary;

/// <summary>How the sidebar asked the library grid to be filtered when navigating in.</summary>
public sealed record MainPageNavigationArgs(bool FavoritesOnly, PhotoSource? Folder, MediaKindFilter MediaKind = MediaKindFilter.All);
