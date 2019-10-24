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
    public sealed partial class StoriesPage : Page
    {
        public User InstaUser { get; set; }
        public UserStory SelectedUserStory { get; set; }
        public ObservableCollection<CustomMedia> Stories { get; set; }
        public StoriesPage()
        {
            this.InitializeComponent();

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
            await InstaServer.DownloadMedia(Stories.Where(x => x.Name == ((Button)sender).Tag.ToString()).First());
        }

        private async void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Stories = await InstaServer.GetStoryUser(InstaUser, SelectedUserStory.User.Pk);
            storiesList.ItemsSource = Stories;
            userBox.Text = SelectedUserStory.User.UserName;

            scrollList.ChangeView(null, 0, 1, true);
        }

        private async void StoriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var story = e.AddedItems[0] as CustomMedia;
                if (story != null)
                {
                    string urlMedia = "";
                    if (story.MediaType == MediaType.Image)
                        urlMedia = story.UrlBigImage;
                    else if (story.MediaType == MediaType.Video)
                        urlMedia = story.UrlVideo;

                    MediaDialog mediaDialog = new MediaDialog(InstaUser, story.Pk, urlMedia, story.MediaType, 0);
                    await mediaDialog.ShowMediaAsync();
                }
            }
            catch
            {

            }
            finally
            {
                ((ListView)sender).SelectedItem = null;
            }
        }

        private async void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var story = Stories.Where(x => x.Pk == ((Image)sender).Tag.ToString()).FirstOrDefault();
            if (story != null)
            {
                string urlMedia = "";
                if (story.MediaType == MediaType.Image)
                    urlMedia = story.UrlBigImage;
                else if (story.MediaType == MediaType.Video)
                    urlMedia = story.UrlVideo;

                MediaDialog mediaDialog = new MediaDialog(InstaUser, story.Pk, urlMedia, story.MediaType, 0);
                await mediaDialog.ShowMediaAsync();
            }
        }
    }
}
