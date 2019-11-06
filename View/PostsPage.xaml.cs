using InstagramApiSharp.Classes.Models;
using MyInsta.Logic;
using MyInsta.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public sealed partial class PostsPage : Page
    {
        public PostsPage()
        {
            this.InitializeComponent();

            progressPosts.IsActive = !InstaServer.IsSavedPostsLoaded;
            InstaServer.OnUserSavedPostsLoaded += () => progressPosts.IsActive = false;
            InstaServer.OnUserCollectionLoaded += () => progressCollection.Visibility = Visibility.Collapsed;
        }

        public User InstUser { get; set; }
        public ObservableCollection<PostItem> SavedPosts { get; set; } = new ObservableCollection<PostItem>();
        public InstaCollections InstaCollections { get; set; }
        int countPosts = 10;
        int typePage;
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var objs = e.Parameter as object[];
            InstUser = objs[0] as User;
            typePage = (int)objs[1];

            SavedPosts = new ObservableCollection<PostItem>(InstUser.UserData.SavedPostItems?.Take(countPosts).Select(x => x).ToList());
            postsList.ItemsSource = SavedPosts;
            InstaCollections = await InstaServer.GetListCollections(InstUser);
            InstaCollections.Items.Add(new InstaCollectionItem() { CollectionId = 1, CollectionName = "All posts" });
            collectionsBox.ItemsSource = InstaCollections.Items;
        }

        private void ScrollListPosts_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var svPosts = sender as ScrollViewer;

            var verticalOffset = svPosts.VerticalOffset;
            var maxVerticalOffset = svPosts.ScrollableHeight;

            if (verticalOffset == maxVerticalOffset)
            {
                countPosts += 3;
                postsList.ItemsSource = new ObservableCollection<PostItem>(InstUser.UserData.SavedPostItems?.Take(countPosts).Select(x => x).ToList()); ;
            }
        }

        private void PostsList_SelectionChanged(object sender, TappedRoutedEventArgs e)
        {
            var sav = ((FlipView)sender).SelectedItem as CustomMedia;
            if (sav != null)
            {
                string urlMedia = "";
                if (sav.MediaType == MediaType.Image)
                    urlMedia = sav.UrlBigImage;
                else if (sav.MediaType == MediaType.Video)
                    urlMedia = sav.UrlVideo;

                var mediaDialog = new MediaDialog(InstUser, sav.Pk, urlMedia, sav.MediaType, 1);
                _ = mediaDialog.ShowMediaAsync();
            }
        }

        private async void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.DownloadAnyPost(
                await InstaServer.GetInstaUserShortById(InstUser,
                    typePage == 1 ? ((IEnumerable<PostItem>) postsList.ItemsSource).FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).UserPk
                    : InstUser.UserData.Feed.FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).UserPk),
                typePage == 1 ? ((IEnumerable<PostItem>) postsList.ItemsSource).FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).Items
                    : InstUser.UserData.Feed.FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).Items);
        }

        private async void ButtonProfile_Click(object sender, RoutedEventArgs e)
        {
            var user = await InstaServer.GetInstaUserShortById(InstUser, long.Parse(((Button)sender).Tag.ToString()));
            this.Frame.Navigate(typeof(PersonPage), new object[] { user, InstUser });
        }

        private async void ButtonShare_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.ShareMedia(InstUser,
                typePage == 1 ? SavedPosts.FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).Items
                : InstUser.UserData.Feed.FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).Items);
        }

        private async void CollectionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            countPosts = 10;
            var collection = ((ListView)sender).SelectedItem as InstaCollectionItem;
            if (collection.CollectionId != 1) progressCollection.Visibility = Visibility.Visible;

            postsList.ItemsSource = collection.CollectionId == 1
                ? SavedPosts?.Take(countPosts)
                : await InstaServer.GetMediasByCollection(InstUser, collection);
            scrollListPosts.ChangeView(null, 0, 1, true);
        }
    }
}
