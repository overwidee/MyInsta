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

            UrlMedias = await InstaServer.GetMediaUser(CurrentUser, InstaUserInfo);
            mediaList.ItemsSource = UrlMedias;
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
            await InstaServer.UnlikeProfile(CurrentUser, SelectUser);
        }

        private async void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.DownloadPost(UrlMedias.Where(x => x.Name == ((Button)sender).Tag.ToString()).First());
        }
    }
}
