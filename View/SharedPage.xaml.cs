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
    public sealed partial class SharedPage : Page
    {
        public SharedPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var objs = e.Parameter as object[];
            InstaUser = objs[0] as User;
            mediaType = (MediaType)objs[1];
            mediaId = objs[2] as string;
        }

        public User InstaUser { get; set; }
        public InstaUserShort SelectedUser { get; set; } = null;
        MediaType mediaType;
        string mediaId;

        private void ListFollowers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedUser = e.AddedItems[0] as InstaUserShort;
        }
        private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            listFollowers.ItemsSource = InstaServer.SearchByUserName(InstaUser.UserData.UserFriends, ((AutoSuggestBox)sender).Text);
        }

        private void SharedPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            ((ContentDialog)((Frame)Parent).Parent).Background = Background;
        }
    }
}
