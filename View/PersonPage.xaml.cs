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

            progressPosts.IsActive = !InstaServer.IsPostsLoaded;
            InstaServer.OnUserPostsLoaded += () => { progressPosts.IsActive = false; };
        }

        public InstaUserShort SelectUser { get; set; }
        public InstaUserInfo InstaUserInfo { get; set; }
        public User CurrentUser { get; set; }
        public bool ButtonFollow { get; set; }
        public bool ButtonUnFollow { get; set; }

        public InstaMediaList MediaUser { get; set; }

        public ObservableCollection<PostItem> Posts { get; set; }
        public ObservableCollection<CustomMedia> UrlStories { get; set; }
        public ObservableCollection<CustomMedia> HighlightsStories { get; set; }
        public InstaHighlightFeeds InstaHighlightFeeds { get; set; }

        int countPosts = 10;

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var objs = e.Parameter as object[];
            SelectUser = objs[0] as InstaUserShort;
            CurrentUser = objs[1] as User;

            InstaUserInfo = AsyncHelpers.RunSync(() => InstaServer.GetInfoUser(CurrentUser, SelectUser.UserName));
            InstaUserInfo.FriendshipStatus = AsyncHelpers.RunSync(()
                => InstaServer.GetFriendshipStatus(CurrentUser, InstaUserInfo));

            ButtonFollow = !InstaUserInfo.FriendshipStatus.Following;
            ButtonUnFollow = InstaUserInfo.FriendshipStatus.Following;
            SetBookmarkStatus();

            if (InstaUserInfo.IsPrivate && !InstaUserInfo.FriendshipStatus.Following)
                return;

            InstaHighlightFeeds = await InstaServer.GetArchiveCollectionStories(CurrentUser, SelectUser.Pk);
            collectionsBox.ItemsSource = InstaHighlightFeeds.Items;

            UrlStories = await InstaServer.GetStoryUser(CurrentUser, InstaUserInfo);
            storiesList.ItemsSource = UrlStories;

            if (InstaHighlightFeeds.Items.Count > 0)
                highTab.Visibility = Visibility.Visible;
            if (UrlStories.Count > 0)
                storyTab.Visibility = Visibility.Visible;

            Posts = await InstaServer.GetMediaUser(CurrentUser, InstaUserInfo, 0);
            mediaList.ItemsSource = Posts?.Take(countPosts);

            Posts = await InstaServer.GetMediaUser(CurrentUser, InstaUserInfo, 1);
            mediaList.ItemsSource = Posts?.Take(countPosts);
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
            => await InstaServer.DownloadAnyPosts(SelectUser, Posts);

        private async void UnlikeButton_Click(object sender, RoutedEventArgs e)
            => await InstaServer.UnlikeProfile(CurrentUser, SelectUser, Posts);

        private async void ButtonDownload_Click(object sender, RoutedEventArgs e)
            => await InstaServer.DownloadAnyPost(await InstaServer.GetInstaUserShortById(CurrentUser,
                Posts.Where(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).First().UserPk),
            Posts.Where(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).First().Items);

        private async void ButtonDownloadStory_Click(object sender, RoutedEventArgs e)
            => await InstaServer.DownloadMedia(UrlStories.Where(x => x.Name == ((Button)sender).Tag.ToString())
                                                   .First());

        private void ScrollListPosts_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var svPosts = sender as ScrollViewer;

            var verticalOffset = svPosts.VerticalOffset;
            var maxVerticalOffset = svPosts.ScrollableHeight;

            if (verticalOffset == maxVerticalOffset)
            {
                countPosts += 3;
                mediaList.ItemsSource = Posts?.Take(countPosts);
            }
        }

        private void MediaList_SelectionChanged(object sender, TappedRoutedEventArgs e)
        {
            var post = ((FlipView)sender).SelectedItem as CustomMedia;
            if (post != null)
            {
                string urlMedia = "";
                if (post.MediaType == MediaType.Image)
                    urlMedia = post.UrlBigImage;
                else if (post.MediaType == MediaType.Video)
                    urlMedia = post.UrlVideo;

                var mediaDialog = new MediaDialog(CurrentUser, post.Pk, urlMedia, post.MediaType, 1);
                _ = mediaDialog.ShowMediaAsync();
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e) => InstaServer.CancelTasks();

        private async void ButtonSaveInProfile_Click(object sender, RoutedEventArgs e)
            => await InstaServer.SaveMediaInProfile(CurrentUser, ((Button)sender).Tag.ToString());

        private async void ButtonShare_Click(object sender, RoutedEventArgs e)
            => await InstaServer.ShareMedia(CurrentUser,
            Posts.Where(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).First().Items);

        private void PostBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (!string.IsNullOrEmpty(sender.Text))
            {
                int i;
                var b = int.TryParse(sender.Text, out i);
                if (b)
                {
                    var items = Posts?.Where(x => x.Id == i);
                    if (items != null)
                        mediaList.ItemsSource = items;
                }
                else
                    mediaList.ItemsSource = Posts;
            }
            else
                mediaList.ItemsSource = Posts;
        }

        private void PostBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!string.IsNullOrEmpty(sender.Text))
            {
                int i;
                var b = int.TryParse(sender.Text, out i);
                if (b)
                {
                    var items = Posts?.Where(x => x.Id == i);
                    if (items != null)
                        mediaList.ItemsSource = items;
                }
                else
                    mediaList.ItemsSource = Posts;
            }
            else
                mediaList.ItemsSource = Posts;
        }

        private void SetBookmarkStatus()
        {
            var button = buttonAction;
            var menu = button.Flyout as MenuFlyout;
            var item = new MenuFlyoutItem()
            {
                CornerRadius = new CornerRadius(5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Text
                = !InstaServer.IsContrainsAccount(CurrentUser, SelectUser.Pk)
                ? "Add in bookmarks"
                : "Remove from bookmarks",
            };

            item.Click += async(s, e) =>
            {
                if (!InstaServer.IsContrainsAccount(CurrentUser, SelectUser.Pk))
                {
                    CurrentUser.UserData.Bookmarks.Add(SelectUser);
                    await InstaServer.SaveBookmarksAsync(CurrentUser);
                }
                else
                {
                    CurrentUser.UserData.Bookmarks.Remove(SelectUser);
                    await InstaServer.SaveBookmarksAsync(CurrentUser);
                }
            };
            menu.Items.Add(item);
        }

        private async void CollectionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectHigh = ((ListView)(sender)).SelectedItem as InstaHighlightFeed;
            HighlightsStories = await InstaServer.GetHighlightStories(CurrentUser, selectHigh.HighlightId);
            archiveList.ItemsSource = HighlightsStories;

            scrollArchive.ChangeView(null, 0, 1, true);
        }

        private async void ButtonDownloadHigh_Click(object sender, RoutedEventArgs e)
            => await InstaServer.DownloadMedia(HighlightsStories.Where(x => x.Name == ((Button)sender).Tag.ToString())
                                                   .First());

        private async void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var high = HighlightsStories.Where(x => x.Pk == ((Image)sender).Tag.ToString()).FirstOrDefault();
            if (high != null)
            {
                string urlMedia = "";
                if (high.MediaType == MediaType.Image)
                    urlMedia = high.UrlBigImage;
                else if (high.MediaType == MediaType.Video)
                    urlMedia = high.UrlVideo;

                var mediaDialog = new MediaDialog(CurrentUser, high.Pk, urlMedia, high.MediaType, 0);
                await mediaDialog.ShowMediaAsync();
            }
        }

        private async void Image_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            var high = UrlStories.Where(x => x.Pk == ((Image)sender).Tag.ToString()).FirstOrDefault();
            if (high != null)
            {
                string urlMedia = "";
                if (high.MediaType == MediaType.Image)
                    urlMedia = high.UrlBigImage;
                else if (high.MediaType == MediaType.Video)
                    urlMedia = high.UrlVideo;

                var mediaDialog = new MediaDialog(CurrentUser, high.Pk, urlMedia, high.MediaType, 0);
                await mediaDialog.ShowMediaAsync();
            }
        }

        private void TextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            InstaServer.ShowComments(CurrentUser, this, ((TextBlock)sender).Tag.ToString());
        }

        private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender,
            AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var result = await InstaServer.AnswerToStory(CurrentUser, sender.Text,
                sender.Tag.ToString(), SelectUser.Pk);

            if (result)
            {
                _ = new CustomDialog("Message", $"Message send to {SelectUser.Pk}", "Ok");
                sender.Text = "";
            }
            else
            {
                _ = new CustomDialog("Message", "Error", "Ok");
            }
        }

        private void ItemsList_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}
