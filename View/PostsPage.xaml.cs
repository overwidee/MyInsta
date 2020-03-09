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

namespace MyInsta.View
{
    public sealed partial class PostsPage : Page
    {
        public PostsPage()
        {
            InitializeComponent();

            progressPosts.IsActive = !InstaServer.IsSavedPostsLoaded;

            InstaServer.OnUserSavedPostsLoaded += delegate
            {
                progressPosts.IsActive = false;
                foreach (var post in InstUser.UserData.SavedPostItems)
                {
                    var existPost = SavedPosts.FirstOrDefault(x => x.Id == post.Id);

                    if (existPost == null)
                    {
                        SavedPosts.Add(post);
                    }
                }
            };
            InstaServer.OnUserCollectionLoaded += () => progressCollection.Visibility = Visibility.Collapsed;
        }

        public User InstUser { get; set; }
        public ObservableCollection<PostItem> SavedPosts { get; set; } = new ObservableCollection<PostItem>();
        public InstaCollections InstaCollections { get; set; }
        int countPosts = 12;
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is object[] obj)
            {
                InstUser = obj[0] as User;
            }

            if (InstUser != null)
            {
                InstaCollections = await InstaServer.GetListCollections(InstUser);
                InstaCollections.Items.Add(new InstaCollectionItem() { CollectionId = 0, CollectionName = "All posts", CoverMedia = new InstaCoverMedia()
                {
                    ImageVersions = new List<InstaImage>(){ (new InstaImage(InstUser.UserData.UrlPicture, 100, 100))}
                }});
                collectionsBox.ItemsSource = InstaCollections.Items;
                collectionsBox.SelectedIndex = InstaCollections.Items.Count - 1;
            }
        }

        private async void ScrollListPosts_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var svPosts = sender as ScrollViewer;

            double verticalOffset = svPosts.VerticalOffset;
            double maxVerticalOffset = svPosts.ScrollableHeight;

            if (verticalOffset >= maxVerticalOffset - maxVerticalOffset / 3)
            {
                var selectedCollectionId = collectionsBox.SelectedItem as InstaCollectionItem;
                
                if (selectedCollectionId.CollectionId == 0)
                {
                    await InstaServer.GetUserPostItems(InstUser, selectedCollectionId.CollectionId);
                }
            }
        }

        private void PostsList_SelectionChanged(object sender, TappedRoutedEventArgs e)
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

                var mediaDialog = new MediaDialog(InstUser, sav.Pk, urlMedia, sav.MediaType, 1);
                _ = mediaDialog.ShowMediaAsync();
            }
        }

        private async void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            if (postsList.ItemsSource != null)
            {
                await InstaServer.DownloadAnyPost(
                    await InstaServer.GetInstaUserShortById(InstUser
                        , ((IEnumerable<PostItem>)postsList.ItemsSource).FirstOrDefault(x => x.Id == int.Parse(((MenuFlyoutItem)sender).Tag.ToString())).UserPk)
                        , ((IEnumerable<PostItem>)postsList.ItemsSource).FirstOrDefault(x => x.Id == int.Parse(((MenuFlyoutItem)sender).Tag.ToString()))?.Items);
            }
        }

        private async void ButtonProfile_Click(object sender, RoutedEventArgs e)
        {
            InstaUserShort user = await InstaServer.GetInstaUserShortById(InstUser, long.Parse(((Button)sender).Tag.ToString()));
            Frame.Navigate(typeof(PersonPage), new object[] { user, InstUser });
        }

        private async void ButtonShare_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.ShareMedia(InstUser,
                SavedPosts?.FirstOrDefault(x => x.Id == int.Parse(((MenuFlyoutItem)sender).Tag.ToString()))?.Items);
        }

        private async void CollectionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ListView)sender).SelectedItem is InstaCollectionItem collection)
            {
                InstUser.UserData.SavedPostItems.Clear();
                InstaServer.UserSavedMediaMaxId = "";

                SavedPosts.Clear();
                InstUser.UserData.SavedPostItems.Clear();
                progressPosts.IsActive = true;

                await InstaServer.GetUserPostItems(InstUser, collection.CollectionId, true);

                scrollListPosts.ChangeView(null, 0, 1, true);
            }
        }

        private async void buttonLike_Click(object sender, RoutedEventArgs e)
        {
            var isChecked = ((CheckBox)sender).IsChecked;
            if (isChecked != null && isChecked.Value)
            {
                bool like = await InstaServer.LikeMedia(InstUser,
                    InstUser.UserData.SavedPostItems.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString()))?.Items[0]);
                InstUser.UserData.SavedPostItems.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString())).Items[0].Liked = true;
            }
            else
            {
                bool like = await InstaServer.UnlikeMedia(InstUser,
                    InstUser.UserData.SavedPostItems.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString()))?.Items[0]);
                InstUser.UserData.SavedPostItems.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString())).Items[0].Liked = false;
            }
        }

        private async void LikesItem_OnClick(object sender, RoutedEventArgs e)
        {
            await InstaServer.ShowLikers(InstUser, ((MenuFlyoutItem)sender).Tag.ToString(), Frame);
        }

        private void CommentsItem_OnClick(object sender, RoutedEventArgs e)
        {
            InstaServer.ShowComments(InstUser, this, ((MenuFlyoutItem)sender).Tag.ToString());
        }
    }
}
