using MyInsta.Logic;
using MyInsta.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace MyInsta.View
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class SavedPostsPage : Page
    {
        public SavedPostsPage()
        {
            this.InitializeComponent();
        }

        public User InstaUser { get; set; }
        int countPosts = 10;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        { 
            base.OnNavigatedTo(e);

            InstaUser = e.Parameter as User;
            savedList.ItemsSource = InstaUser.UserData.SavedItems.Take(10);
        }

        private void ScrollListPosts_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            ScrollViewer svPosts = sender as ScrollViewer;

            var verticalOffset = svPosts.VerticalOffset;
            var maxVerticalOffset = svPosts.ScrollableHeight;

            if (verticalOffset == maxVerticalOffset)
            {
                countPosts += 3;
                savedList.ItemsSource = InstaUser.UserData.SavedItems.Take(countPosts);
            }
        }

        private void SavedList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var sav = e.AddedItems[0] as SavedItem;
                if (sav.Item != null)
                {
                    string urlMedia = "";
                    if (sav.Item.MediaType == MediaType.Image)
                        urlMedia = sav.Item.UrlBigImage;
                    else if (sav.Item.MediaType == MediaType.Video)
                        urlMedia = sav.Item.UrlVideo;

                    MediaDialog mediaDialog = new MediaDialog(urlMedia, sav.Item.MediaType, 1);
                    mediaDialog.ShowMedia();
                }
            }
            catch
            {

            }
            finally
            {
                ((ListView)sender).SelectedItem = null;
            }
        }

        private async void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.DownloadMedia(InstaUser.UserData.SavedItems.Where(x => x.Item.Name == ((Button)sender).Tag.ToString()).First().Item);
        }
    }
}
