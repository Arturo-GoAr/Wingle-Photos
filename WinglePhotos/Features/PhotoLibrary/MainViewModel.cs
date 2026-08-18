using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage;
using WinglePhotos.Features.Favorites;
using WinglePhotos.Features.PhotoSources;
using WinglePhotos.Features.Tags;

namespace WinglePhotos.Features.PhotoLibrary;

public partial class MainViewModel : ObservableObject
{
    private const int FlushBatchSize = 250;

    private readonly IPhotoSourceService sourceService;
    private readonly IPhotoEnumerationService enumerationService;
    private readonly IThumbnailCacheService thumbnailCacheService;
    private readonly IFavoritesService favoritesService;
    private readonly ITagsService tagsService;

    private readonly List<PhotoItem> allItems = new();
    private readonly Dictionary<DateOnly, PhotoDateGroup> groupsByDate = new();

    private CancellationTokenSource? scanCancellation;
    private Task scanTask = Task.CompletedTask;
    private bool initialized;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool showFavoritesOnly;

    [ObservableProperty]
    private PhotoSource? selectedFolder;

    [ObservableProperty]
    private MediaKindFilter mediaKind = MediaKindFilter.All;

    [ObservableProperty]
    private int photoCount;

    /// <summary>Empty relative to the current filter (favorites/folder), not the whole library.</summary>
    public bool IsEmpty => !IsLoading && Groups.Count == 0;

    public ObservableCollection<PhotoDateGroup> Groups { get; } = new();

    public ObservableCollection<PhotoSource> Sources => sourceService.Sources;

    public MainViewModel(
        IPhotoSourceService sourceService,
        IPhotoEnumerationService enumerationService,
        IThumbnailCacheService thumbnailCacheService,
        IFavoritesService favoritesService,
        ITagsService tagsService)
    {
        this.sourceService = sourceService;
        this.enumerationService = enumerationService;
        this.thumbnailCacheService = thumbnailCacheService;
        this.favoritesService = favoritesService;
        this.tagsService = tagsService;
        Groups.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>
    /// Loads sources/favorites and runs the first scan. Safe to call every time
    /// MainPage is navigated to — the underlying view model is a DI singleton, so
    /// only the first call does real work; later ones are a no-op.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        await favoritesService.LoadAsync();
        await tagsService.LoadAsync();
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
                foreach (var tag in tagsService.GetTags(item.Key))
                {
                    item.Tags.Add(tag);
                }

                allItems.Add(item);
                PhotoCount++;

                if (MatchesFilter(item))
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

    public IReadOnlyList<string> AllTagSuggestions => tagsService.AllTags;

    public async Task AddTagAsync(PhotoItem item, string tag)
    {
        await tagsService.AddTagAsync(item.Key, tag);

        var normalized = tag.Trim();
        if (normalized.Length > 0 && !item.Tags.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            item.Tags.Add(normalized);
        }
    }

    public async Task RemoveTagAsync(PhotoItem item, string tag)
    {
        await tagsService.RemoveTagAsync(item.Key, tag);

        var existing = item.Tags.FirstOrDefault(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            item.Tags.Remove(existing);
        }
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

    /// <summary>Applies all filter dimensions at once, e.g. from sidebar navigation.</summary>
    public void ApplyFilter(bool favoritesOnly, PhotoSource? folder, MediaKindFilter mediaKind = MediaKindFilter.All)
    {
        ShowFavoritesOnly = favoritesOnly;
        SelectedFolder = folder;
        MediaKind = mediaKind;
    }

    private bool MatchesFilter(PhotoItem item)
    {
        if (ShowFavoritesOnly && !item.IsFavorite)
        {
            return false;
        }

        if (MediaKind == MediaKindFilter.Photos && item.IsVideo)
        {
            return false;
        }

        if (MediaKind == MediaKindFilter.Videos && !item.IsVideo)
        {
            return false;
        }

        if (SelectedFolder?.Folder is { } folder &&
            !item.Path.StartsWith(folder.Path, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    partial void OnShowFavoritesOnlyChanged(bool value) => RebuildGroupsFromAllItems();

    partial void OnSelectedFolderChanged(PhotoSource? value) => RebuildGroupsFromAllItems();

    partial void OnMediaKindChanged(MediaKindFilter value) => RebuildGroupsFromAllItems();

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));

    private void RebuildGroupsFromAllItems()
    {
        Groups.Clear();
        groupsByDate.Clear();

        var pending = new Dictionary<DateOnly, List<PhotoItem>>();
        foreach (var item in allItems.Where(MatchesFilter))
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
