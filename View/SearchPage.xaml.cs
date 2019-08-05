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
    public sealed partial class SearchPage : Page
    {
        public SearchPage()
        {
            this.InitializeComponent();
        }
        public User InstaUser { get; set; }
        public ObservableCollection<InstaUserShort> SearchUsers { get; set; }
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

        private async void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var user = await InstaServer.SearchByUserName(InstaUser, ((AutoSuggestBox)sender).Text);
            if (user != null)
            {
                SearchUsers = new ObservableCollection<InstaUserShort>();
                SearchUsers.Add(user);

                listSearch.ItemsSource = null;
                listSearch.ItemsSource = SearchUsers;
            }
        }
    }
}
