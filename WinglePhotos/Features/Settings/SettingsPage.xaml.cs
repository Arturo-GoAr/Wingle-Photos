using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinglePhotos.Features.PhotoSources;

namespace WinglePhotos.Features.Settings;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = App.Services.GetRequiredService<SettingsViewModel>();

    public SettingsPage()
    {
        InitializeComponent();
    }

    private void RemoveSourceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PhotoSource source })
        {
            _ = ViewModel.Library.RemoveSourceCommand.ExecuteAsync(source);
        }
    }
}
