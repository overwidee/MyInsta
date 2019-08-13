using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MyInsta.Logic
{
    public class SessionHelper
    {
        public static readonly string AccountPath = Path.Combine(Helper.LocalPath, "Accounts");
        static readonly string SessionPath = Path.Combine(Helper.LocalPath, "session.dat");
        public static void CreateInstaDeskDirectory()
        {
            if (!Directory.Exists(Helper.LocalPath))
                Directory.CreateDirectory(Helper.LocalPath);
        }
        public static async Task<bool> LoadAndLogin(IInstaApi InstaApi, string userLogin, string password)
        {
            if (!File.Exists(SessionPath))
                return false;
            try
            {
                var userSession = new UserSessionData
                {
                    UserName = userLogin,
                    Password = password
                };
                if (Helper.Settings != null && Helper.Settings.UseProxy && !string.IsNullOrEmpty(Helper.Settings.ProxyIP) && !string.IsNullOrEmpty(Helper.Settings.ProxyPort))
                {
                    var proxy = new WebProxy()
                    {
                        Address = new Uri($"http://{Helper.Settings.ProxyIP}:{Helper.Settings.ProxyPort}"),
                        BypassProxyOnLocal = false,
                        UseDefaultCredentials = false,
                    };
                    var httpClientHandler = new HttpClientHandler()
                    {
                        Proxy = proxy,
                    };
                    InstaApi = InstaApiBuilder.CreateBuilder()
                        .SetUser(userSession)
                        .UseLogger(new DebugLogger(LogLevel.Exceptions))
                        .UseHttpClientHandler(httpClientHandler)
                        .Build();
                }
                else
                {
                    InstaApi = InstaApiBuilder.CreateBuilder()
                        .SetUser(userSession)
                        .UseLogger(new DebugLogger(LogLevel.Exceptions))
                        .Build();
                }
                var text = LoadSession();
                InstaApi.LoadStateDataFromString(text);
                if (!InstaApi.IsUserAuthenticated)
                    await InstaApi.LoginAsync();
                return true;
            }
            catch (Exception ex)
            {

            }
            return false;
        }
        public static void DeleteCurrentSession()
        {
            if (!File.Exists(SessionPath))
                return;
            try
            {
                File.Delete(SessionPath);
            }
            catch { }
        }
        public static void SaveCurrentSession(IInstaApi InstaApi)
        {
            if (InstaApi == null)
                return;
            if (InstaApi.IsUserAuthenticated)
                return;
            try
            {
                var state = InstaApi.GetStateDataAsString();
                File.WriteAllText(SessionPath, state);
            }
            catch (Exception ex) {  }
        }

        public static string LoadSession()
        {
            try
            {
                var text = File.ReadAllText(SessionPath);

                return text;
            }
            catch (Exception ex) {  }
            return null;
        }
    }
}
