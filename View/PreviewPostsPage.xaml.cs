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
        public ObservableCollection<PreviewPost> PreviewPosts { get; set; }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            InstaUser = e.Parameter as User;
            PreviewPosts = await InstaServer.GetPreviewPosts(InstaUser);
            grid.ItemsSource = PreviewPosts;
        }

    }
}
