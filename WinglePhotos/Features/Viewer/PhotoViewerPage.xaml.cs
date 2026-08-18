using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Media.Core;
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

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        StopVideo();
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

        StopVideo();

        PositionText.Text = $"{currentIndex + 1} de {items.Count}";
        UpdateFavoriteIcon(item.IsFavorite);

        if (item.IsVideo)
        {
            ImageScrollViewer.Visibility = Visibility.Collapsed;
            VideoPlayer.Visibility = Visibility.Visible;
            LoadingRing.IsActive = false;
            PreviewImage.Source = null;

            var sourceFile = await StorageFile.GetFileFromPathAsync(item.Path);
            VideoPlayer.Source = MediaSource.CreateFromStorageFile(sourceFile);
            return;
        }

        ImageScrollViewer.Visibility = Visibility.Visible;
        VideoPlayer.Visibility = Visibility.Collapsed;

        LoadingRing.IsActive = true;
        PreviewImage.Source = null;
        ImageScrollViewer.ChangeView(0, 0, 1, disableAnimation: true);

        PreviewImage.Source = await thumbnailCacheService.GetPreviewAsync(item, CancellationToken.None);
        LoadingRing.IsActive = false;
        UpdateImageSize();
    }

    /// <summary>
    /// Stops and detaches the current video source before switching items so audio/playback
    /// doesn't continue after navigating to a different (possibly non-video) item.
    /// </summary>
    private void StopVideo()
    {
        VideoPlayer.MediaPlayer?.Pause();
        VideoPlayer.Source = null;
    }

    /// <summary>
    /// Sizes the image to exactly fit the viewport at zoom factor 1, computed from the
    /// bitmap's real decoded pixel dimensions. A ScrollViewer gives its content unconstrained
    /// space, so Stretch="Uniform" alone does nothing — without this, the image opens at
    /// native pixel size (often much larger than the window) instead of fit-to-window.
    /// </summary>
    private void UpdateImageSize()
    {
        if (PreviewImage.Source is not BitmapImage { PixelWidth: > 0, PixelHeight: > 0 } bitmap)
        {
            return;
        }

        var viewportWidth = ImageScrollViewer.ActualWidth;
        var viewportHeight = ImageScrollViewer.ActualHeight;
        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            return;
        }

        var scale = Math.Min(viewportWidth / bitmap.PixelWidth, viewportHeight / bitmap.PixelHeight);
        PreviewImage.Width = bitmap.PixelWidth * scale;
        PreviewImage.Height = bitmap.PixelHeight * scale;
    }

    private void ImageScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateImageSize();

    private void UpdateFavoriteIcon(bool isFavorite) =>
        FavoriteIcon.Glyph = isFavorite ? "" : "";

    private async void Favorite_Click(object sender, RoutedEventArgs e)
    {
        var item = items[currentIndex];
        await libraryViewModel.ToggleFavoriteCommand.ExecuteAsync(item);
        UpdateFavoriteIcon(item.IsFavorite);
    }

    private async void AddTag_Click(object sender, RoutedEventArgs e) =>
        await AddTagDialog.ShowAsync(items[currentIndex], libraryViewModel, Content.XamlRoot);

    private async void ViewDetails_Click(object sender, RoutedEventArgs e) =>
        await PhotoDetailsDialog.ShowAsync(items[currentIndex], Content.XamlRoot);

    private void ShowInExplorer_Click(object sender, RoutedEventArgs e)
    {
        var path = items[currentIndex].Path;
        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
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
