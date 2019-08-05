using InstagramApiSharp.Classes.Models;
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
    public sealed partial class UnfollowersPage : Page
    {
        public UnfollowersPage()
        {
            this.InitializeComponent();
        }
        public User InstaUser { get; set; }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            InstaUser = e.Parameter as User;
        }

        private void ListFollowers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var user = e.AddedItems[0] as InstaUserShort;
            if (user != null)
                this.Frame.Navigate(typeof(PersonPage), new object[] { user, InstaUser });
        }

        private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            listFollowers.ItemsSource = InstaServer.SearchByUserName(InstaUser.UserData.UserUnfollowers, ((AutoSuggestBox)sender).Text);
        }

        private async void UnFollowButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog contentDialog = new ContentDialog()
            {
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                Content = "Are you sure to unfollow from all unfollowers?",
                Title = "Approve"
            };
            var b = await contentDialog.ShowAsync();
            if (b == ContentDialogResult.Primary)
                await InstaServer.UnFollowFromList(InstaUser, InstaUser.UserData.UserUnfollowers);
        }
    }
}
