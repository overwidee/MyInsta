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
        public ObservableCollection<InstaUserShort> UserFollowers { get; set; }
        public ObservableCollection<InstaUserShort> UserUnfollowers { get; set; }
        public ObservableCollection<InstaUserShort> UserFriends { get; set; }
        public ObservableCollection<PostItem> Feed { get; set; }
        public ObservableCollection<PostItem> SavedPostItems { get; set; }
        public ObservableCollection<UserStory> Stories { get; set; }
        public ObservableCollection<InstaUserShort> Bookmarks { get; set; }
        public ObservableCollection<InstaDirectInboxThread> DirectItems { get; set; }
        public UserData()
        {
            UserFollowers = new ObservableCollection<InstaUserShort>();
            UserFriends = new ObservableCollection<InstaUserShort>();
            UserUnfollowers = new ObservableCollection<InstaUserShort>();
            SavedPostItems = new ObservableCollection<PostItem>();
            Feed = new ObservableCollection<PostItem>();
            Bookmarks = new ObservableCollection<InstaUserShort>();
            DirectItems = new ObservableCollection<InstaDirectInboxThread>();
        }
    }
}
