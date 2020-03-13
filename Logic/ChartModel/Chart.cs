using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyInsta.Model;

namespace MyInsta.Logic.ChartModel
{
    public class ChartModel<T>
    {
        public T Id { get; set; }
        public int Value { get; set; }
        public string UrlImage { get; set; }
    }
    public static class Chart
    {
        public static IEnumerable<ChartModel<DateTime>> GetChartLikes(IEnumerable<PostItem> posts)
        {
            return posts.Select(post => new ChartModel<DateTime>()
            {
                Id = post.Items[0].Date,
                Value = post.Items[0].CountLikes,
                UrlImage = post.Items[0].UrlBigImage
            });
        }

        public static int GetMax(IEnumerable<PostItem> posts, Func<PostItem, int> func)
        {
            return posts.Max(func);
        }

        public static int GetMin(IEnumerable<PostItem> posts, Func<PostItem, int> func)
        {
            return posts.Min(func);
        }

        public static double GetAverage(IEnumerable<PostItem> posts, Func<PostItem, int> func)
        {
            return posts.Average(func);
        }

        public static IEnumerable<ChartModel<DateTime>> GetChartComments(IEnumerable<PostItem> posts)
        {
            return posts.Select(post => new ChartModel<DateTime>()
            {
                Id = post.Items[0].Date,
                Value = post.Items[0].CountComments,
                UrlImage = post.Items[0].UrlBigImage
            });
        }
    }
}
