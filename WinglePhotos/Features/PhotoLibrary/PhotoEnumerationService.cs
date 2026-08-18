using System.Runtime.CompilerServices;
using Windows.Storage;
using Windows.Storage.Search;
using WinglePhotos.Features.PhotoSources;
using WinglePhotos.Shared;

namespace WinglePhotos.Features.PhotoLibrary;

/// <summary>
/// Enumerates via the Windows.Storage query APIs (StorageFolder/StorageFileQueryResult)
/// rather than System.IO. The default Pictures source only grants access through the
/// picturesLibrary capability, which unlocks the Storage API broker but does not add an
/// NTFS ACL entry for the package — plain System.IO calls throw UnauthorizedAccessException
/// there even though FolderPicker-granted folders would allow them. Using Storage APIs
/// everywhere means one code path works for both access-grant mechanisms.
/// </summary>
public sealed class PhotoEnumerationService : IPhotoEnumerationService
{
    private const uint PageSize = 200;

    public async IAsyncEnumerable<PhotoItem> EnumerateAsync(
        IReadOnlyList<PhotoSource> sources,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var allExtensions = ImageFormats.Standard.Concat(ImageFormats.Raw).Concat(ImageFormats.Video).ToList();

        foreach (var source in sources)
        {
            if (!source.IsAvailable || source.Folder is null)
            {
                continue;
            }

            StorageFileQueryResult queryResult;
            try
            {
                var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, allExtensions)
                {
                    FolderDepth = FolderDepth.Deep,
                    // The Windows Search indexer silently returns zero results for extensions
                    // it doesn't recognize as a known content type (e.g. .mkv, .webm, .avi) even
                    // though QueryOptions' fileTypeFilter is documented as extension-based. Force
                    // a live file-system walk so every extension in the filter is honored.
                    IndexerOption = IndexerOption.DoNotUseIndexer,
                };
                queryResult = source.Folder.CreateFileQueryWithOptions(queryOptions);
            }
            catch (Exception)
            {
                // Query indexing unavailable for this folder — skip it rather than
                // aborting the whole scan.
                continue;
            }

            uint startIndex = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                IReadOnlyList<StorageFile> files;
                try
                {
                    files = await queryResult.GetFilesAsync(startIndex, PageSize);
                }
                catch (Exception)
                {
                    break;
                }

                if (files.Count == 0)
                {
                    break;
                }

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var extension = System.IO.Path.GetExtension(file.Name);
                    if (!ImageFormats.IsSupported(extension))
                    {
                        continue;
                    }

                    yield return new PhotoItem
                    {
                        Path = file.Path,
                        DateTaken = file.DateCreated,
                        IsRaw = ImageFormats.IsRaw(extension),
                        IsVideo = ImageFormats.IsVideo(extension),
                    };
                }

                startIndex += (uint)files.Count;
            }
        }
    }
}
