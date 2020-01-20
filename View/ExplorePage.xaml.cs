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
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExplorePage : Page
    {
        public ExplorePage()
        {
            InitializeComponent();

            InstaServer.OnUserExploreFeedLoaded += () => ProgressExplore.IsActive = false;
        }

        private User InstaUser;
        public ObservableCollection<PostItem> ExploreFeed { get; set; }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            InstaUser = e.Parameter as User;

            ExploreFeed = await InstaServer.GetExploreFeed(InstaUser);
            postsList.ItemsSource = ExploreFeed;
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
                var urlMedia = "";
                switch (sav.MediaType)
                {
                    case MediaType.Image:
                        urlMedia = sav.UrlBigImage;
                        break;
                    case MediaType.Video:
                        urlMedia = sav.UrlVideo;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

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
                    InstaUser.UserData.SavedPostItems.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString()))?.Items[0]);
                InstaUser.UserData.SavedPostItems.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString())).Items[0].Liked = true;
            }
            else
            {
                bool like = await InstaServer.UnlikeMedia(InstaUser,
                    InstaUser.UserData.SavedPostItems.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString()))?.Items[0]);
                InstaUser.UserData.SavedPostItems.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString())).Items[0].Liked = false;
            }
        }

        private async void ButtonDownload_OnClick(object sender, RoutedEventArgs e)
        {
            if (postsList.ItemsSource != null)
            {
                await InstaServer.DownloadAnyPost(
                    await InstaServer.GetInstaUserShortById(InstaUser
                        , ((IEnumerable<PostItem>)postsList.ItemsSource).FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString())).UserPk)
                    , ((IEnumerable<PostItem>)postsList.ItemsSource).FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString()))?.Items);
            }
        }

        private async void ButtonShare_OnClick(object sender, RoutedEventArgs e)
        {
            await InstaServer.ShareMedia(InstaUser,
                ExploreFeed?.FirstOrDefault(x => x.Id == int.Parse(((Button)sender).Tag.ToString()))?.Items);
        }

        private async void AutoSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (string.IsNullOrEmpty(sender.Text))
            {
                return;
            }
            ExploreFeed = new ObservableCollection<PostItem>();
            ProgressExplore.IsActive = true;

            ExploreFeed = await InstaServer.GetFeedByTag(InstaUser, sender.Text);
            postsList.ItemsSource = ExploreFeed;
        }

        private async void ButtonSaveInProfile_OnClick(object sender, RoutedEventArgs e)
        {
            await InstaServer.SaveMediaInProfile(InstaUser, ((Button)sender).Tag.ToString());
        }
    }
}
