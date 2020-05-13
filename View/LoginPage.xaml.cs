using MyInsta.Logic;
using MyInsta.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using UserSettings = MyInsta.Logic.UserSettings;

namespace MyInsta.View
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();

            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.BackgroundColor = Resources["SystemControlAcrylicElementMediumHighBrush"] as Color?;
            CoreApplicationViewTitleBar bar = CoreApplication.GetCurrentView().TitleBar;
            bar.ExtendViewIntoTitleBar = true;

            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            UserInsta.LoginUser = localSettings.Values["Login"] != null ? localSettings.Values["Login"].ToString() : null;
            UserInsta.PasswordUser = localSettings.Values["Password"] != null ? localSettings.Values["Password"].ToString() : null;

            DataContext = UserInsta;
        }

        public User UserInsta { get; set; } = new User();

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            _ = LoginInsta();
            UserSettings.PaneMode = NavigationViewPaneDisplayMode.Left;
        }

        async Task LoginInsta()
        {
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
            {
                _ = LoginInsta();
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await InstaServer.LoginInstagram(UserInsta, this);
        }
    }
}
