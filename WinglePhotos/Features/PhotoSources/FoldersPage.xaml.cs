using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using WinglePhotos.Features.PhotoLibrary;

namespace WinglePhotos.Features.PhotoSources;

public sealed partial class FoldersPage : Page
{
    public MainViewModel ViewModel { get; } = App.Services.GetRequiredService<MainViewModel>();

    public FoldersPage()
    {
        InitializeComponent();
    }

    private void FoldersListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not PhotoSource { IsAvailable: true } source)
        {
            return;
        }

        Frame.Navigate(typeof(MainPage), new MainPageNavigationArgs(FavoritesOnly: false, Folder: source));
    }

    private async void RemoveFolder_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: PhotoSource source })
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Quitar carpeta",
            Content = $"\"{source.DisplayPath}\" se quitará de tus fuentes. Las fotos y videos no se eliminarán del disco.",
            PrimaryButtonText = "Quitar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.RemoveSourceCommand.ExecuteAsync(source);
    }
}
