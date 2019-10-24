using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyInsta.Model
{
    public class PostItem : IPost
    {
        public int Id { get; set; }
        public string UserNamePost { get; set; }
        public long UserPk { get; set; }
        public string UserPicture { get; set; }
        public ObservableCollection<CustomMedia> Items { get; set; }
    }

    public class SavedItem : IPost
    {
        public int Id { get; set; }
        public string UserNamePost { get; set; }
        public long UserPk { get; set; }
        public string UserPicture { get; set; }
        public ObservableCollection<CustomMedia> Items { get; set; }
        public long CollectionId { get; set; }
    }

    interface IPost
    {
        int Id { get; set; }
        string UserNamePost { get; set; }
        long UserPk { get; set; }
        string UserPicture { get; set; }
        ObservableCollection<CustomMedia> Items { get; set; }
    }
}
