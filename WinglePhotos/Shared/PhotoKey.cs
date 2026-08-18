using System.Security.Cryptography;
using System.Text;

namespace WinglePhotos.Shared;

/// <summary>
/// Builds the stable identity used for favorites and thumbnail-cache lookups.
/// Path + last-write-time survives being read from different source roots,
/// while staying cheap to recompute (no content hashing of the photo itself).
/// </summary>
public static class PhotoKey
{
    public static string For(string path, DateTimeOffset lastModified)
    {
        var raw = $"{path.ToLowerInvariant()}|{lastModified.UtcTicks}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }
}
