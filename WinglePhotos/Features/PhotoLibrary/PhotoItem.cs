using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using WinglePhotos.Shared;

namespace WinglePhotos.Features.PhotoLibrary;

public partial class PhotoItem : ObservableObject
{
    private string? key;

    public required string Path { get; init; }
    public required DateTimeOffset DateTaken { get; init; }
    public required bool IsRaw { get; init; }
    public required bool IsVideo { get; init; }

    public string Key => key ??= PhotoKey.For(Path, DateTaken);

    public ObservableCollection<string> Tags { get; } = new();

    [ObservableProperty]
    private BitmapImage? thumbnail;

    /// <summary>
    /// Guards against duplicate concurrent loads: GridView container recycling
    /// can re-fire ContainerContentChanging for the same item before its first
    /// load finishes, which without this would start another overlapping
    /// StorageFile/COM call for the same photo.
    /// </summary>
    internal bool IsThumbnailLoading { get; set; }

    /// <summary>
    /// Set while a thumbnail load is in flight so <see cref="MainPage"/> can cancel it if the
    /// GridView recycles this item's container before the load finishes — otherwise scrolling
    /// past an item still makes it wait its turn behind items the user already passed.
    /// </summary>
    internal CancellationTokenSource? ThumbnailLoadCts { get; set; }

    [ObservableProperty]
    private bool isFavorite;
}
