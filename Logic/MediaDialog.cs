using MyInsta.Model;
using MyInsta.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MyInsta.Logic
{
    public class MediaDialog
    {
        public string Url { get; set; }
        public MediaType MediaType { get; set; }
        int type;
        public User InstaUser { get;set; }
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
            ContentDialog contentDialog = new ContentDialog()
            {
                Height = (type == 0) ? 1100 : 800,
                Width = (type == 0) ? 800 : 1100,
                SecondaryButtonText = "All right"
            };
            Grid grid = new Grid();
            Col
            //StackPanel stackPanel = new StackPanel() { Width = (type == 0) ? 800 : 1100,
            //    Height = (type == 0) ? 1100 : 800,
            //    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center };
            if (MediaType == MediaType.Video)
            {
                var media = new MediaElement()
                {
                    Source = new Uri(Url),
                    Width = (type == 0) ? 350 : 1000,
                    Height = (type == 0) ? 1100 : 450,
                    AutoPlay = true,
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                    VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center
                };
                contentDialog.Content = media;
                //stackPanel.Children.Add(media);
            }
            else if (MediaType == MediaType.Image)
            {
                var imageMedia = new Image()
                {
                    Source = new BitmapImage(
                        new Uri(Url, UriKind.Absolute)),
                    Width = (type == 0) ? 350 : 1000,
                    Height = (type == 0) ? 1100 : 450,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                    VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center
                };
                contentDialog.Content = imageMedia;
                //stackPanel.Children.Add(imageMedia);
            }
            //StackPanel panelButtons = new StackPanel() { Orientation = Orientation.Horizontal };
            //if (type != 0)
            //{
            //    Button button = new Button() { Margin = new Windows.UI.Xaml.Thickness(0, 10, 10, 0), CornerRadius = new Windows.UI.Xaml.CornerRadius(5) };
            //    button.Content = new SymbolIcon() { Symbol = Symbol.Emoji };
            //    button.Click += async (s, e) => {
            //        await InstaServer.SaveMediaInProfile(InstaUser, PkMedia);
            //    };
            //    panelButtons.Children.Add(button);
            //    Button buttonSend = new Button() { Margin = new Windows.UI.Xaml.Thickness(0, 10, 10, 0), CornerRadius = new Windows.UI.Xaml.CornerRadius(5) };
            //    buttonSend.Content = new SymbolIcon() { Symbol = Symbol.Share };
            //    buttonSend.Click += async (s, e) => {
            //        contentDialog.Hide();
            //        ContentDialog contentShared = new ContentDialog()
            //        {
            //            PrimaryButtonText = "Send",
            //            SecondaryButtonText = "Cancel",
            //            Width = 1200

            //        };
            //        Frame frame = new Frame() { Width = 1000, Height = 400 };
            //        frame.Navigate(typeof(SharedPage), new object[] { InstaUser, MediaType, PkMedia });
            //        contentShared.Content = frame;
            //        var dialog = await contentShared.ShowAsync();
            //        if (dialog == ContentDialogResult.Primary)
            //        {
            //            var page = frame.Content as SharedPage;
            //            if (page.SelectedUser != null)
            //            {
            //                var b = await InstaServer.SharedInDirect(InstaUser, PkMedia, MediaType, page.SelectedUser.Pk);
            //            };
            //            //_ = contentDialog.ShowAsync();
            //        }
            //    };
            //    panelButtons.Children.Add(buttonSend);

            //    stackPanel.Children.Add(panelButtons);
            //}

            //contentDialog.Content = //stackPanel;
            _ = await contentDialog.ShowAsync();
        }
    }
}
