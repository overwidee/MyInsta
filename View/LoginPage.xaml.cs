using MyInsta.Logic;
using MyInsta.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace MyInsta.View
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            this.InitializeComponent();

            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            UserInsta.LoginUser = localSettings.Values["Login"] != null ? localSettings.Values["Login"].ToString() : "";
            UserInsta.PasswordUser = localSettings.Values["Password"] != null ? localSettings.Values["Password"].ToString() : "";
            if (localSettings.Values["Login"] != null && localSettings.Values["Password"] != null)
                checkRemember.IsChecked = true;

            DataContext = UserInsta;
        }

        public User UserInsta { get; set; } = new User();

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            _ = LoginInsta();
        }

        async Task LoginInsta()
        {
            if (checkRemember.IsChecked.Value)
            {
                ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["Login"] = UserInsta.LoginUser;
                localSettings.Values["Password"] = UserInsta.PasswordUser;
            }
            else
            {
                ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["Login"] = "";
                localSettings.Values["Password"] = "";
            }

            IsEnabled = false;
            modalRing.Visibility = Visibility.Visible;

            await InstaServer.LoginInstagram(UserInsta, this);
            await Task.Delay(1000);

            modalRing.Visibility = Visibility.Collapsed;
            IsEnabled = true;
        }

        private void Grid_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                _ = LoginInsta();
        }
    }
}
