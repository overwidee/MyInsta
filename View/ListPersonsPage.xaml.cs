using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
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
using InstagramApiSharp.Classes.Models;
using MyInsta.Logic;
using MyInsta.Model;
using DataType = MyInsta.Logic.DataType;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MyInsta.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ListPersonsPage : Page
    {
        public ListPersonsPage()
        {
            InitializeComponent();

            InstaServer.OnCommonDataLoaded += () =>
            {
                ProgressRing.IsActive = false;
            };
        }

        public User InstaUser { get; set; }
        public ObservableCollection<InstaUserShort> ListInstaUserShorts { get; set; }
        public Frame ToFrame { get; set; }
        public DataType MainType { get; set; }
        private string UserName { get; set; }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is object[] obj)
            {
                InstaUser = obj[0] as User;
                UserName = obj[1].ToString();
                MainType = (DataType)obj[2];
                ToFrame = obj[3] as Frame;
            }

            if (MainType == DataType.Likers)
            {
                GetAllButton.Visibility = Visibility.Collapsed;
            }

            await GetMainData(UserName);
        }

        private void SearchBox_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            ListFollowers.ItemsSource = string.IsNullOrEmpty(((AutoSuggestBox)sender).Text)
                ? ListInstaUserShorts
                : ListInstaUserShorts.Where(x => x.UserName.Contains(((AutoSuggestBox)sender).Text));
        }

        private async Task GetMainData(string userName, bool all = false)
        {
            ProgressRing.IsActive = true;
            ListFollowers.ItemsSource = null;

            ListInstaUserShorts = new ObservableCollection<InstaUserShort>();
            ListInstaUserShorts = MainType switch
            {
                DataType.Followers => await InstaServer.GetFollowers(InstaUser, userName, all),
                DataType.Following => await InstaServer.GetFollowing(InstaUser, userName, all),
                DataType.Likers => await InstaServer.GetLikers(InstaUser, userName),
                DataType.Viewers => await InstaServer.GetViewersStory(InstaUser, userName),
                _ => ListInstaUserShorts
            };

            ListFollowers.ItemsSource = ListInstaUserShorts;

            //if (ListInstaUserShorts == null)
            //{
            //    ((ContentDialog)((Frame)Parent).Parent).Hide();
            //    return;
            //}
            BlockCount.Text = $"Count: {ListInstaUserShorts?.Count}";
        }

        private void ListFollowers_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems[0] is InstaUserShort user)
            {
                ToFrame.Navigate(typeof(PersonPage), new object[] { user, InstaUser });
                ((ContentDialog)((Frame)Parent).Parent).Hide();
            } 
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            await GetMainData(UserName, true);
        }

        private void ListPersonsPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            ((ContentDialog)((Frame)Parent).Parent).Background = Background;
        }
    }
}
