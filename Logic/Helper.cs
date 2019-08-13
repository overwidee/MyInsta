using InstagramApiSharp.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MyInsta.Logic
{
    public class Helper
    {
        public const string AppName = "MyInsta";
        public static readonly string LocalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
        public static IInstaApi InstaApi { get; set; }
        public static Settings Settings { get; set; }

        public static void CreateNewInstaApi(string username, string password)
        {
        }

        public static bool CheckIPAddress(string ip)
        {
            return IPAddress.TryParse(ip, out IPAddress ipadd);
        }

        public static bool CheckPortAddress(string port)
        {
            return int.TryParse(port, out int p);
        }
        public static void CreateDirectory()
        {
            if (!Directory.Exists(LocalPath))
                Directory.CreateDirectory(LocalPath);

            if (!Directory.Exists(TempPath))
                Directory.CreateDirectory(TempPath);

            if (!Directory.Exists(CacheTempPath))
                Directory.CreateDirectory(CacheTempPath);
        }

        public static string RandomString(int length = 12)
        {
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            var chars = Enumerable.Range(0, length)
                .Select(x => pool[Random.Next(0, pool.Length)]);
            return new string(chars.ToArray());
        }
        static Random Random = new Random();
        public readonly static string TempPath = Path.Combine(Path.GetTempPath(), AppName);
        public readonly static string CacheTempPath = Path.Combine(TempPath, "Cached");
    }
}
