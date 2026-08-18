# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Wingle Photos — a WinUI 3 desktop app (packaged, MSIX) that browses photos and videos from user-chosen source folders (Windows Pictures and Videos libraries included by default), grouped by date like Google Photos. See `README.md` for the full feature list (favorites, tags, RAW/NEF support, video playback, details modal, full-screen viewer, sidebar navigation, SQLite-backed settings).

## Commands

All commands run from the `WinglePhotos/` project directory (not the repo root).

```bash
cd WinglePhotos
dotnet build      # build
dotnet run        # build, register the MSIX package, and launch
```

There is no test suite and no lint step configured in this repo.

### Environment requirements

- .NET SDK 9+
- Windows Developer Mode enabled (Settings → Privacy & security → For developers) — required for `dotnet run` to launch the packaged app without a signed certificate.
- No Visual Studio required. `Microsoft.Windows.SDK.BuildTools.WinApp` (referenced in the `.csproj`) adds `dotnet run` support for packaged WinUI 3 apps directly from the CLI.

### Known build warnings

`MVVMTK0045` fires on every `[ObservableProperty]` field — see "CommunityToolkit.Mvvm gotcha" below for why the field-backed syntax is used instead of the suggested partial-property syntax. It's suppressed project-wide via `<NoWarn>MVVMTK0045</NoWarn>` in `WinglePhotos.csproj` rather than "fixed" by switching syntax, since that switch is what broke the build previously.

## Architecture

### Screaming architecture: organized by feature, not by layer

Code lives under `Features/<FeatureName>/`, each folder self-contained with its own models, service interfaces, service implementations, view models, and pages:

```
Features/
├── PhotoLibrary/   # PhotoItem, PhotoDateGroup, enumeration, thumbnail cache, MainViewModel + MainPage (the grouped grid), PhotoDetailsDialog, AddTagDialog
├── PhotoSources/   # PhotoSource model, FolderPicker + FutureAccessList management, FoldersPage (browse by folder, remove via kebab)
├── Favorites/      # Favorites persistence (JSON file in LocalFolder)
├── Tags/           # User-defined tags/categories persistence (JSON file in LocalFolder)
├── Viewer/         # Full-screen photo/video viewer (PhotoViewerPage)
└── Settings/       # SQLite-backed settings store, theme service, SettingsPage
Shared/             # Cross-feature utilities only (converters, AppDatabase, BulkObservableCollection, PhotoKey, ImageFormats)
```

When adding a feature, follow this convention: new `Features/<Name>/` folder, an `I<Name>Service` interface + implementation if it needs a service, a page + view model if it needs UI, registered in `App.xaml.cs`'s `BuildServiceProvider()`.

### Dependency injection / composition root

`App.xaml.cs` builds a `Microsoft.Extensions.DependencyInjection` `ServiceProvider` (`App.Services`, static). Every service is registered behind an interface (`IPhotoSourceService`, `IPhotoEnumerationService`, `IThumbnailCacheService`, `IFavoritesService`, `ITagsService`, `ISettingsStore`, `IThemeService`). Pages resolve what they need via `App.Services.GetRequiredService<T>()` in their constructor — there is no page-level constructor injection because WinUI's `Frame.Navigate(Type)` requires a parameterless constructor.

`MainViewModel` and `SettingsViewModel` are registered **singleton**, not transient. This is deliberate: `MainViewModel` holds the full in-memory photo list (`allItems`) and the scan state. Sidebar navigation (Todo / Fotos / Videos / Favoritos / Carpetas) re-resolves the *same* `MainViewModel` instance and just re-filters in memory via `ApplyFilter(favoritesOnly, folder, mediaKind)` — it does not re-scan the disk. If you ever need a second, independent view of the library, don't make `MainViewModel` transient to solve it; that would silently reintroduce full re-scans on every sidebar click. Add a filtering parameter or a new, separate view model instead.

### Navigation

`MainWindow.xaml` hosts a `NavigationView` (the sidebar) wrapping a `Frame`. `MainWindow.xaml.cs`'s `RootNavigationView_SelectionChanged` switches on each item's `Tag` and calls `RootFrame.Navigate(...)`. Pages that need context on navigation use a parameter record (e.g. `MainPageNavigationArgs(bool FavoritesOnly, PhotoSource? Folder, MediaKindFilter MediaKind = MediaKindFilter.All)`, `PhotoViewerNavigationParameter`) read in `OnNavigatedTo`. The sidebar tabs are: Todo (`all`), Fotos (`photos`), Videos (`videos`), Favoritos (`favorites`), Carpetas (`folders`) — the first four all navigate to `MainPage` with a different `MediaKindFilter`/`FavoritesOnly` combination and reuse the same in-memory scan.

### Photos vs. videos

`Shared/ImageFormats.cs` recognizes three extension sets: `Standard` (photo formats), `Raw`, and `Video` (`.mp4`, `.m4v`, `.mov`, `.mkv`, `.avi`, `.wmv`, `.webm`). `PhotoEnumerationService` feeds all three into `QueryOptions.FileTypeFilter` with `IndexerOption = IndexerOption.DoNotUseIndexer` — the Windows Search indexer silently drops extensions it doesn't recognize as a known content type even though the filter is documented as extension-based, so enumeration forces a live file-system walk instead of trusting the index. `PhotoItem.IsVideo` distinguishes the two for filtering (`MediaKindFilter.Photos`/`.Videos` in `MainViewModel.MatchesFilter`), the grid's video badge, and the viewer's `MediaPlayerElement` vs. `Image` branch in `PhotoViewerPage.LoadCurrentAsync`. Video thumbnails/posters reuse `ThumbnailCacheService` unchanged — `StorageFile.GetThumbnailAsync` generates a frame-grab poster for video the same way it generates one for RAW photos.

### Default sources: Pictures and Videos libraries

`PhotoSourceService.LoadAsync` seeds two non-removable default sources on first run: the Pictures library (sentinel token `__pictures__`, requires the `picturesLibrary` manifest capability) and the Videos library (sentinel token `__videos__`, requires `videosLibrary`). `ResolveAsync` switches on the token to pick `KnownFolders.PicturesLibrary` / `KnownFolders.VideosLibrary` for these two; every other token resolves through `StorageApplicationPermissions.FutureAccessList.GetFolderAsync`. `PhotoSource.CanRemove` (`!IsDefault`) gates the "Quitar carpeta" kebab option on `FoldersPage` so neither default source can be removed from the UI.

### Why photo enumeration uses Windows.Storage APIs, not System.IO

`Features/PhotoLibrary/PhotoEnumerationService.cs` enumerates via `StorageFolder.CreateFileQueryWithOptions` / `StorageFileQueryResult`, not `System.IO.Directory`. This is required, not a style choice: the default Pictures source is granted through the `picturesLibrary` manifest capability, which unlocks the WinRT Storage API broker but does **not** add an NTFS ACL entry for the package — plain `System.IO` calls throw `UnauthorizedAccessException` there even though they'd work fine on a folder granted via `FolderPicker`. Using the Storage API everywhere means one code path works for both access-grant mechanisms.

### Why thumbnail loading hooks `ContainerContentChanging`, not `Loaded`

`MainPage.xaml.cs` loads thumbnails lazily from `GridView.ContainerContentChanging`, not from a per-item `Loaded` event. This was a real bug fixed during development: with large batched collection updates (`BulkObservableCollection.AddRange`, used so a scan of thousands of photos doesn't fire one `CollectionChanged` per item), WinUI recycles `GridView` containers without re-firing `Loaded` — so a container's image could stay bound to whichever `PhotoItem` first loaded it, silently going stale as new data flowed in. `ContainerContentChanging` fires on every rebind, including recycled containers, so it's the correct hook for lazy-loading data in any virtualized list here.

### Thumbnail vs. preview generation

`IThumbnailCacheService` has two methods, both going through `StorageFile.GetThumbnailAsync` with **`ThumbnailMode.PicturesView`** (not `SingleItem`) and caching the result to disk as JPEG:
- `GetThumbnailAsync` — 320px, for the grid.
- `GetPreviewAsync` — 1600px, for the full-screen viewer.

Both intentionally use the same `ThumbnailMode.PicturesView`: it reads the fast embedded/cached preview. `ThumbnailMode.SingleItem` was tried for the viewer and caused multi-second load times on RAW (NEF) files because it can force a full re-decode instead of reading the embedded JPEG preview. If you touch this code, keep both modes in sync unless you have a specific reason not to — RAW performance regresses immediately if `GetPreviewAsync` drifts from `GetThumbnailAsync`'s mode.

RAW support (NEF, CR2/CR3, ARW, DNG, ORF, RW2, RAF, PEF, SRW) is the embedded JPEG preview only, never a full RAW decode — see `Shared/ImageFormats.cs` for the recognized extension list.

### Full-screen viewer image sizing

`Features/Viewer/PhotoViewerPage.xaml.cs`'s `UpdateImageSize()` computes the fit-to-window size manually from the decoded bitmap's `PixelWidth`/`PixelHeight` and the `ScrollViewer` viewport, then sets the `Image`'s explicit `Width`/`Height`. This is required, not optional: a `ScrollViewer` gives its content unconstrained space, so `Stretch="Uniform"` alone does nothing — without an explicit computed size, images open at native pixel size (large photos overflow the window) instead of fit-to-window. `ZoomMode="Enabled"` on the `ScrollViewer` then zooms from that fitted baseline.

### Persistence

Two separate SQLite tables in one file (`Shared/AppDatabase.cs`, `ApplicationData.Current.LocalFolder/wingle.db`, schema created lazily via `AppDatabase.EnsureInitializedAsync()`):
- `settings` — generic key/value store (`Features/Settings/SqliteSettingsStore.cs`), currently used for the theme preference (`Features/Settings/ThemeService.cs`).
- `photo_sources` — the list of source folders (`Features/PhotoSources/PhotoSourceService.cs`). Stores the `FutureAccessList` **token**, never the raw path — packaged apps must re-resolve folders through `StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token)` on every launch since the token, not the path, is what carries the access grant. The default Pictures source uses a sentinel token (`__pictures__`) and resolves through `KnownFolders.PicturesLibrary` instead.

Favorites (`Features/Favorites/FavoritesService.cs`) are a separate JSON file in `LocalFolder`, keyed by `PhotoKey.For(path, dateTaken)` (`Shared/PhotoKey.cs`) — a hash of path + last-modified, not the raw path, so favorites survive being re-scanned from disk.

Tags (`Features/Tags/TagsService.cs`) follow the same pattern: a separate JSON file (`tags.json`) mapping `PhotoKey` → `List<string>`, plus an in-memory `SortedSet<string>` of every tag ever used (`AllTags`) for future autocomplete. `MainViewModel.AddTagAsync`/`RemoveTagAsync` are plain async methods, not `[RelayCommand]`s — they take two parameters (`PhotoItem`, tag text), which doesn't map cleanly onto the single-parameter `ICommand` shape the rest of the view model uses for grid/viewer bindings. They're invoked directly from code-behind (`AddTagDialog.ShowAsync`) instead.

### "Ver detalles" and "Agregar etiqueta" dialogs

`Features/PhotoLibrary/PhotoDetailsDialog.cs` and `AddTagDialog.cs` are static helpers (not services) that build and show a `ContentDialog` given a `PhotoItem` and a `XamlRoot`. Both `MainPage` (grid kebab/right-click) and `PhotoViewerPage` (overflow menu) call the same helper rather than each having its own dialog-building logic. `PhotoDetailsDialog` fetches metadata (`GetBasicPropertiesAsync`, `GetImagePropertiesAsync`/`GetVideoPropertiesAsync`) on demand when the dialog opens, not during the scan — those calls are comparatively slow and only a handful of items get inspected per session, so eager fetching for the whole library would be wasted work.

### Kebab and right-click on grid items

`MainPage.xaml`'s `DataTemplate` defines a single `MenuFlyout` (`x:Key="ItemMenuFlyout"`) in the item `Grid`'s own `Grid.Resources` — since each `GridView` item gets its own realized `Grid` instance, this resource is per-item, not shared/reused across rows despite the `StaticResource` lookup. It's wired to both the item's kebab `Button.Flyout` and the `Grid.ContextFlyout` (right-click), so right-click and the kebab button open the exact same menu: Favorito, Agregar etiqueta, Ver detalles, Eliminar. Don't try to factor this into one shared top-level resource — a single `MenuFlyout` instance can't carry a different `PhotoItem` context (`Tag="{x:Bind}"`) per row.

### Delete always goes to the Recycle Bin

Every delete path (`MainViewModel.DeletePhotoAsync`, called from the grid kebab, the viewer's overflow menu, and nowhere else) calls `StorageFile.DeleteAsync(StorageDeleteOption.Default)` — never `PermanentDelete`. `Default` tries the Recycle Bin first and only falls back to a permanent delete if the Recycle Bin is genuinely unavailable (e.g. the file lives outside a local NTFS volume); this is the strongest guarantee the WinRT Storage API offers. If you add another delete entry point, route it through `MainViewModel.DeletePhotoCommand` rather than calling `DeleteAsync` directly, so this guarantee and the in-memory list/group cleanup stay in one place.

### CommunityToolkit.Mvvm gotcha: field-backed `[ObservableProperty]`, not partial properties

Every `[ObservableProperty]` in this codebase uses the classic private-field syntax (`[ObservableProperty] private bool isLoading;`), not the newer C# 13 partial-property syntax (`[ObservableProperty] public partial bool IsLoading { get; set; }`). This was a deliberate downgrade: the partial-property source generator failed to produce an implementation in this project's build setup (even in the freshly-scaffolded template code), causing `CS9248`/`CS0759` compile errors. The field-backed syntax is confirmed working. Don't "fix" existing properties to the partial-property style without first verifying a clean build — it broke the entire project the first time it was tried.
