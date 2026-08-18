namespace WinglePhotos.Features.Settings;

/// <summary>
/// Generic key/value configuration store, backed by SQLite so app settings
/// survive restarts without relying on ApplicationData.LocalSettings.
/// </summary>
public interface ISettingsStore
{
    Task<string?> GetAsync(string key);

    Task SetAsync(string key, string value);
}
