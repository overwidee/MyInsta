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
using WinRTXamlToolkit.Tools;

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

            InstaServer.OnUserFeedLoaded += UpdateList;
        }

        #region CompleteEvent
        private void UpdateList(int pk)
        {
            foreach (var postItem in InstaUser.UserData.Feed)
            {
                var m = Feed.FirstOrDefault(x => x.Id == postItem.Id);
                if (m == null)
                {
                    Feed.Add(postItem);
                }
            }

            ProgressStack.Visibility = Visibility.Collapsed;
            LoadBlock.Visibility = Visibility.Collapsed;
        }
        #endregion

        private int count = 6;
        public ObservableCollection<PostItem> Feed { get; set; } = new ObservableCollection<PostItem>();
        public User InstaUser { get; set; }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            InstaUser = e.Parameter as User;
            if (InstaUser != null)
            {
                InstaUser.UserData.Feed.Clear();

                if (!InstaServer.IsFeedLoading)
                {
                    InstaServer.FeedMaxLoadedId = "";
                    await InstaServer.GetCustomFeed(InstaUser, true);
                }
            }
        }
        private async void ScrollListPosts_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var svPosts = sender as ScrollViewer;

            double verticalOffset = svPosts.VerticalOffset;
            double maxVerticalOffset = svPosts.ScrollableHeight;


            if (verticalOffset >= maxVerticalOffset - maxVerticalOffset / 3 && InstaUser.UserData.Feed.Count < 250)
            {
                await InstaServer.GetCustomFeed(InstaUser);
            }
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

                var mediaDialog = new MediaDialog(InstaUser, sav, urlMedia, sav.MediaType, 1);
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
                        , ((IEnumerable<PostItem>)PostsList.ItemsSource).FirstOrDefault(x => x.Id == int.Parse(((MenuFlyoutItem)sender).Tag.ToString())).UserPk)
                    , ((IEnumerable<PostItem>)PostsList.ItemsSource).FirstOrDefault(x => x.Id == int.Parse(((MenuFlyoutItem)sender).Tag.ToString()))?.Items);
            }
        }
        private async void ButtonShare_OnClick(object sender, RoutedEventArgs e)
        {
            await InstaServer.ShareMedia(InstaUser,
                Feed?.FirstOrDefault(x => x.Id == int.Parse(((MenuFlyoutItem)sender).Tag.ToString()))?.Items);
        }

        private async void LoadButton_OnClick(object sender, RoutedEventArgs e)
        {
            ProgressStack.Visibility = Visibility.Visible;

            Feed.Clear();
            await InstaServer.GetCustomFeed(InstaUser);
        }

        private void FeedPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            InstaServer.OnUserFeedLoaded -= UpdateList;
        }

        private async void MenuFlyoutItem_OnClick(object sender, RoutedEventArgs e)
        {
            await InstaServer.ShowLikers(InstaUser, ((MenuFlyoutItem)sender).Tag.ToString(), Frame);
        }

        private void MenuFlyoutItem1_OnClick(object sender, RoutedEventArgs e)
        {
            InstaServer.ShowComments(InstaUser, this, ((MenuFlyoutItem)sender).Tag.ToString());
        }
         
        private async void MenuFlyoutItem2_OnClick(object sender, RoutedEventArgs e)
        {
            await InstaServer.SaveMediaInProfile(InstaUser, ((MenuFlyoutItem)sender).Tag.ToString());
        }
    }
}
