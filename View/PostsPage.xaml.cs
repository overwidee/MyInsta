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
    public sealed partial class PostsPage : Page
    {
        public PostsPage()
        {
            this.InitializeComponent();

            progressPosts.IsActive = !InstaServer.IsSavedPostsLoaded;
            InstaServer.OnUserSavedPostsLoaded += () => progressPosts.IsActive = false;
        }

        public User InstaUser { get; set; }
        int countPosts = 10;
        int typePage;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var objs = e.Parameter as object[];
            InstaUser = objs[0] as User;
            typePage = (int)objs[1];

            postsList.ItemsSource = InstaUser.UserData.SavedPostItems?.Take(10);
            //switch (typePage)
            //{
            //    case 0: postsList.ItemsSource = InstaUser.UserData.Feed?.Take(10);
            //        break;
            //    case 1: postsList.ItemsSource = InstaUser.UserData.SavedPostItems?.Take(10);
            //        break;
            //    default:
            //        break;
            //}

        }

        private void ScrollListPosts_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            ScrollViewer svPosts = sender as ScrollViewer;

            var verticalOffset = svPosts.VerticalOffset;
            var maxVerticalOffset = svPosts.ScrollableHeight;

            if (verticalOffset == maxVerticalOffset)
            {
                countPosts += 3;
                postsList.ItemsSource = typePage == 1 ? InstaUser.UserData.SavedPostItems?.Take(countPosts) : InstaUser.UserData.Feed?.Take(countPosts);
            }
        }

        private void PostsList_SelectionChanged(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                var sav = ((FlipView)sender).SelectedItem as CustomMedia;
                if (sav != null)
                {
                    string urlMedia = "";
                    if (sav.MediaType == MediaType.Image)
                        urlMedia = sav.UrlBigImage;
                    else if (sav.MediaType == MediaType.Video)
                        urlMedia = sav.UrlVideo;

                    MediaDialog mediaDialog = new MediaDialog(InstaUser, sav.Pk, urlMedia, sav.MediaType, 1);
                    _ = mediaDialog.ShowMediaAsync();
                }
            }
            catch
            {

            }
        }

        private async void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.DownloadAnyPost(
                await InstaServer.GetInstaUserShortById(InstaUser,
                    typePage == 1 ? InstaUser.UserData.SavedPostItems.FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).UserPk 
                    : InstaUser.UserData.Feed.FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).UserPk),
                typePage == 1 ? InstaUser.UserData.SavedPostItems.FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).Items 
                    : InstaUser.UserData.Feed.FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).Items);
        }

        private async void ButtonProfile_Click(object sender, RoutedEventArgs e)
        {
            var user = await InstaServer.GetInstaUserShortById(InstaUser, long.Parse(((Button)sender).Tag.ToString()));
            this.Frame.Navigate(typeof(PersonPage), new object[] { user, InstaUser });
        }

        private async void ButtonShare_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.ShareMedia(InstaUser, 
                typePage == 1 ? InstaUser.UserData.SavedPostItems.FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).Items 
                : InstaUser.UserData.Feed.FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).Items);
        }
    }
}
