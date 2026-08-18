using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using WinglePhotos.Features.PhotoLibrary;
using WinglePhotos.Features.PhotoSources;
using WinglePhotos.Features.Settings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinglePhotos;

/// <summary>
/// The application window. Hosts the sidebar (NavigationView) and the Frame that
/// displays feature pages. Add your UI and logic to the individual feature pages
/// instead of here so you can use Page features such as navigation events.
/// </summary>
public sealed partial class MainWindow : Window
{
    private const string HelpUrl = "https://github.com/Arturo-GoAr/Wingle-Photos/blob/main/README.md";

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        RootNavigationView.SelectedItem = AllPhotosItem;
        RootFrame.Navigate(typeof(MainPage), new MainPageNavigationArgs(FavoritesOnly: false, Folder: null));
    }

    private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is not NavigationViewItem { Tag: string tag })
        {
            return;
        }

        switch (tag)
        {
            case "all":
                RootFrame.Navigate(typeof(MainPage), new MainPageNavigationArgs(FavoritesOnly: false, Folder: null));
                break;
            case "photos":
                RootFrame.Navigate(typeof(MainPage), new MainPageNavigationArgs(FavoritesOnly: false, Folder: null, MediaKind: MediaKindFilter.Photos));
                break;
            case "videos":
                RootFrame.Navigate(typeof(MainPage), new MainPageNavigationArgs(FavoritesOnly: false, Folder: null, MediaKind: MediaKindFilter.Videos));
                break;
            case "favorites":
                RootFrame.Navigate(typeof(MainPage), new MainPageNavigationArgs(FavoritesOnly: true, Folder: null));
                break;
            case "folders":
                RootFrame.Navigate(typeof(FoldersPage));
                break;
            case "settings":
                RootFrame.Navigate(typeof(SettingsPage));
                break;
            case "help":
                _ = Launcher.LaunchUriAsync(new Uri(HelpUrl));
                break;
        }
    }
}
