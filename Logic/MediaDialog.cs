using MyInsta.Model;
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
        public MediaDialog(string url, MediaType mediaType)
        {
            Url = url;
            MediaType = mediaType;
        }

        public void ShowMedia()
        {
            ContentDialog contentDialog = new ContentDialog()
            {
                Height = 1000,
                Width = 700,
                SecondaryButtonText = "All right"
            };
            if (MediaType == MediaType.Video)
            {
                contentDialog.Content = new MediaElement()
                {
                    Source = new Uri(Url),
                    Width = 600,
                    Height = 900,
                    AutoPlay = true,
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center
                };
            }
            else if (MediaType == MediaType.Image)
            {
                contentDialog.Content = new Image()
                {
                    Source = new BitmapImage(
                        new Uri(Url, UriKind.Absolute)),
                    Width = 600,
                    Height = 900,
                };
            }
            _ = contentDialog.ShowAsync();
        }
    }
}
