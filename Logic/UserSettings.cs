using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json;
using Windows.UI.Xaml;

namespace MyInsta.Logic
{
    public class UserSettings
    {
        public delegate void UpdateSettings();

        public static event UpdateSettings OnPaneModeChanged;
        public static event UpdateSettings OnThemeChanged;
        private static IEnumerable<DownloadPath> DefaultPaths { get; set; } = new ObservableCollection<DownloadPath>();

        public static bool IsMenuOpen
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                return localSettings.Values["IsMenuOpen"] == null || (bool)localSettings.Values["IsMenuOpen"];
            }
            set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["IsMenuOpen"] = value;
            }
        }

        public static string Theme
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                return localSettings.Values["Theme"] != null
                    ? localSettings.Values["Theme"].ToString()
                    : Application.Current.RequestedTheme.ToString();
            }
            set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["Theme"] = value;
                OnThemeChanged?.Invoke();
            }
        }

        public static NavigationViewPaneDisplayMode PaneMode
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                return localSettings.Values["PaneMode"] != null
                    ? (NavigationViewPaneDisplayMode)Enum.Parse(typeof(NavigationViewPaneDisplayMode),
                        localSettings.Values["PaneMode"].ToString())
                    : NavigationViewPaneDisplayMode.Left;
            }
            set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["PaneMode"] = (byte)value;
                OnPaneModeChanged?.Invoke();
            }
        }

        public static void AddPath(StorageFolder folder)
        {
            string token = StorageApplicationPermissions.FutureAccessList.Add(folder, folder.Path);
            (DefaultPaths as ObservableCollection<DownloadPath>).Add(new DownloadPath
            {
                Path = folder.Path,
                AssessToken = token
            });

            SavePaths();
        }

        public static void RemovePath(string path)
        {
            var pathModel = DefaultPaths.FirstOrDefault(x => x.Path == path);
            if (pathModel != null)
            {
                StorageApplicationPermissions.FutureAccessList.Remove(pathModel.AssessToken);
                (DefaultPaths as ObservableCollection<DownloadPath>)?.Remove(pathModel);
            }

            SavePaths();
        }

        private static void SavePaths()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["DefaultPaths"] = JsonConvert.SerializeObject(DefaultPaths);
        }

        public static IEnumerable<DownloadPath> GetDefaultPaths()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values["DefaultPaths"] != null)
            {
                DefaultPaths = JsonConvert.DeserializeObject<ObservableCollection<DownloadPath>>(localSettings
                    .Values["DefaultPaths"]
                    .ToString());
            }
            
            return DefaultPaths;
        }
    }

    public class DownloadPath
    {
        public string AssessToken { get; set; }
        public string Path { get; set; }
    }
}
