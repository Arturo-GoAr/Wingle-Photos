using Microsoft.UI.Xaml;

namespace WinglePhotos.Features.Settings;

/// <summary>
/// Applies and persists the app's theme preference (system/light/dark).
/// Single place that knows both how to read the saved value and how to
/// push it onto the root window, so App startup and the Settings page
/// stay in sync without duplicating the SQLite read/write logic.
/// </summary>
public interface IThemeService
{
    ElementTheme Current { get; }

    Task<ElementTheme> LoadAndApplyAsync();

    Task ApplyAsync(ElementTheme theme);
}
