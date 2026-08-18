using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace WinglePhotos.Features.PhotoLibrary;

public sealed class ThumbnailCacheService : IThumbnailCacheService
{
    private const int GridSize = 320;
    private const int PreviewSize = 1600;

    private readonly Task<StorageFolder> gridCacheFolderTask =
        ApplicationData.Current.LocalFolder.CreateFolderAsync("ThumbnailCache", CreationCollisionOption.OpenIfExists).AsTask();

    private readonly Task<StorageFolder> previewCacheFolderTask =
        ApplicationData.Current.LocalFolder.CreateFolderAsync("PreviewCache", CreationCollisionOption.OpenIfExists).AsTask();

    public Task<BitmapImage?> GetThumbnailAsync(PhotoItem item, CancellationToken cancellationToken) =>
        LoadOrGenerateAsync(item, gridCacheFolderTask, GridSize, ThumbnailMode.PicturesView, cancellationToken);

    public Task<BitmapImage?> GetPreviewAsync(PhotoItem item, CancellationToken cancellationToken) =>
        LoadOrGenerateAsync(item, previewCacheFolderTask, PreviewSize, ThumbnailMode.SingleItem, cancellationToken);

    private static async Task<BitmapImage?> LoadOrGenerateAsync(
        PhotoItem item, Task<StorageFolder> cacheFolderTask, int size, ThumbnailMode mode, CancellationToken cancellationToken)
    {
        var cacheFolder = await cacheFolderTask;
        var cacheFileName = $"{item.Key}.jpg";

        try
        {
            if (await cacheFolder.TryGetItemAsync(cacheFileName) is StorageFile cachedFile)
            {
                return await LoadBitmapAsync(cachedFile);
            }

            var sourceFile = await StorageFile.GetFileFromPathAsync(item.Path);
            using var thumbnailStream = await sourceFile.GetThumbnailAsync(mode, (uint)size, ThumbnailOptions.ResizeThumbnail);
            if (thumbnailStream is null || thumbnailStream.Size == 0)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var cacheFile = await cacheFolder.CreateFileAsync(cacheFileName, CreationCollisionOption.ReplaceExisting);
            using (var cacheStream = await cacheFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                await RandomAccessStream.CopyAsync(thumbnailStream, cacheStream);
            }

            return await LoadBitmapAsync(cacheFile);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Missing file, no thumbnail provider for this RAW variant, or access
            // revoked mid-scan — degrade to "no preview" instead of throwing.
            return null;
        }
    }

    private static async Task<BitmapImage> LoadBitmapAsync(StorageFile file)
    {
        using var stream = await file.OpenAsync(FileAccessMode.Read);
        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(stream);
        return bitmap;
    }
}
