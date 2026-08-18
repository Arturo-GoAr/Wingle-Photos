using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage;
using WinglePhotos.Features.Favorites;
using WinglePhotos.Features.PhotoSources;

namespace WinglePhotos.Features.PhotoLibrary;

public partial class MainViewModel : ObservableObject
{
    private const int FlushBatchSize = 250;

    private readonly IPhotoSourceService sourceService;
    private readonly IPhotoEnumerationService enumerationService;
    private readonly IThumbnailCacheService thumbnailCacheService;
    private readonly IFavoritesService favoritesService;

    private readonly List<PhotoItem> allItems = new();
    private readonly Dictionary<DateOnly, PhotoDateGroup> groupsByDate = new();

    private CancellationTokenSource? scanCancellation;
    private Task scanTask = Task.CompletedTask;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool showFavoritesOnly;

    [ObservableProperty]
    private int photoCount;

    public bool IsEmpty => !IsLoading && PhotoCount == 0;

    public ObservableCollection<PhotoDateGroup> Groups { get; } = new();

    public ObservableCollection<PhotoSource> Sources => sourceService.Sources;

    public MainViewModel(
        IPhotoSourceService sourceService,
        IPhotoEnumerationService enumerationService,
        IThumbnailCacheService thumbnailCacheService,
        IFavoritesService favoritesService)
    {
        this.sourceService = sourceService;
        this.enumerationService = enumerationService;
        this.thumbnailCacheService = thumbnailCacheService;
        this.favoritesService = favoritesService;
    }

    public async Task InitializeAsync()
    {
        await favoritesService.LoadAsync();
        await sourceService.LoadAsync();
        await RescanAsync();
    }

    [RelayCommand]
    private async Task RescanAsync()
    {
        scanCancellation?.Cancel();
        try
        {
            await scanTask;
        }
        catch (OperationCanceledException)
        {
            // Expected: the previous scan was cancelled by the line above.
        }

        var cts = new CancellationTokenSource();
        scanCancellation = cts;
        scanTask = RunScanAsync(cts);
        await scanTask;
    }

    private async Task RunScanAsync(CancellationTokenSource cts)
    {
        allItems.Clear();
        Groups.Clear();
        groupsByDate.Clear();
        PhotoCount = 0;
        IsLoading = true;

        try
        {
            var availableSources = sourceService.Sources.Where(s => s.IsAvailable).ToList();
            var pending = new Dictionary<DateOnly, List<PhotoItem>>();
            var pendingCount = 0;

            await foreach (var item in enumerationService.EnumerateAsync(availableSources, cts.Token))
            {
                item.IsFavorite = favoritesService.IsFavorite(item.Key);
                allItems.Add(item);
                PhotoCount++;

                if (!ShowFavoritesOnly || item.IsFavorite)
                {
                    Buffer(pending, item);
                    pendingCount++;
                }

                if (pendingCount >= FlushBatchSize)
                {
                    Flush(pending);
                    pendingCount = 0;
                }
            }

            Flush(pending);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer scan (sources changed) — the newer scan owns the UI now.
        }
        finally
        {
            if (scanCancellation == cts)
            {
                IsLoading = false;
            }
        }
    }

    private static void Buffer(Dictionary<DateOnly, List<PhotoItem>> pending, PhotoItem item)
    {
        var date = DateOnly.FromDateTime(item.DateTaken.LocalDateTime);
        if (!pending.TryGetValue(date, out var list))
        {
            list = new List<PhotoItem>();
            pending[date] = list;
        }

        list.Add(item);
    }

    private void Flush(Dictionary<DateOnly, List<PhotoItem>> pending)
    {
        if (pending.Count == 0)
        {
            return;
        }

        foreach (var (date, items) in pending)
        {
            GetOrCreateGroup(date).AddRange(items);
        }

        pending.Clear();
    }

    private PhotoDateGroup GetOrCreateGroup(DateOnly date)
    {
        if (groupsByDate.TryGetValue(date, out var group))
        {
            return group;
        }

        group = new PhotoDateGroup(date);
        groupsByDate[date] = group;

        var insertIndex = 0;
        while (insertIndex < Groups.Count && Groups[insertIndex].Date > date)
        {
            insertIndex++;
        }

        Groups.Insert(insertIndex, group);
        return group;
    }

    [RelayCommand]
    private async Task AddSourceAsync()
    {
        var added = await sourceService.AddSourceAsync(App.WindowHandle);
        if (added is not null)
        {
            await RescanAsync();
        }
    }

    [RelayCommand]
    private async Task RemoveSourceAsync(PhotoSource source)
    {
        await sourceService.RemoveSourceAsync(source);
        await RescanAsync();
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(PhotoItem item)
    {
        await favoritesService.ToggleFavoriteAsync(item.Key);
        item.IsFavorite = favoritesService.IsFavorite(item.Key);

        if (ShowFavoritesOnly && !item.IsFavorite)
        {
            RemoveFromGroups(item);
        }
    }

    [RelayCommand]
    public async Task DeletePhotoAsync(PhotoItem item)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(item.Path);
            await file.DeleteAsync(StorageDeleteOption.Default);
        }
        catch (Exception)
        {
            return;
        }

        allItems.Remove(item);
        PhotoCount--;
        RemoveFromGroups(item);
    }

    [RelayCommand]
    private async Task LoadThumbnailAsync(PhotoItem item)
    {
        if (item.Thumbnail is not null)
        {
            return;
        }

        item.Thumbnail = await thumbnailCacheService.GetThumbnailAsync(item, CancellationToken.None);
    }

    partial void OnShowFavoritesOnlyChanged(bool value) => RebuildGroupsFromAllItems();

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));

    partial void OnPhotoCountChanged(int value) => OnPropertyChanged(nameof(IsEmpty));

    private void RebuildGroupsFromAllItems()
    {
        Groups.Clear();
        groupsByDate.Clear();

        var pending = new Dictionary<DateOnly, List<PhotoItem>>();
        var source = ShowFavoritesOnly ? allItems.Where(i => i.IsFavorite) : allItems;
        foreach (var item in source)
        {
            Buffer(pending, item);
        }

        Flush(pending);
    }

    private void RemoveFromGroups(PhotoItem item)
    {
        var date = DateOnly.FromDateTime(item.DateTaken.LocalDateTime);
        if (!groupsByDate.TryGetValue(date, out var group))
        {
            return;
        }

        if (group.Remove(item) && group.Count == 0)
        {
            groupsByDate.Remove(date);
            Groups.Remove(group);
        }
    }
}
