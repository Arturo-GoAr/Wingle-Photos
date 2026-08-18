using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinglePhotos.Features.PhotoSources;
using WinglePhotos.Features.Viewer;

namespace WinglePhotos.Features.PhotoLibrary;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; } = App.Services.GetRequiredService<MainViewModel>();

    public MainPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.InitializeAsync();
    }

    private void PhotosGridView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (!args.InRecycleQueue && args.Item is PhotoItem item)
        {
            _ = ViewModel.LoadThumbnailCommand.ExecuteAsync(item);
        }
    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PhotoItem item })
        {
            _ = ViewModel.ToggleFavoriteCommand.ExecuteAsync(item);
        }
    }

    private void RemoveSourceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PhotoSource source })
        {
            _ = ViewModel.RemoveSourceCommand.ExecuteAsync(source);
        }
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
