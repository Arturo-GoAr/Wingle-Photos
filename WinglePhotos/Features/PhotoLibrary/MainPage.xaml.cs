using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinglePhotos.Features.Viewer;

namespace WinglePhotos.Features.PhotoLibrary;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; } = App.Services.GetRequiredService<MainViewModel>();

    public MainPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            try
            {
                await ViewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        };
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is MainPageNavigationArgs args)
        {
            ViewModel.ApplyFilter(args.FavoritesOnly, args.Folder, args.MediaKind);
        }
    }

    private void PhotosGridView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Item is not PhotoItem item)
        {
            return;
        }

        if (args.InRecycleQueue)
        {
            // Container is being recycled for reuse elsewhere — if this item's thumbnail
            // load hasn't started yet (still waiting on the semaphore) or is mid-flight,
            // cancel it so scrolling doesn't queue work behind items already scrolled past.
            // IsThumbnailLoading/ThumbnailLoadCts are cleared here synchronously (not left
            // to the load's own finally, which runs later on the dispatcher) so that if this
            // item's container is re-realized before that finally runs, LoadThumbnailAsync
            // doesn't see a stale "still loading" flag and skip starting a fresh load.
            item.ThumbnailLoadCts?.Cancel();
            item.ThumbnailLoadCts = null;
            item.IsThumbnailLoading = false;
            return;
        }

        _ = ViewModel.LoadThumbnailCommand.ExecuteAsync(item);
    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PhotoItem item })
        {
            _ = ViewModel.ToggleFavoriteCommand.ExecuteAsync(item);
        }
    }

    private void FavoriteMenuItem_Click(object sender, RoutedEventArgs e) => FavoriteButton_Click(sender, e);

    private async void AddTagMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PhotoItem item })
        {
            await AddTagDialog.ShowAsync(item, ViewModel, Content.XamlRoot);
        }
    }

    private async void ViewDetailsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PhotoItem item })
        {
            await PhotoDetailsDialog.ShowAsync(item, Content.XamlRoot);
        }
    }

    private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: PhotoItem item })
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = item.IsVideo ? "Eliminar video" : "Eliminar foto",
            Content = "Se moverá a la papelera de reciclaje. ¿Continuar?",
            PrimaryButtonText = "Eliminar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.DeletePhotoCommand.ExecuteAsync(item);
    }

    private void PhotosGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not PhotoItem clicked)
        {
            return;
        }

        var orderedItems = ViewModel.Groups.SelectMany(g => g).ToList();
        var startIndex = orderedItems.IndexOf(clicked);

        Frame.Navigate(typeof(PhotoViewerPage), new PhotoViewerNavigationParameter(ViewModel, orderedItems, startIndex));
    }
}
