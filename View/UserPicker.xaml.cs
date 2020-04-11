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
using MyInsta.Logic;
using WinRTXamlToolkit.Tools;

namespace MyInsta.View
{
    public sealed partial class UserPicker : ContentDialog
    {
        public UserPicker()
        {
            InitializeComponent();

            var paths = new ObservableCollection<DownloadPath>();
            var defaults = UserSettings.GetDefaultPaths();

            defaults.ForEach(x => paths.Add(new DownloadPath() { Path = x.Path, AssessToken = null }));
            paths.Add(new DownloadPath() { Path = "Custom" });
            ListViewPaths.ItemsSource = paths;
        }

        public string Path => (ListViewPaths.SelectedValue as DownloadPath)?.Path;
    }
}
