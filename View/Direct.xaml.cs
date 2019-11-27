using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using InstagramApiSharp.Classes.Models;
using MyInsta.Logic;
using MyInsta.Model;
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
    public sealed partial class Direct : Page
    {
        public Direct()
        {
            this.InitializeComponent();
        }

        public User InstaUser { get; set; }

        public ObservableCollection<InstaDirectInboxThread> DirectItems { get; set; } 
        public ObservableCollection<InstaDirectInboxItem> AudioAttachment { get; set; }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            InstaUser = e.Parameter as User;

            DirectItems = await InstaServer.GetDirectDialogsAsync(InstaUser);
            listItems.ItemsSource = DirectItems.Where(x => !string.IsNullOrEmpty(x.Title));
        }

        private async void ListItems_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
        {
            var selectD = ((ListView)(sender)).SelectedItem as InstaDirectInboxThread;
            AudioAttachment = await InstaServer.GetDialogAudioAsync(InstaUser, selectD.ThreadId);

            listDialog.ItemsSource = AudioAttachment;
        }
    }
}
