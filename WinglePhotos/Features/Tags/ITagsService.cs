namespace WinglePhotos.Features.Tags;

/// <summary>
/// Tracks user-defined tags/categories per photo or video, keyed by <c>PhotoItem.Key</c>
/// (path + last-modified) so tags survive being re-scanned from disk.
/// </summary>
public interface ITagsService
{
    Task LoadAsync();

    IReadOnlyList<string> GetTags(string key);

    /// <summary>All tags used anywhere in the library, for autocomplete suggestions.</summary>
    IReadOnlyList<string> AllTags { get; }

    Task AddTagAsync(string key, string tag);

    Task RemoveTagAsync(string key, string tag);
}
