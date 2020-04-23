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
using InstagramApiSharp.Classes.Models;
using MyInsta.Logic;
using MyInsta.Model;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MyInsta.View
{
    public sealed partial class ArchivePage : Page
    {
        public ArchivePage() 
        {
            InitializeComponent();

            InstaServer.OnUserArchivePostsLoaded += UpdatePosts;
            InstaServer.OnUserArchiveStoriesLoaded += UpdateStories;
            InstaServer.OnUserArchiveStoriesListLoaded += UpdateStoriesList;
        }
        public User InstaUser { get; set; }
        public ObservableCollection<PostItem> Posts { get; set; } = new ObservableCollection<PostItem>();
        public ObservableCollection<CustomMedia> Stories { get; set; } = new ObservableCollection<CustomMedia>();
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            InstaUser = e.Parameter as User;

            InstaServer.ArchivePostMaxId = "";

            InstaUser.UserData.ArchivePosts.Clear();
            InstaUser.UserData.ArchiveStories.Clear();

            await InstaServer.GetArchivePosts(InstaUser);
            await InstaServer.GetArchiveStoriesList(InstaUser, true);
        }

        private void UpdatePosts()
        {
            foreach (var post in InstaUser.UserData.ArchivePosts)
            {
                var m = Posts.FirstOrDefault(x => x.Id == post.Id || x.Items[0].Pk == post.Items[0].Pk);
                if (m == null)
                {
                    Posts.Add(post);
                }

                PostsTab.Header = $"Posts ({Posts.Count})";
                ProgressPosts.IsActive = false;
            }
        }

        private async void UpdateStoriesList()
        {
            await InstaServer.GetArchiveStories(InstaUser, InstaUser.UserData.ArchiveHigh);
        }

        private void UpdateStories()
        {
            foreach (var post in InstaUser.UserData.ArchiveStories)
            {
                var m = Stories.FirstOrDefault(x => x.Pk == post.Pk);
                if (m == null)
                {
                    Stories.Add(post);
                }

                StoriesTab.Header = $"Stories ({Stories.Count})";
                //ProgressPosts.IsActive = false;
            }
        }

        private async void ScrollListPosts_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var svPosts = sender as ScrollViewer;

            double verticalOffset = svPosts.VerticalOffset;
            double maxVerticalOffset = svPosts.ScrollableHeight;


            if (verticalOffset >= maxVerticalOffset - maxVerticalOffset / 3 && InstaServer.ArchivePostMaxId != null)
            {
                await InstaServer.GetArchivePosts(InstaUser);
            }
        }

        private void ItemsList_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.OriginalSource.GetType() != typeof(Border))
            {
                return;
            }

            if (((FlipView)sender).SelectedItem is CustomMedia post)
            {
                var urlMedia = "";
                switch (post.MediaType)
                {
                    case MediaType.Image:
                        urlMedia = post.UrlBigImage;
                        break;
                    case MediaType.Video:
                        urlMedia = post.UrlVideo;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var mediaDialog = new MediaDialog(InstaUser, post, urlMedia, post.MediaType, 1, Helper.ConvertToCustomMedia(Posts));
                _ = mediaDialog.ShowMediaAsync();
            }
        }

        private async void ButtonDownload_OnClick(object sender, RoutedEventArgs e)
        {
            await InstaServer.DownloadAnyPost(await InstaServer.GetInstaUserShortById(InstaUser,
                    Posts.First(x => x.Id == int.Parse(((MenuFlyoutItem)sender).Tag.ToString())).UserPk),
                Posts.First(x => x.Id == int.Parse(((MenuFlyoutItem)sender).Tag.ToString())).Items);
        }

        private void ButtonComments_OnClick(object sender, RoutedEventArgs e)
        {
            InstaServer.ShowComments(InstaUser, this, ((MenuFlyoutItem)sender).Tag.ToString());
        }

        private async void ButtonLikers_OnClick(object sender, RoutedEventArgs e)
        {
            await InstaServer.ShowLikers(InstaUser, ((MenuFlyoutItem)sender).Tag.ToString(), Frame);
        }

        private void ArchivePage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            InstaServer.OnUserArchivePostsLoaded -= UpdatePosts;
        }

        private async void ButtonDownloadStory_OnClick(object sender, RoutedEventArgs e)
        {
            await InstaServer.DownloadMedia(Stories.First(x => x.Name == ((Button)sender).Tag.ToString()));
        }

        private async void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var story = Stories.FirstOrDefault(x => x.Pk == ((Border)sender).Tag.ToString());
            if (story == null)
            {
                return;
            }

            var urlMedia = "";
            switch (story.MediaType)
            {
                case MediaType.Image:
                    urlMedia = story.UrlBigImage;
                    break;
                case MediaType.Video:
                    urlMedia = story.UrlVideo;
                    break;
            }

            var mediaDialog = new MediaDialog(InstaUser, story, urlMedia, story.MediaType, 0, Stories);
            await mediaDialog.ShowMediaAsync();
        }

        private async void MainScroll_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var svPosts = sender as ScrollViewer;

            double verticalOffset = svPosts.VerticalOffset;
            double maxVerticalOffset = svPosts.ScrollableHeight;


            if (verticalOffset >= maxVerticalOffset - maxVerticalOffset / 3)
            {
                await InstaServer.GetArchiveStories(InstaUser, InstaUser.UserData.ArchiveHigh);
            }
        }
    }
}
