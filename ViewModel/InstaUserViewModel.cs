using MyInsta.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using System.Threading.Tasks;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Logger;

namespace MyInsta.ViewModel
{
    public class InstaUserViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private User mainUser;

        public InstaUserViewModel()
        {
            mainUser = new User();
        }

        public string LoginUser
        {
            get { return mainUser.LoginUser; }
            set
            {
                if (mainUser.LoginUser != value)
                {
                    mainUser.LoginUser = value;
                    OnPropertyChanged("LoginUser");
                }
            }
        }

        public string PasswordUser
        {
            get { return mainUser.PasswordUser; }
            set
            {
                if (mainUser.PasswordUser != value)
                {
                    mainUser.PasswordUser = value;
                    OnPropertyChanged("PasswordUser");
                }
            }
        }

        //private void LoginCommand(object userObject)
        //{
        //    AsyncHelpers.RunSync(() => LoginAsync(userObject));
        //}

      

        protected void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }
}
