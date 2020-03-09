using InstagramApiSharp.Classes.Models;
using MyInsta.Logic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public ObservableCollection<string> FeedUsers { get; set; }
        public ObservableCollection<UserFeed> FeedObjUsers { get; set; }
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
            FeedUsers = new ObservableCollection<string>();
            Bookmarks = new ObservableCollection<InstaUserShort>();
            FeedObjUsers = new ObservableCollection<UserFeed>();
            Feed = new ObservableCollection<PostItem>();
            PostsLastUser = new ObservableCollection<PostItem>();
            Stories = new ObservableCollection<UserStory>();
            ArchivePosts = new ObservableCollection<PostItem>();
            ArchiveHigh = new InstaHighlightShortList();
            ArchiveStories = new ObservableCollection<CustomMedia>();
        }
    }

    public class UserFeed
    {
        public InstaUserShort InstaUserShort { get; set; }
        public bool Received { get; set; }
    }
}
