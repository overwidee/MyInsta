using InstagramApiSharp.API;
using MyInsta.Logic;
using MyInsta.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MyInsta.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VerifyPage : Page
    {
        public VerifyPage()
        {
            InitializeComponent();
        }

        public User InstaUser { get; set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            InstaUser = e.Parameter as User;
        }
        private async void sendButton_Click(object sender, RoutedEventArgs e)
        {
            bool result = radioSMS.IsChecked.Value 
                ? await InstaServer.SendSMSVerify(InstaUser.Api) 
                : await InstaServer.SendEmailVerify(InstaUser.Api);
        }

        private async void loginButton_Click(object sender, RoutedEventArgs e)
        {
            MainProgressRing.Visibility = Visibility.Visible;
            IsEnabled = false;
            var api = await InstaServer.LoginByCode(InstaUser, textCode.Text);
            if (api != null)
            {
                InstaUser.Api = api;
                Frame.Navigate(typeof(MenuPage), InstaUser);
            }
            IsEnabled = true;
        }
    }
}
