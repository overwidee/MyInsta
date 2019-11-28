using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyInsta.Model
{
    public class CustomMedia
    {
        public string Pk { get; set; }
        public string Name { get; set; }
        public string UrlSmallImage { get; set; }
        public string UrlBigImage { get; set; }
        public int CountLikes { get; set; }
        public int CountComments { get; set; }
        public bool Liked { get; set; }
        public MediaType MediaType { get; set; }
        public string UrlVideo { get; set; }
        public DateTime Date { get; set; }
        public string DatePost => $"{Date.ToLongDateString()} {Date.ToLongTimeString()}";
    }


    public enum MediaType
    {
        Image,
        Video
    }
}
