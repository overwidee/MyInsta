using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using InstagramApiSharp.Classes.Models;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarSymbols;
using MyInsta.Logic;
using MyInsta.Model;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MyInsta.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FeedPage : Page
    {
        public FeedPage()
        {
            InitializeComponent();

            InstaServer.OnUsersFeedLoaded += () => MainProgressRing.IsActive = false;
            InstaServer.OnFeedLoaded += () => ProgressStack.Visibility = Visibility.Collapsed;
            PostsList.ItemsSource = Feed;
        }

        public ObservableCollection<PostItem> Feed { get; set; } = new ObservableCollection<PostItem>();
        public ObservableCollection<InstaUserShort> ListInstaUserShorts { get; set; }
        public User InstaUser { get; set; }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            InstaUser = e.Parameter as User;
            if (InstaUser != null)
            {
                ListInstaUserShorts =
                    await InstaServer.GetUserInstaShortsByNames(InstaUser, InstaUser.UserData.FeedUsers);
                ListFollowers.ItemsSource = ListInstaUserShorts;
                LoadButton.IsEnabled = true;

                if (InstaUser.UserData.Feed.Count != 0)
                {
                    Feed = InstaUser.UserData.Feed;
                    PostsList.ItemsSource = Feed;

                    ProgressStack.Visibility = Visibility.Collapsed;
                }
                else
                {
                    Feed = await InstaServer.GetCustomFeed(InstaUser, ListInstaUserShorts, 3);
                    PostsList.ItemsSource = Feed;
                    InstaUser.UserData.Feed = Feed;
                }
            }
        }
        private void ScrollListPosts_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
        }

        private async void ButtonProfile_OnClick(object sender, RoutedEventArgs e)
        {
            InstaUserShort user = await InstaServer.GetInstaUserShortById(InstaUser, long.Parse(((Button)sender).Tag.ToString()));
            Frame.Navigate(typeof(PersonPage), new object[] { user, InstaUser });
        }

        private void ItemsList_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (((FlipView)sender).SelectedItem is CustomMedia sav)
            {
                string urlMedia = sav.MediaType switch
                {
                    MediaType.Image => sav.UrlBigImage,
                    MediaType.Video => sav.UrlVideo,
                    _ => throw new ArgumentOutOfRangeException()
                };

                var mediaDialog = new MediaDialog(InstaUser, sav.Pk, urlMedia, sav.MediaType, 1);
                _ = mediaDialog.ShowMediaAsync();
            }
        }

        private async void ButtonLike_OnClick(object sender, RoutedEventArgs e)
        {
            var isChecked = ((CheckBox)sender).IsChecked;
            if (isChecked != null && isChecked.Value)
            {
                bool like = await InstaServer.LikeMedia(InstaUser,
                    Feed.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString()))?.Items[0]);
                Feed.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString())).Items[0].Liked = true;
            }
            else
            {
                bool like = await InstaServer.UnlikeMedia(InstaUser,
                    Feed.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString()))?.Items[0]);
                Feed.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString())).Items[0].Liked = false;
            }
        }

        private async void ButtonDownload_OnClick(object sender, RoutedEventArgs e)
        {
            if (PostsList.ItemsSource != null)
            {
                await InstaServer.DownloadAnyPost(
                    await InstaServer.GetInstaUserShortById(InstaUser
                        , ((IEnumerable<PostItem>)PostsList.ItemsSource).FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).UserPk)
                    , ((IEnumerable<PostItem>)PostsList.ItemsSource).FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString()))?.Items);
            }
        }

        private async void ButtonShare_OnClick(object sender, RoutedEventArgs e)
        {
            await InstaServer.ShareMedia(InstaUser,
                Feed?.FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString()))?.Items);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            SplitView.IsPaneOpen = !SplitView.IsPaneOpen;
        }

        private async void LoadButton_OnClick(object sender, RoutedEventArgs e)
        {
            ProgressStack.Visibility = Visibility.Visible;

            PostsList.ItemsSource = null;
            Feed = await InstaServer.GetCustomFeed(InstaUser, ListInstaUserShorts, 3);

            PostsList.ItemsSource = Feed;
            InstaUser.UserData.Feed = Feed;
        }

        private async void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            ProgressStack.Visibility = Visibility.Visible;

            var res = await InstaServer.AddFeedUsers(InstaUser);
            foreach (var user in res)
            {
                ListInstaUserShorts.Add(user);
            }
            ListFollowers.ItemsSource = ListInstaUserShorts;
        }

        private async void DeleteButton_OnClick(object sender, RoutedEventArgs e)
        {
            await InstaServer.RemoveFeedUser(InstaUser, ((MenuFlyoutItem)sender).Tag.ToString());
            var remove = ListInstaUserShorts.FirstOrDefault(x => x.UserName == ((MenuFlyoutItem)sender).Tag.ToString());
            ListInstaUserShorts.Remove(remove);
        }
    }
}
