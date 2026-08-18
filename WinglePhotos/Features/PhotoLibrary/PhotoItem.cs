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

    [ObservableProperty]
    private bool isFavorite;
}
