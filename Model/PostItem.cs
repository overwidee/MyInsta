using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace MyInsta.Model
{
    public class PostItem
    {
        public int Id { get; set; }
        public string UserNamePost { get; set; }
        public long UserPk { get; set; }
        public string UserPicture { get; set; }
        public ObservableCollection<CustomMedia> Items { get; set; }
        public Visibility IsVisible { get; set; } = Visibility.Visible;
    }
}
