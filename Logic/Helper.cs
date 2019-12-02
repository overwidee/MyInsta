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
                    if (int.TryParse(m[0], out int k) && int.TryParse(m[m.Length - 1], out int j))
                    {
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
    }
}
