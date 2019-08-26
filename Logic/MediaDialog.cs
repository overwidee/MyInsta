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
            }
            _ = await contentDialog.ShowAsync();
        }
    }
}
