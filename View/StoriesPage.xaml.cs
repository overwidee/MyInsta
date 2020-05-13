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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using WinRTXamlToolkit.Controls.Extensions;

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
        public ObservableCollection<CustomMedia> Stories { get; set; } = new ObservableCollection<CustomMedia>();

        public StoriesPage()
        {
            InitializeComponent();

            InstaServer.OnUserStoriesLoaded += OnUserStoriesLoaded;
            InstaServer.OnStoriesLoaded += OnUserStoriesLoaded;
        }

        private void OnUserStoriesLoaded()
        {
            Stories?.Clear();
            progressStories.IsActive = false;
            InstaServer.OnUserStoriesLoaded -= OnUserStoriesLoaded;
            Bindings.Update();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            InstaUser = e.Parameter as User;

            if (InstaUser != null)
            {
                InstaUser.UserData.Stories.Clear();
                await InstaServer.GetCurrentUserStories(InstaUser);
            }
        }

        private async void ButtonDownloadStory_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.DownloadMedia(Stories.First(x => x.Name == ((Button)sender).Tag.ToString()));
        }

        private async void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedUserStory?.User != null)
            {
                storiesList.ItemsSource = null;
                
                var item = SelectedUserStory;

                var listViewItem = (FrameworkElement)ListViewStories.ContainerFromItem(item);

                if (listViewItem == null)
                {
                    ListViewStories.ScrollIntoView(item);
                }

                while (listViewItem == null)
                {
                    await Task.Delay(1); // wait for scrolling to complete - it takes a moment
                    listViewItem = (FrameworkElement)ListViewStories.ContainerFromItem(item);
                }

                var topLeft =
                    listViewItem
                        .TransformToVisual(ListViewStories)
                        .TransformPoint(new Point()).X;
                var lvih = listViewItem.ActualWidth;
                var lvh = ListViewStories.ActualWidth;
                var desiredTopLeft = (lvh - lvih) / 2.0;
                var desiredDelta = topLeft - desiredTopLeft;

                var scrollViewer = ListViewStories.GetFirstDescendantOfType<ScrollViewer>();
                var currentOffset = scrollViewer.HorizontalOffset;
                var desiredOffset = currentOffset + desiredDelta;
                scrollViewer.ChangeView(desiredOffset, null, 1, true);

                progressStories.IsActive = true;
                Stories = await InstaServer.GetStoryUser(InstaUser, SelectedUserStory.User.Pk);
                storiesList.ItemsSource = Stories;
                userBox.Content = SelectedUserStory?.User.UserName;
                userBox.Visibility = Visibility.Visible;

                imageBack.Source = new BitmapImage
                    (new Uri(SelectedUserStory?.User?.ProfilePicUrl));

                mainScroll.ChangeView(null, 0, 1, true);
            }
        }

        private async void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var story = Stories.FirstOrDefault(x => x.Pk == ((Border)sender).Tag.ToString());
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

            var mediaDialog = new MediaDialog(InstaUser, story, urlMedia, story.MediaType, 0, Stories);
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

        private async void ButtonViewersStory_OnClick(object sender, RoutedEventArgs e)
        {
            await InstaServer.ShowViewers(InstaUser, ((Button)sender).Tag.ToString(), Frame);
        }
    }
}
