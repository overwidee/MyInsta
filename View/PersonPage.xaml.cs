using InstagramApiSharp.Classes.Models;
using MyInsta.Logic;
using MyInsta.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using MyInsta.Logic.ChartModel;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;
using Chart = MyInsta.Logic.ChartModel.Chart;

namespace MyInsta.View
{
    public sealed partial class PersonPage : Page
    {
        public PersonPage()
        {
            InitializeComponent();

            progressPosts.IsActive = !InstaServer.IsPostsLoaded;
            InstaServer.OnUserPostsLoaded += UsersPostsLoaded;
            InstaServer.OnUserAllPostsLoaded += UserAllPostsLoaded;
            InstaServer.OnDynamicUserMediaLoaded += () =>
            {
                foreach (var postItem in CurrentUser.UserData.PostsLastUser)
                {
                    var m = Posts.FirstOrDefault(x => x.Id == postItem.Id);
                    if (m == null)
                    {
                        Posts.Add(postItem);
                    }
                }

                progressPosts.IsActive = false;
                postTab.Header = $"Posts ({Posts.Count})";
                saveButton.Text = $"Download posts ({Posts.Count})";

                if ((likeChart.Series[0] as LineSeries)?.ItemsSource == null && Posts.Count != 0)
                {
                    StatTab.Visibility = Visibility.Visible;

                    ((LineSeries)likeChart.Series[0]).ItemsSource = Chart.GetChartLikes(Posts);
                    ((LineSeries)commentChart.Series[0]).ItemsSource = Chart.GetChartComments(Posts);

                    int maxValue = Chart.GetMax(Posts, x => x.Items[0].CountLikes);
                    int minValue = Chart.GetMin(Posts, x => x.Items[0].CountLikes);
                    double averageValue = Chart.GetAverage(Posts, x => x.Items[0].CountLikes);
                    ChartMaxLikes.Text = $"Max likes = {maxValue}";
                    ChartMinLikes.Text = $"Min likes = {minValue}";
                    ChartAverageLikes.Text = $"Average likes = {averageValue:F}";

                    int months = (Posts.Max(x => x.Items[0].Date) - Posts.Min(x => x.Items[0].Date)).Days / 30;
                    AxisLikesY.Interval = (maxValue - minValue) / 10;
                    AxisLikesX.Interval = AxisCommentX.Interval = months == 0 ? 1 : months / (months / 3);

                    maxValue = Chart.GetMax(Posts, x => x.Items[0].CountComments);
                    minValue = Chart.GetMin(Posts, x => x.Items[0].CountComments);
                    averageValue = Chart.GetAverage(Posts, x => x.Items[0].CountComments);
                    ChartMaxComments.Text = $"Max comments = {maxValue}";
                    ChartMinComments.Text = $"Min comments = {minValue}";
                    ChartAverageComments.Text = $"Average comments = {averageValue:F}";

                    AxisCommentY.Interval = (maxValue - minValue) / 10;
                }
            };
        }

        #region CompleteEvent
        private void UsersPostsLoaded()
        {
            progressPosts.IsActive = false;
            postBox.IsEnabled = true;
        }

        private void UserAllPostsLoaded()
        {
            //ProgressAllPosts.IsActive = false;
            postBox.IsEnabled = true;
        }

        private void UserInfoLoaded()
        {
            Bindings.Update();
        }
        #endregion
        public InstaUserShort SelectUser { get; set; }
        public InstaUserInfo InstaUserInfo { get; set; }
        public User CurrentUser { get; set; }
        public bool ButtonFollow { get; set; }
        public bool ButtonUnFollow { get; set; }

        public ObservableCollection<PostItem> Posts { get; set; } = new ObservableCollection<PostItem>();
        public ObservableCollection<CustomMedia> UrlStories { get; set; }
        public ObservableCollection<CustomMedia> HighlightsStories { get; set; }
        public InstaHighlightFeeds InstaHighlightFeeds { get; set; }

        int countPosts = 24;

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is object[] obj)
            {
                SelectUser = obj[0] as InstaUserShort;
                CurrentUser = obj[1] as User;
            }

            InstaUserInfo = await InstaServer.GetInfoUser(CurrentUser, SelectUser.UserName);
            ButtonFollow = !InstaUserInfo.FriendshipStatus.Following;
            ButtonUnFollow = InstaUserInfo.FriendshipStatus.Following;

            //await InstaServer.SendMessage(CurrentUser, InstaUserInfo.Pk);


            UserInfoLoaded();
            SetBookmarkStatus();

            InstaHighlightFeeds = await InstaServer.GetArchiveCollectionStories(CurrentUser, SelectUser.Pk);
            collectionsBox.ItemsSource = InstaHighlightFeeds.Items;

            UrlStories = await InstaServer.GetStoryUser(CurrentUser, InstaUserInfo);
            storiesList.ItemsSource = UrlStories;

            if (InstaHighlightFeeds.Items.Count > 0)
            {
                highTab.Visibility = Visibility.Visible;
            }

            if (UrlStories.Count > 0)
            {
                storyTab.Visibility = Visibility.Visible;
            }


            CurrentUser.UserData.PostsLastUser.Clear();
            InstaServer.MediasUserMaxId = "";
            await InstaServer.GetDynamicMediaUser(CurrentUser, InstaUserInfo);
        }

        public ObservableCollection<PostItem> AllPosts { get; set; } = new ObservableCollection<PostItem>();

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
            await InstaServer.DownloadAnyPosts(SelectUser, Posts);
        }
        private async void UnlikeButton_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.UnlikeProfile(CurrentUser, SelectUser, Posts);
        }
        private async void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.DownloadAnyPost(await InstaServer.GetInstaUserShortById(CurrentUser,
                    Posts.First(x => x.Id == int.Parse(((MenuFlyoutItem)sender).Tag.ToString())).UserPk),
                Posts.First(x => x.Id == int.Parse(((MenuFlyoutItem)sender).Tag.ToString())).Items);
        }
        private async void ButtonDownloadStory_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.DownloadMedia(UrlStories
                .First(x => x.Name == ((Button)sender).Tag.ToString()));
        }
        private async void ScrollListPosts_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var svPosts = sender as ScrollViewer;

            double verticalOffset = svPosts.VerticalOffset;
            double maxVerticalOffset = svPosts.ScrollableHeight;


            if (verticalOffset >= maxVerticalOffset - maxVerticalOffset / 3 && Posts.Count < InstaUserInfo.MediaCount
                && string.IsNullOrEmpty(postBox.Text))
            {
                await InstaServer.GetDynamicMediaUser(CurrentUser, InstaUserInfo);
            }
        }

        private void MediaList_SelectionChanged(object sender, TappedRoutedEventArgs e)
        {
            if (e.OriginalSource.GetType() != typeof(Border))
            {
                return;
            }

            if (((FlipView)sender).SelectedItem is CustomMedia post)
            {
                var urlMedia = "";
                switch (post.MediaType)
                {
                    case MediaType.Image:
                        urlMedia = post.UrlBigImage;
                        break;
                    case MediaType.Video:
                        urlMedia = post.UrlVideo;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var mediaDialog = new MediaDialog(CurrentUser, post, urlMedia, post.MediaType, 1);
                _ = mediaDialog.ShowMediaAsync();
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            InstaServer.OnUserPostsLoaded -= UsersPostsLoaded;
            InstaServer.OnUserAllPostsLoaded -= UserAllPostsLoaded;
        }

        private async void ButtonSaveInProfile_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.SaveMediaInProfile(CurrentUser, ((MenuFlyoutItem)sender).Tag.ToString());
        }

        private async void ButtonShare_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.ShareMedia(CurrentUser,
                       Posts.First(x => x.Id == int.Parse(((MenuFlyoutItem)sender).Tag.ToString())).Items);
        }

        private void PostBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!string.IsNullOrEmpty(sender.Text))
            {
                var arr = Helper.ReturnNumbers(sender.Text);

                var items = CurrentUser.UserData.PostsLastUser?.Where(x => arr.Contains(x.Id)).ToList();
                if (items.Count != 0)
                {
                    for (int i = CurrentUser.UserData.PostsLastUser.Count - 1; i >= 0; i--)
                    {
                        var item = CurrentUser.UserData.PostsLastUser[i];
                        if (!items.Contains(item))
                        {
                            var post = Posts.FirstOrDefault(x => x.Id == item.Id);
                            Posts.Remove(post);
                        }
                    }

                    foreach (var item in items)
                    {
                        if (!Posts.Contains(item))
                        {
                            Posts.Add(item);
                        }
                    }
                }
            }
            else
            {
                Posts.Clear();
                foreach (var post in CurrentUser.UserData.PostsLastUser)
                {
                    Posts.Add(post);
                }
            }
        }

        private void SetBookmarkStatus()
        {
            var button = buttonAction;
            var menu = button.Flyout as MenuFlyout;
            var item = new MenuFlyoutItem()
            {
                CornerRadius = new CornerRadius(5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                FontWeight = FontWeights.Light,
                Text = !InstaServer.IsContrainsAccount(CurrentUser, SelectUser.Pk)
                    ? "Add in bookmarks" : "Remove from bookmarks"
            };

            item.Click += async (s, e) =>
            {
                if (!InstaServer.IsContrainsAccount(CurrentUser, SelectUser.Pk))
                {
                    CurrentUser.UserData.Bookmarks.Add(SelectUser);
                    await InstaServer.SaveBookmarksAsync(CurrentUser);
                }
                else
                {
                    CurrentUser.UserData.Bookmarks.Remove(SelectUser);
                    await InstaServer.SaveBookmarksAsync(CurrentUser);
                }
            };
            menu?.Items?.Add(item);
        }

        private async void CollectionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectHigh = ((ListView)(sender)).SelectedItem as InstaHighlightFeed;
            HighlightsStories = await InstaServer.GetHighlightStories(CurrentUser, selectHigh.HighlightId);
            archiveList.ItemsSource = HighlightsStories;

            scrollArchive.ChangeView(null, 0, 1, true);
        }

        private async void ButtonDownloadHigh_Click(object sender, RoutedEventArgs e)
        {
            await InstaServer.DownloadMedia(HighlightsStories.First(x => x.Name == ((Button)sender).Tag.ToString()));
        }

        private async void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var high = HighlightsStories.FirstOrDefault(x => x.Pk == ((Border)sender).Tag.ToString());

            if (high != null)
            {
                string urlMedia = high.MediaType switch
                {
                    MediaType.Image => high.UrlBigImage,
                    MediaType.Video => high.UrlVideo,
                    _ => throw new ArgumentOutOfRangeException()
                };

                var mediaDialog = new MediaDialog(CurrentUser, high, urlMedia, high.MediaType, 0);
                await mediaDialog.ShowMediaAsync();
            }
        }

        private async void Image_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            var high = UrlStories.FirstOrDefault(x => x.Pk == ((Border)sender).Tag.ToString());
            if (high != null)
            {
                string urlMedia = high.MediaType switch
                {
                    MediaType.Image => high.UrlBigImage,
                    MediaType.Video => high.UrlVideo,
                    _ => throw new ArgumentOutOfRangeException()
                };

                var mediaDialog = new MediaDialog(CurrentUser, high, urlMedia, high.MediaType, 0);
                await mediaDialog.ShowMediaAsync();
            }
        }

        private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender,
            AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            bool result = await InstaServer.AnswerToStory(CurrentUser, sender.Text, sender.Tag.ToString(),
                SelectUser.Pk);

            if (result)
            {
                _ = new CustomDialog("Message", $"Message send to {SelectUser.UserName}", "All right");
                sender.Text = "";
            }
            else
            {
                _ = new CustomDialog("Message", "Error", "All right");
            }
        }

        private async void ButtonLike_Click(object sender, RoutedEventArgs e)
        {
            bool? isChecked = ((CheckBox)sender).IsChecked;
            if (isChecked != null && isChecked.Value)
            {
                bool like = await InstaServer.LikeMedia(CurrentUser,
                    Posts.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString()))?.Items[0]);
                PostItem first = Posts.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString()));

                if (first != null)
                {
                    first.Items[0].Liked = true;
                }
            }
            else
            {
                bool like = await InstaServer.UnlikeMedia(CurrentUser,
                    Posts.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString()))?.Items[0]);
                PostItem first = Posts.FirstOrDefault(x => x.Id == int.Parse(((CheckBox)sender).Tag.ToString()));
                if (first != null)
                {
                    first.Items[0].Liked = false;
                }
            }
        }


        private void Image_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            //base.OnPointerWheelChanged(e);
            //e.Handled = true;
        }

        private void itemsList_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
        }

        CoreCursor cursorBeforePointerEntered = null;
        private void UIElement_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            cursorBeforePointerEntered = Window.Current.CoreWindow.PointerCursor;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 0);
        }

        private void UIElement_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = cursorBeforePointerEntered;
        }

        private async void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            await InstaServer.ShowFollowers(CurrentUser, SelectUser.UserName, Frame);
        }

        private async void FollowingBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            await InstaServer.ShowFollowing(CurrentUser, SelectUser.UserName, Frame);
        }

        private async void LikesBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
        }

        private async void ProfileImageButton_OnClick(object sender, RoutedEventArgs e)
        {
            await InstaServer.Download(((MenuFlyoutItem)sender).Tag.ToString());
        }

        private async void UIElement_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var newCoreAppView = CoreApplication.CreateNewView();
            var appView = ApplicationView.GetForCurrentView();
            await newCoreAppView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () =>
                {
                    var window = Window.Current;
                    var newAppView = ApplicationView.GetForCurrentView();

                    var frame = new Frame();
                    window.Content = frame;

                    frame.Navigate(typeof(ImagePage), InstaUserInfo.HdProfilePicUrlInfo.Uri);
                    window.Activate();
                    await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newAppView.Id,
                        ViewSizePreference.UseMinimum, appView.Id, ViewSizePreference.UseMinimum);
                });
        }

        private async void MenuFlyoutItem_OnClick(object sender, RoutedEventArgs e)
        {
            await InstaServer.ShowLikers(CurrentUser, ((MenuFlyoutItem)sender).Tag.ToString(), Frame);
        }

        private void MenuFlyoutItem1_OnClick(object sender, RoutedEventArgs e)
        {
            InstaServer.ShowComments(CurrentUser, this, ((MenuFlyoutItem)sender).Tag.ToString());
        }

        private void Chart_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.OriginalSource is Ellipse source)
            {
                var point = source.DataContext as ChartModel<DateTime>;

                var myFlout = new Flyout
                {
                    Content = new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                        Children =
                        {
                            new Image()
                            {
                                Width = 300,
                                Height = 300,
                                Stretch = Stretch.Uniform,
                                Source = new BitmapImage(new Uri(point.UrlImage))
                            },
                            new TextBlock()
                            {
                                Margin = new Thickness(5),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Text = $"Likes: {point.Value}",
                                FontWeight = FontWeights.Light
                            },
                            new TextBlock()
                            {
                                Margin = new Thickness(5),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Text = $"Date: {point.Id.ToLongDateString()}",
                                FontWeight = FontWeights.Light
                            }
                        }
                    }
                };

                myFlout.ShowAt(source, new FlyoutShowOptions()
                {
                    Position = e.GetPosition(e.OriginalSource as UIElement),
                    ShowMode = FlyoutShowMode.Transient,
                    Placement = FlyoutPlacementMode.Left
                });
            }
        }

        private Flyout imageFlyout = new Flyout();
        private void LineSeries_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.OriginalSource is Ellipse source)
            {
                var point = source.DataContext as ChartModel<DateTime>;

                imageFlyout = new Flyout
                {
                    Content = new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                        Children =
                        {
                            new Image()
                            {
                                Width = 300,
                                Height = 300,
                                Stretch = Stretch.Uniform,
                                Source = new BitmapImage(new Uri(point.UrlImage))
                            },
                            new TextBlock()
                            {
                                Margin = new Thickness(5),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Text = $"Likes: {point.Value}",
                                FontWeight = FontWeights.Light
                            },
                            new TextBlock()
                            {
                                Margin = new Thickness(5),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Text = $"Date: {point.Id.ToLongDateString()}",
                                FontWeight = FontWeights.Light
                            }
                        }
                    },
                    ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway
                };

                imageFlyout.ShowAt(source, new FlyoutShowOptions()
                {
                    Position = e.GetCurrentPoint(e.OriginalSource as UIElement).Position,
                    ShowMode = FlyoutShowMode.Transient,
                    Placement = FlyoutPlacementMode.Left
                });
            }
        }

        private void LineSeries_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            imageFlyout.Hide();
        }
    }
}
