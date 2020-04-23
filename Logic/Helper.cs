using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyInsta.Model;

namespace MyInsta.Logic
{
    static class Helper
    {
        public static IEnumerable<int> ReturnNumbers(string str)
        {
            var arr = str.Split(',');
            foreach (string item in arr)
            {
                if (item.Contains("-"))
                {
                    var m = item.Split('-');
                    if (int.TryParse(m[0], out int k) && int.TryParse(m[m.Length - 1], out int j))
                    {
                        if (Math.Abs(k - j) > 50)
                        {
                            yield return 0;
                        }
                        for (int i = k; i <= j; i++)
                        {
                            yield return i;
                        }
                    }
                }
                else
                {
                    yield return int.TryParse(item, out int t) ? int.Parse(item) : 0;
                }
            }
        }

        public static ObservableCollection<CustomMedia> ConvertToCustomMedia(IEnumerable<PostItem> posts)
        {
            var medias = new ObservableCollection<CustomMedia>();

            foreach (var post in posts)
            {
                foreach (var media in post.Items)
                {
                    medias.Add(media);
                }
            }

            return medias;
        }
    }
}
