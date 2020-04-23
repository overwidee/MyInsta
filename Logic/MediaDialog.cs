using MyInsta.Model;
using MyInsta.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Point = Windows.Foundation.Point;
using User = MyInsta.Model.User;

namespace MyInsta.Logic
{
    public class MediaDialog
    {
        public string Url => MediaType == MediaType.Image ? mediaModel.UrlBigImage : mediaModel.UrlVideo;
        public MediaType MediaType { get; set; }
        int type;
        public User InstaUser { get; set; }
        public string PkMedia { get; set; }

        private CustomMedia mediaModel;

        private ObservableCollection<CustomMedia> mediasCollection;
        private int currentIndexMedia;
        private bool isScroll;

        public MediaDialog(User user, CustomMedia media, string url, MediaType mediaType, int i, ObservableCollection<CustomMedia> allMedias = null)
        {
            MediaType = mediaType;
            type = i;
            InstaUser = user;
            PkMedia = media.Pk;
            mediaModel = media;
            mediasCollection = allMedias;

            var dataPackage = new DataPackage();
            dataPackage.SetText(Url);
            Clipboard.SetContent(dataPackage);

            if (allMedias != null)
            {
                currentIndexMedia = allMedias.IndexOf(media);
            }
            isScroll = false;
        }

        public async Task ShowMediaAsync()
        {
            var bounds = Window.Current.Bounds;
            double height = bounds.Height;
            double width = bounds.Width;

            var contentDialog = new ContentDialog()
            {
                SecondaryButtonText = "Next >",
                CloseButtonText = "Close",
                PrimaryButtonText = "< Previous",
                Tag = Url,
                CornerRadius = new CornerRadius(20),
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 33, 34, 34)),
                FullSizeDesired = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MaxWidth = width
            };
            contentDialog.PrimaryButtonClick += delegate
            {
                if (currentIndexMedia > 0 && currentIndexMedia < mediasCollection.Count)
                {
                    currentIndexMedia--;
                    mediaModel = mediasCollection[currentIndexMedia];

                    isScroll = true;
                    contentDialog.Hide();
                }
            };

            contentDialog.SecondaryButtonClick += delegate
            {
                if (currentIndexMedia >= 0 && currentIndexMedia < mediasCollection.Count - 1)
                {
                    currentIndexMedia++;
                    mediaModel = mediasCollection[currentIndexMedia];

                    isScroll = true;
                    contentDialog.Hide();
                }
            };

            contentDialog.CloseButtonClick += delegate
            {
                isScroll = false;
            };

            contentDialog.Closed += async (sender, args) =>
            {
                if (isScroll)
                {
                    var mediaDialog =
                        new MediaDialog(InstaUser, mediaModel, Url, mediaModel.MediaType, 0, mediasCollection);
                    await mediaDialog.ShowMediaAsync();
                }
            };

            //contentDialog.PreviewKeyDown += (sender, args) =>
            //{
            //    if (args.Key == VirtualKey.Escape)
            //    {
            //        isScroll = false;
            //    }
            //};

            switch (MediaType)
            {
                case MediaType.Video:
                {
                    var media = new MediaElement()
                    {
                        Source = new Uri(Url),
                        AutoPlay = true,
                        HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                        VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
                        IsLooping = true
                    };

                    if (!string.IsNullOrEmpty(mediaModel.Caption))
                    {
                        var toolTip = new ToolTip
                        {
                            Content = mediaModel.Caption
                        };
                        ToolTipService.SetToolTip(media, toolTip);
                    }

                    media.DoubleTapped += async (s, e) =>
                    {
                        contentDialog.Hide();
                        var newCoreAppView = CoreApplication.CreateNewView();
                        var appView = ApplicationView.GetForCurrentView();
                        await newCoreAppView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                        {
                            var window = Window.Current;
                            var newAppView = ApplicationView.GetForCurrentView();

                            var frame = new Frame();
                            window.Content = frame;

                            frame.Navigate(typeof(BlankPage), Url);
                            window.Activate();
                            await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newAppView.Id, ViewSizePreference.UseMinimum, appView.Id, ViewSizePreference.UseMinimum);
                        });
                    };
                    contentDialog.Content = media;

                    break;
                }
                case MediaType.Image:
                {
                    var imageMedia = new Image()
                    {
                        Source = new BitmapImage(new Uri(Url, UriKind.Absolute)),
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                        VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center
                    };

                    if (!string.IsNullOrEmpty(mediaModel.Caption))
                    {
                        var toolTip = new ToolTip
                        {
                            Content = mediaModel.Caption
                        };
                        ToolTipService.SetToolTip(imageMedia, toolTip);
                    }

                    imageMedia.DoubleTapped += async (s, e) =>
                    {
                        contentDialog.Hide();
                        var newCoreAppView = CoreApplication.CreateNewView();
                        var appView = ApplicationView.GetForCurrentView();
                        await newCoreAppView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                            async () =>
                            {
                                var window = Window.Current;
                                var newAppView = ApplicationView.GetForCurrentView();

                                var frame = new Frame();
                                window.Content = frame;

                                frame.Navigate(typeof(ImagePage), Url);
                                window.Activate();
                                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newAppView.Id,
                                    ViewSizePreference.UseLess, appView.Id, ViewSizePreference.UseLess);
                            });
                    };

                    contentDialog.Content = imageMedia;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _ = await contentDialog.ShowAsync();
        }
    }
}
