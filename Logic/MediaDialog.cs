using MyInsta.Model;
using MyInsta.View;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Input;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Point = Windows.Foundation.Point;

namespace MyInsta.Logic
{
    public class MediaDialog
    {
        public string Url { get; set; }
        public MediaType MediaType { get; set; }
        int type;
        public User InstaUser { get; set; }
        public string PkMedia { get; set; }

        public MediaDialog(User user, string pk, string url, MediaType mediaType, int i)
        {
            Url = url;
            MediaType = mediaType;
            type = i;
            InstaUser = user;
            PkMedia = pk;
        }

        public async Task ShowMediaAsync()
        {
            var contentDialog = new ContentDialog()
            {
                //Height = (type == 0) ? 1100 : 1000,
                //Width = (type == 0) ? 1000 : 1100,
                SecondaryButtonText = "All right",
                PrimaryButtonText = "Copy link",
                Tag = Url,
                FullSizeDesired = true
            };
            contentDialog.PrimaryButtonClick += delegate
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(Url);
                Clipboard.SetContent(dataPackage);
            };

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
                    media.Tapped += async (s, e) =>
                    {
                        var popup = new PopupMenu();
                        popup.Commands.Add(new UICommand()
                        {
                            Label = "Full screen",
                            Invoked = async (args) =>
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
                            }
                        });
                        await popup.ShowAsync(new Point(e.GetPosition(contentDialog).X, e.GetPosition(contentDialog).Y));
                    };
                    contentDialog.Content = media;
                    break;
                }
                case MediaType.Image:
                {
                    var imageMedia = new Image()
                    {
                        Source = new BitmapImage(new Uri(Url, UriKind.Absolute)),
                        //Width = (type == 0) ? 350 : 1000,
                        //Height = (type == 0) ? 1100 : 450,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                        VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center
                    };
                    imageMedia.Tapped += async (s, e) =>
                    {
                        var popup = new PopupMenu();
                        popup.Commands.Add(new UICommand()
                        {
                            Label = "Full screen",
                            Invoked = async (args) =>
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
                                            ViewSizePreference.UseMinimum, appView.Id, ViewSizePreference.UseMinimum);
                                    });
                            }
                        });
                        await popup.ShowAsync(new Point(e.GetPosition(contentDialog).X, e.GetPosition(contentDialog).Y));
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
