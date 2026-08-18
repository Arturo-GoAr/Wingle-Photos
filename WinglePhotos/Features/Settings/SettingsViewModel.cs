using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using WinglePhotos.Features.PhotoLibrary;

namespace WinglePhotos.Features.Settings;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService themeService;

    /// <summary>Reused for source management so add/remove here also refreshes the library grid.</summary>
    public MainViewModel Library { get; }

    [ObservableProperty]
    private ElementTheme currentTheme;

    public SettingsViewModel(IThemeService themeService, MainViewModel library)
    {
        this.themeService = themeService;
        Library = library;
        currentTheme = themeService.Current;
    }

    [RelayCommand]
    private async Task SetThemeAsync(string themeName)
    {
        var theme = Enum.Parse<ElementTheme>(themeName);
        await themeService.ApplyAsync(theme);
        CurrentTheme = theme;
    }
}
