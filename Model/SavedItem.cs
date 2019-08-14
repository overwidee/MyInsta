using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyInsta.Model
{
    public class SavedItem
    {
        public int Id { get; set; }
        public string UserNamePost { get; set; }
        public long UserPk { get; set; }
        public string UserPicture { get; set; }
        public CustomMedia Item { get; set; }
    }
}
