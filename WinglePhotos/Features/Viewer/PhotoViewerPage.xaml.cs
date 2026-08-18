using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using WinglePhotos.Features.PhotoLibrary;

namespace WinglePhotos.Features.Viewer;

public sealed partial class PhotoViewerPage : Page
{
    private readonly IThumbnailCacheService thumbnailCacheService = App.Services.GetRequiredService<IThumbnailCacheService>();

    private MainViewModel libraryViewModel = null!;
    private List<PhotoItem> items = new();
    private int currentIndex;

    public PhotoViewerPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var parameter = (PhotoViewerNavigationParameter)e.Parameter;
        libraryViewModel = parameter.LibraryViewModel;
        items = parameter.Items.ToList();
        currentIndex = parameter.StartIndex;

        _ = LoadCurrentAsync();
    }

    private async Task LoadCurrentAsync()
    {
        if (items.Count == 0)
        {
            Frame.GoBack();
            return;
        }

        currentIndex = Math.Clamp(currentIndex, 0, items.Count - 1);
        var item = items[currentIndex];

        LoadingRing.IsActive = true;
        PreviewImage.Source = null;
        ImageScrollViewer.ChangeView(0, 0, 1, disableAnimation: true);

        PositionText.Text = $"{currentIndex + 1} de {items.Count}";
        UpdateFavoriteIcon(item.IsFavorite);

        PreviewImage.Source = await thumbnailCacheService.GetPreviewAsync(item, CancellationToken.None);
        LoadingRing.IsActive = false;
    }

    private void UpdateFavoriteIcon(bool isFavorite) =>
        FavoriteIcon.Glyph = isFavorite ? "" : "";

    private async void Favorite_Click(object sender, RoutedEventArgs e)
    {
        var item = items[currentIndex];
        await libraryViewModel.ToggleFavoriteCommand.ExecuteAsync(item);
        UpdateFavoriteIcon(item.IsFavorite);
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Eliminar foto",
            Content = "La foto se moverá a la papelera de reciclaje. ¿Continuar?",
            PrimaryButtonText = "Eliminar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        var item = items[currentIndex];
        await libraryViewModel.DeletePhotoCommand.ExecuteAsync(item);
        items.RemoveAt(currentIndex);
        await LoadCurrentAsync();
    }

    private void Previous_Click(object sender, RoutedEventArgs e)
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            _ = LoadCurrentAsync();
        }
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (currentIndex < items.Count - 1)
        {
            currentIndex++;
            _ = LoadCurrentAsync();
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e) => Frame.GoBack();

    private void PreviewImage_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) =>
        ImageScrollViewer.ChangeView(0, 0, ImageScrollViewer.ZoomFactor > 1 ? 1 : 2);

    private void PreviousAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        Previous_Click(sender, new RoutedEventArgs());
        args.Handled = true;
    }

    private void NextAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        Next_Click(sender, new RoutedEventArgs());
        args.Handled = true;
    }

    private void BackAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        Back_Click(sender, new RoutedEventArgs());
        args.Handled = true;
    }
}
