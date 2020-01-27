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

namespace MyInsta.View
{
    public enum MyResult
    {
        Add,
        Cancel
    }

    public sealed partial class ReturnPersonPage : ContentDialog
    {
        public ReturnPersonPage(User user, DataType typeList)
        {
            InitializeComponent();

            InstaUser = user;
            var list = new ObservableCollection<InstaUserShort>();
            switch (typeList)
            {
                case DataType.Followers:
                    list = InstaUser.UserData.UserFollowers;
                    break;
                case DataType.Following:
                    list = InstaUser.UserData.UserFollowing;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeList), typeList, null);
            }
            if (InstaUser != null)
            {
                foreach (var friend in list)
                {
                    if (!InstaUser.UserData.FeedUsers.Contains(friend.UserName))
                    {
                        ListInstaUserShorts.Add(friend);
                    }
                }
                ListFollowers.ItemsSource = ListInstaUserShorts;
            }
        }
        public MyResult DialogResult { get; set; }
        public User InstaUser { get; set; }

        public ObservableCollection<InstaUserShort> ListInstaUserShorts { get; set; } = new
            ObservableCollection<InstaUserShort>();

        public ObservableCollection<InstaUserShort> SelectedUserShorts { get; set; }
        private void SearchBox_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            ListFollowers.ItemsSource = string.IsNullOrEmpty(((AutoSuggestBox)sender).Text)
                ? ListInstaUserShorts
                : ListInstaUserShorts.Where(x => x.UserName.Contains(((AutoSuggestBox)sender).Text));
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            SelectedUserShorts = new ObservableCollection<InstaUserShort>();
            foreach (var user in ListFollowers.SelectedItems)
            {
                SelectedUserShorts.Add(user as InstaUserShort);
            }
            DialogResult = MyResult.Add;
            Hide();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = MyResult.Cancel;
            Hide();
        }

        private void ReturnPersonPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            this.Background = MainGrid.Background;
        }
    }
}
