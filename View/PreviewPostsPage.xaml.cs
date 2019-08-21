using MyInsta.Logic;
using MyInsta.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed partial class PreviewPostsPage : Page
    {
        public PreviewPostsPage()
        {
            this.InitializeComponent();
        }
        public User InstaUser { get; set; }
        public ObservableCollection<string> TempUrl { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<PreviewPost> PreviewPosts { get; set; }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            InstaUser = e.Parameter as User;
            PreviewPosts = await InstaServer.GetPreviewPosts(InstaUser);
            mainGrid.ItemsSource = PreviewPosts;
        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainGrid.SelectedItem != null)
            {
                if (TempUrl.Contains((mainGrid.SelectedItem as PreviewPost).Url))
                {
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    StorageFile file = await localFolder.GetFileAsync((mainGrid.SelectedItem as PreviewPost).Name);
                    await file.DeleteAsync();
                }
                PreviewPosts.Remove(mainGrid.SelectedItem as PreviewPost);
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var name = file.Name + DateTime.Now.Millisecond.ToString();
                await file.CopyAsync(ApplicationData.Current.LocalFolder, name);

                PreviewPosts.Insert(0, new PreviewPost()
                {
                    Id = PreviewPosts.Last().Id + 1,
                    Url = ApplicationData.Current.LocalFolder.Path + @"\" + name,
                    Name = name
                });
                TempUrl.Add(ApplicationData.Current.LocalFolder.Path + @"\" + name);
                mainGrid.ItemsSource = PreviewPosts;
            }
        }
    }
}
