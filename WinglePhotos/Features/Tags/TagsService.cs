using System.Text.Json;
using Windows.Storage;

namespace WinglePhotos.Features.Tags;

public sealed class TagsService : ITagsService
{
    private const string FileName = "tags.json";

    private readonly Dictionary<string, List<string>> tagsByKey = new();
    private readonly SortedSet<string> allTags = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim persistLock = new(1, 1);

    public IReadOnlyList<string> AllTags => allTags.ToList();

    public async Task LoadAsync()
    {
        if (await ApplicationData.Current.LocalFolder.TryGetItemAsync(FileName) is not StorageFile file)
        {
            return;
        }

        var json = await FileIO.ReadTextAsync(file);
        var stored = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? new();

        tagsByKey.Clear();
        allTags.Clear();
        foreach (var (key, tags) in stored)
        {
            tagsByKey[key] = tags;
            foreach (var tag in tags)
            {
                allTags.Add(tag);
            }
        }
    }

    public IReadOnlyList<string> GetTags(string key) =>
        tagsByKey.TryGetValue(key, out var tags) ? tags : Array.Empty<string>();

    public async Task AddTagAsync(string key, string tag)
    {
        tag = tag.Trim();
        if (tag.Length == 0)
        {
            return;
        }

        if (!tagsByKey.TryGetValue(key, out var tags))
        {
            tags = new List<string>();
            tagsByKey[key] = tags;
        }

        if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            tags.Add(tag);
            allTags.Add(tag);
            await PersistAsync();
        }
    }

    public async Task RemoveTagAsync(string key, string tag)
    {
        if (!tagsByKey.TryGetValue(key, out var tags))
        {
            return;
        }

        if (tags.RemoveAll(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            if (tags.Count == 0)
            {
                tagsByKey.Remove(key);
            }

            RebuildAllTags();
            await PersistAsync();
        }
    }

    private void RebuildAllTags()
    {
        allTags.Clear();
        foreach (var tags in tagsByKey.Values)
        {
            foreach (var tag in tags)
            {
                allTags.Add(tag);
            }
        }
    }

    private async Task PersistAsync()
    {
        await persistLock.WaitAsync();
        try
        {
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, JsonSerializer.Serialize(tagsByKey));
        }
        finally
        {
            persistLock.Release();
        }
    }
}
