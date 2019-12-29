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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace MyInsta.View
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class StoriesPage : Page
    {
        public User InstaUser { get; set; }
        public UserStory SelectedUserStory { get; set; }
        public ObservableCollection<CustomMedia> Stories { get; set; }

        public StoriesPage()
        {
            InitializeComponent();

            progressStories.IsActive = !InstaServer.IsStoriesLoaded;
            InstaServer.OnUserStoriesLoaded += () => progressStories.IsActive = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            InstaUser = e.Parameter as User;
            SelectedUserStory = InstaUser.UserData.Stories?[0] ?? new UserStory();
        }

        private async void ButtonDownloadStory_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.DownloadMedia(Stories.First(x => x.Name == ((Button)sender).Tag.ToString()));
        }

        private async void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedUserStory.User != null)
            {
                Stories = await InstaServer.GetStoryUser(InstaUser, SelectedUserStory.User.Pk);
                storiesList.ItemsSource = Stories;
                userBox.Content = SelectedUserStory.User.UserName;
                imageBack.Source = new BitmapImage
                    (new Uri(SelectedUserStory.User.ProfilePicUrl));

                scrollList.ChangeView(null, 0, 1, true);
            }
        }

        private async void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var story = Stories.FirstOrDefault(x => x.Pk == ((Image)sender).Tag.ToString());
            if (story == null)
            {
                return;
            }

            var urlMedia = "";
            switch (story.MediaType)
            {
                case MediaType.Image:
                    urlMedia = story.UrlBigImage;
                    break;
                case MediaType.Video:
                    urlMedia = story.UrlVideo;
                    break;
            }

            var mediaDialog = new MediaDialog(InstaUser, story.Pk, urlMedia, story.MediaType, 0);
            await mediaDialog.ShowMediaAsync();
        }

        private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender,
            AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            bool result = await InstaServer.AnswerToStory(InstaUser, sender.Text, sender.Tag.ToString(),
                SelectedUserStory.User.Pk);

            if (result)
            {
                _ = new CustomDialog("Message", $"Message send to {SelectedUserStory.User.UserName}", "Ok");
                sender.Text = "";
            }
            else
            {
                _ = new CustomDialog("Message", "Error", "Ok");
            }
        }

        private async void userBox_Click(object sender, RoutedEventArgs e)
        {
            var selectUser = await InstaServer.GetInstaUserShortById(InstaUser, SelectedUserStory.User.Pk);
            Frame.Navigate(typeof(PersonPage), new object[]
            {
                selectUser,
                InstaUser
            });
        }
    }
}
