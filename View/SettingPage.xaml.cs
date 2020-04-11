using MyInsta.Logic;
using MyInsta.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
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
    public sealed partial class SettingPage : Page
    {
        public SettingPage()
        {
            InitializeComponent();
        }

        public User InstaUser { get; set; }
        public double StoryWidth { get; set; }
        public double StoryHeight { get; set; }
        public double PostHeight { get; set; }
        public double PostWidth { get; set; }

        public NavigationViewPaneDisplayMode SelectedMode
        {
            get => UserSettings.PaneMode;
            set
            {
                if (value != UserSettings.PaneMode)
                {
                    UserSettings.PaneMode = value;
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var objs = e.Parameter as User;
            InstaUser = objs;

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            StoryHeight = (double?)localSettings.Values["StoryHeight"] ?? 550;
            StoryWidth = (double?)localSettings.Values["StoryWidth"] ?? 350;
            PostWidth = (double?)localSettings.Values["PostWidth"] ?? 500;
            PostHeight = (double?)localSettings.Values["PostHeight"] ?? 500;

            ListViewPaths.ItemsSource = UserSettings.GetDefaultPaths();
            var paneModes = Enum.GetValues(typeof(NavigationViewPaneDisplayMode)).Cast<NavigationViewPaneDisplayMode>();
            ComboBoxPaneMode.ItemsSource = paneModes;

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).SelectedItem.ToString() == "English")
            {
                ApplicationLanguages.PrimaryLanguageOverride = "en-US";
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["Language"] = "en-US";
            }
            else if (((ComboBox)sender).SelectedItem.ToString() == "Русский")
            {
                ApplicationLanguages.PrimaryLanguageOverride = "ru-RU";
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["Language"] = "ru-RU";
            }
            RestartApp();
        }

        private async void ButtonLogout_Click(object sender, RoutedEventArgs e)
        {
            bool log = await InstaServer.RemoveConnection(InstaUser.Api);
            if (log)
            {
                RestartApp();
            }
        }

        private async void RestartApp()
        {
            var result = await CoreApplication.RequestRestartAsync("Application Restart Programmatically ");

            if (result == AppRestartFailureReason.NotInForeground ||
                result == AppRestartFailureReason.RestartPending ||
                result == AppRestartFailureReason.Other)
            {
                var msgBox = new MessageDialog("Restart Failed", result.ToString());
                await msgBox.ShowAsync();
            }
        }

        private void SettingPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["StoryHeight"] = StoryHeight;
            localSettings.Values["StoryWidth"] = StoryWidth;
            localSettings.Values["PostWidth"] = PostWidth;
            localSettings.Values["PostHeight"] = PostHeight;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                UserSettings.AddPath(folder);
            }
        }

        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            UserSettings.RemovePath(((Button)sender).Tag.ToString());
        }

        private void ComboBoxPaneMode_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //UserSettings.PaneMode = (NavigationViewPaneDisplayMode)ComboBoxPaneMode.SelectedItem;
        }
    }
}
