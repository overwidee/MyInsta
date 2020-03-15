using InstagramApiSharp.Classes.Models;
using MyInsta.Logic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace MyInsta.Model
{
    public class UserData
    {
        public long Pk { get; set; }
        public string UrlPicture { get; set; }
        public ObservableCollection<InstaUserShort> UserFollowers { get; set; }
        public ObservableCollection<InstaUserShort> UserFollowing { get; set; }
        public ObservableCollection<InstaUserShort> UserUnfollowers { get; set; }
        public ObservableCollection<InstaUserShort> UserFriends { get; set; }
        public ObservableCollection<PostItem> Feed { get; set; }
        public ObservableCollection<PostItem> SavedPostItems { get; set; }
        public ObservableCollection<UserStory> Stories { get; set; }
        public ObservableCollection<InstaUserShort> Bookmarks { get; set; }
        public ObservableCollection<PostItem> PostsLastUser { get; set; }
        public ObservableCollection<PostItem> ArchivePosts { get; set; }
        public InstaHighlightShortList ArchiveHigh { get; set; }
        public ObservableCollection<CustomMedia> ArchiveStories { get; set; }
        public UserData()
        {
            UserFollowers = new ObservableCollection<InstaUserShort>();
            UserFollowing = new ObservableCollection<InstaUserShort>();
            UserFriends = new ObservableCollection<InstaUserShort>();
            UserUnfollowers = new ObservableCollection<InstaUserShort>();
            SavedPostItems = new ObservableCollection<PostItem>();
            Bookmarks = new ObservableCollection<InstaUserShort>();
            Feed = new ObservableCollection<PostItem>();
            PostsLastUser = new ObservableCollection<PostItem>();
            Stories = new ObservableCollection<UserStory>();
            ArchivePosts = new ObservableCollection<PostItem>();
            ArchiveHigh = new InstaHighlightShortList();
            ArchiveStories = new ObservableCollection<CustomMedia>();
        }
    }

    public class UserSettings
    {
        public UserSettings()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            StoryHeight = (double?)localSettings.Values["StoryHeight"] ?? 550;
            StoryWidth = (double?)localSettings.Values["StoryWidth"] ?? 350;
            PostWidth = (double?)localSettings.Values["PostWidth"] ?? 500;
            PostHeight = (double?)localSettings.Values["PostHeight"] ?? 500;
        }

        public double PostWidth { get; set; }
        public double PostHeight { get; set; }
        public double StoryHeight { get; set; }
        public double StoryWidth { get; set; }
    }
}
