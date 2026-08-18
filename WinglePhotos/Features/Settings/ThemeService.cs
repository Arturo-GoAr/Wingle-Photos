using Microsoft.UI.Xaml;

namespace WinglePhotos.Features.Settings;

public sealed class ThemeService : IThemeService
{
    private const string SettingKey = "theme";

    private readonly ISettingsStore settingsStore;

    public ElementTheme Current { get; private set; } = ElementTheme.Default;

    public ThemeService(ISettingsStore settingsStore)
    {
        this.settingsStore = settingsStore;
    }

    public async Task<ElementTheme> LoadAndApplyAsync()
    {
        var stored = await settingsStore.GetAsync(SettingKey);
        var theme = Enum.TryParse<ElementTheme>(stored, out var parsed) ? parsed : ElementTheme.Default;
        Apply(theme);
        return theme;
    }

    public async Task ApplyAsync(ElementTheme theme)
    {
        Apply(theme);
        await settingsStore.SetAsync(SettingKey, theme.ToString());
    }

    private void Apply(ElementTheme theme)
    {
        Current = theme;
        if (App.Window.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme;
        }
    }
}
