namespace WinglePhotos.Features.PhotoSources;

public sealed class PhotoSource
{
    public required string Token { get; init; }
    public required string DisplayPath { get; init; }
    public bool IsDefault { get; init; }
    public bool IsAvailable { get; set; } = true;
    public Windows.Storage.StorageFolder? Folder { get; set; }

    public bool IsUnavailable => !IsAvailable;
    public bool CanRemove => !IsDefault;
}
