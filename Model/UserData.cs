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
        public ObservableCollection<InstaUserShort> UserFollowers { get; set; }
        public ObservableCollection<InstaUserShort> UserUnfollowers { get; set; }
        public ObservableCollection<InstaUserShort> UserFriends { get; set; }

        public UserData()
        {
            this.UserFollowers = new ObservableCollection<InstaUserShort>();
            this.UserFriends = new ObservableCollection<InstaUserShort>();
            this.UserUnfollowers = new ObservableCollection<InstaUserShort>();
        }
    }
}
