using InstagramApiSharp.Classes.Models;
using MyInsta.Logic;
using MyInsta.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
    public sealed partial class PersonPage : Page
    {
        public PersonPage()
        {
            this.InitializeComponent();
        }

        public InstaUserShort SelectUser { get; set; }
        public InstaUserInfo InstaUserInfo { get; set; }
        public User CurrentUser { get; set; }
        public bool ButtonFollow { get; set; }
        public bool ButtonUnFollow { get; set; }

        public InstaMediaList MediaUser { get; set; }

        public ObservableCollection<CustomMedia> UrlMedias { get; set; }
        public ObservableCollection<CustomMedia> UrlStories { get; set; }

        int countPosts = 10;

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var objs = e.Parameter as object[];
            SelectUser = objs[0] as InstaUserShort;
            CurrentUser = objs[1] as User;

            InstaUserInfo = AsyncHelpers.RunSync(() =>
                InstaServer.GetInfoUser(CurrentUser, SelectUser.UserName));
            InstaUserInfo.FriendshipStatus = AsyncHelpers.RunSync(() =>
                InstaServer.GetFriendshipStatus(CurrentUser, InstaUserInfo));
            ButtonFollow = !InstaUserInfo.FriendshipStatus.Following;
            ButtonUnFollow = InstaUserInfo.FriendshipStatus.Following;

            UrlStories = await InstaServer.GetStoryUser(CurrentUser, InstaUserInfo);
            storiesList.ItemsSource = UrlStories;

            UrlMedias = await InstaServer.GetMediaUser(CurrentUser, InstaUserInfo, 0);
            mediaList.ItemsSource = UrlMedias.Take(countPosts);

            UrlMedias = await InstaServer.GetMediaUser(CurrentUser, InstaUserInfo, 1);
            mediaList.ItemsSource = UrlMedias?.Take(countPosts);
        }

        private async void UnfollowButton_Click(object sender, RoutedEventArgs e)
        {
            unfollowButton.IsEnabled = false;
            await InstaServer.UnfollowUser(CurrentUser, SelectUser);
            followButton.IsEnabled = true;
        }

        private async void FollowButton_Click(object sender, RoutedEventArgs e)
        {
            followButton.IsEnabled = false;
            await InstaServer.FollowUser(CurrentUser, InstaUserInfo);
            unfollowButton.IsEnabled = true;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.SaveImages(UrlMedias, SelectUser);
        }

        private async void UnlikeButton_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.UnlikeProfile(CurrentUser, SelectUser, UrlMedias);
        }

        private async void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.DownloadMedia(UrlMedias.Where(x => x.Name == ((Button)sender).Tag.ToString()).First());
        }

        private async void ButtonDownloadStory_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.DownloadMedia(UrlStories.Where(x => x.Name == ((Button)sender).Tag.ToString()).First());
        }

        private void ScrollListPosts_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            ScrollViewer svPosts = sender as ScrollViewer;

            var verticalOffset = svPosts.VerticalOffset;
            var maxVerticalOffset = svPosts.ScrollableHeight;

            if (verticalOffset == maxVerticalOffset)
            {
                countPosts += 3;
                mediaList.ItemsSource = UrlMedias.Take(countPosts);
            }
        }

        private void StoriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var story = e.AddedItems[0] as CustomMedia;
                if (story != null)
                {
                    string urlMedia = "";
                    if (story.MediaType == MediaType.Image)
                        urlMedia = story.UrlBigImage;
                    else if (story.MediaType == MediaType.Video)
                        urlMedia = story.UrlVideo;

                    MediaDialog mediaDialog = new MediaDialog(CurrentUser, story.Pk, urlMedia, story.MediaType, 0);
                    mediaDialog.ShowMediaAsync();
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

        private void MediaList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var post = e.AddedItems[0] as CustomMedia;
                if (post != null)
                {
                    string urlMedia = "";
                    if (post.MediaType == MediaType.Image)
                        urlMedia = post.UrlBigImage;
                    else if (post.MediaType == MediaType.Video)
                        urlMedia = post.UrlVideo;

                    MediaDialog mediaDialog = new MediaDialog(CurrentUser, post.Pk, urlMedia, post.MediaType, 1);
                    mediaDialog.ShowMediaAsync();
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

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            InstaServer.CancelTasks();
        }
    }
}
