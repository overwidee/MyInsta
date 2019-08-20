using InstagramApiSharp.Classes.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyInsta.Model
{
    public class UserStory
    {
        public InstaUserShortFriendshipFull User { get; set; }
        public ObservableCollection<CustomMedia> Story { get; set; }
    }
}
