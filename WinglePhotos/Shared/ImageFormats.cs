namespace WinglePhotos.Shared;

/// <summary>
/// Single source of truth for which file extensions the library recognizes as photos.
/// Consumed by enumeration (filtering) and thumbnail generation (RAW vs standard strategy).
/// </summary>
public static class ImageFormats
{
    public static readonly IReadOnlySet<string> Standard = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".webp", ".heic", ".heif",
    };

    public static readonly IReadOnlySet<string> Raw = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".nef", ".cr2", ".cr3", ".arw", ".dng", ".orf", ".rw2", ".raf", ".pef", ".srw",
    };

    public static bool IsRaw(string extension) => Raw.Contains(extension);

    public static bool IsSupported(string extension) => Standard.Contains(extension) || Raw.Contains(extension);
}
