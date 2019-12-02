using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyInsta.Logic
{
    static class Helper
    {
        public static IEnumerable<int> ReturnNumbers(string str)
        {
            var arr = str.Split(',');
            foreach (var item in arr)
            {
                if (item.Contains("-"))
                {
                    var m = item.Split('-');
                    for (int i = int.Parse(m[0]); i <= int.Parse(m[1]); i++)
                    {
                        yield return i;
                    }
                }
                else
                {
                    yield return int.TryParse(item, out int t) ? int.Parse(item) : 0;
                }
            }
        }
    }
}
