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
            InitializeComponent();

            progressUnfollowers.IsActive = !InstaServer.IsUnfollowersLoaded;
            InstaServer.OnUserUnfollowersLoaded += () =>
            {
                progressUnfollowers.IsActive = false;
                foreach (var user in InstaUser.UserData.UserUnfollowers.Take(40))
                {
                    TempUsers.Add(user);
                }

                listFollowers.ItemsSource = TempUsers;
            };
        }
        public User InstaUser { get; set; }
        public ObservableCollection<InstaUserShort> TempUsers { get; set; } =
            new ObservableCollection<InstaUserShort>();
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            InstaUser = e.Parameter as User;

            foreach (var user in InstaUser.UserData.UserUnfollowers.Take(count))
            {
                TempUsers.Add(user);
            }
        }

        private void ListFollowers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.AddedItems[0] is InstaUserShort user)
                {
                    Frame.Navigate(typeof(PersonPage), new object[] { user, InstaUser });
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }

        private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(((AutoSuggestBox)sender).Text))
            {
                var tempFiltered = !string.IsNullOrEmpty(((AutoSuggestBox)sender).Text)
                    ? InstaUser.UserData.UserUnfollowers.Where(contact =>
                        contact.UserName.Contains(((AutoSuggestBox)sender).Text,
                            StringComparison.InvariantCultureIgnoreCase)).ToList()
                    : InstaUser.UserData.UserUnfollowers.ToList();

                for (int i = TempUsers.Count - 1; i >= 0; i--)
                {
                    var item = TempUsers[i];
                    if (!tempFiltered.Contains(item))
                    {
                        TempUsers.Remove(item);
                    }
                }

                foreach (var item in tempFiltered)
                {
                    if (!TempUsers.Contains(item))
                    {
                        TempUsers.Add(item);
                    }
                }
            }
        }
        private int count = 40;
        private async void UnFollowButton_Click(object sender, RoutedEventArgs e)
        {
            var contentDialog = new ContentDialog()
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

        private void ScrollViewer_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var svPosts = sender as ScrollViewer;

            double verticalOffset = svPosts.VerticalOffset;
            double maxVerticalOffset = svPosts.ScrollableHeight;


            if (verticalOffset == maxVerticalOffset)
            {
                if (count >= InstaUser.UserData.UserUnfollowers.Count)
                {
                    return;
                }

                count += 40;
                foreach (var item in InstaUser.UserData.UserUnfollowers?.Take(count))
                {
                    if (!TempUsers.Contains(item))
                    {
                        TempUsers.Add(item);
                    }
                }
            }
        }

        private void SearchBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var tempFiltered = !string.IsNullOrEmpty(sender.Text)
                ? InstaUser.UserData.UserFriends.Where(contact =>
                    contact.UserName.Contains(sender.Text,
                        StringComparison.InvariantCultureIgnoreCase)).ToList()
                : InstaUser.UserData.UserFriends.ToList();

            for (int i = TempUsers.Count - 1; i >= 0; i--)
            {
                var item = TempUsers[i];
                if (!tempFiltered.Contains(item))
                {
                    TempUsers.Remove(item);
                }
            }

            foreach (var item in tempFiltered)
            {
                if (!TempUsers.Contains(item))
                {
                    TempUsers.Add(item);
                }
            }
        }
    }
}
