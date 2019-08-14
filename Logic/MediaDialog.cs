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
        int type;
        public MediaDialog(string url, MediaType mediaType, int i)
        {
            Url = url;
            MediaType = mediaType;
            type = i;
        }

        public void ShowMedia()
        {
            ContentDialog contentDialog = new ContentDialog()
            {
                Height = (type == 0) ? 1100 : 800,
                Width = (type == 0) ? 800 : 1100,
                SecondaryButtonText = "All right"
            };
            if (MediaType == MediaType.Video)
            {
                contentDialog.Content = new MediaElement()
                {
                    Source = new Uri(Url),
                    Width = (type == 0) ? 350 : 1000,
                    Height = (type == 0) ? 900 : 450,
                    AutoPlay = true,
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                    VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center
                };
            }
            else if (MediaType == MediaType.Image)
            {
                contentDialog.Content = new Image()
                {
                    Source = new BitmapImage(
                        new Uri(Url, UriKind.Absolute)),
                    Width = (type == 0) ? 350 : 1000,
                    Height = (type == 0) ? 900 : 450,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                    VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center
                };
            }
            _ = contentDialog.ShowAsync();
        }
    }
}
