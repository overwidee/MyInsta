using InstagramApiSharp.API;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MyInsta.Model
{
    public class User
    {
        public string LoginUser { get; set; }
        public string PasswordUser { get; set; }
        public IInstaApi Api { get; set; }
        public UserData UserData { get; set; }
        public UserSettings Settings { get; set; }
        public User()
        {
            UserData = new UserData();
            Settings = new UserSettings();
        }
    }
}
