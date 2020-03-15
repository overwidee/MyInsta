using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;

namespace MyInsta.Model
{
    public class CustomMedia
    {
        public string Pk { get; set; }
        public string Name { get; set; }
        public string UrlSmallImage { get; set; }
        public string UrlBigImage { get; set; }
        public int CountLikes { get; set; }
        public int CountComments { get; set; }
        public bool Liked { get; set; }
        public MediaType MediaType { get; set; }
        public PostType PostType { get; set; }
        public string UrlVideo { get; set; }
        public DateTime Date { get; set; }
        public string DatePost => $"{Date.ToLongDateString()} {Date.ToLongTimeString()}";
        public string CountLikersShow => $"Likes ({CountLikes})";
        public string CountCommentsShow => $"Comments ({CountComments})";
        public Visibility IsVideoVisibility => MediaType == MediaType.Video ? Visibility.Visible : Visibility.Collapsed;
        public double GetWidth
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                return PostType switch
                {
                    PostType.Story => ((double?)localSettings.Values["StoryWidth"] ?? 350),
                    PostType.Post => ((double?)localSettings.Values["PostWidth"] ?? 500),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            
        }
        public double GetHeight
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                return PostType switch
                {
                    PostType.Story => ((double?)localSettings.Values["StoryHeight"] ?? 550),
                    PostType.Post => ((double?)localSettings.Values["PostHeight"] ?? 500),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
    }
}


public enum MediaType
{
    Image,
    Video
}

public enum PostType
{
    Story,
    Post
}
