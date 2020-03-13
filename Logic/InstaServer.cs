using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Logger;
using MyInsta.Model;
using MyInsta.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinRTXamlToolkit.Tools;
using static Windows.Networking.Connectivity.NetworkInformation;
using ReturnPersonPage = MyInsta.View.ReturnPersonPage;

namespace MyInsta.Logic
{
    public enum DataType
    {
        Followers,
        Following,
        Likers
    }
    public static class InstaServer
    {
        public static int UtcValue = 0;
        public static string LatestMediaMaxId = string.Empty;
        private static CancellationTokenSource cancellationTokenMedia;

        public delegate void CompleteHandler();
        public delegate void UpdateUserCheck(int pk);

        #region Events

        public static event CompleteHandler OnUserFollowersLoaded;
        public static event CompleteHandler OnUserStoriesLoaded;
        public static event CompleteHandler OnUserSavedPostsLoaded;
        public static event CompleteHandler OnUserSavedPostsAllLoaded;
        public static event CompleteHandler OnUserUnfollowersLoaded;
        public static event CompleteHandler OnUserFriendsLoaded;
        public static event CompleteHandler OnUserPostsLoaded;
        public static event CompleteHandler OnUserCollectionLoaded;
        public static event CompleteHandler OnUserAllPostsLoaded;
        public static event CompleteHandler OnUserExploreFeedLoaded;
        public static event CompleteHandler OnCommonDataLoaded;
        public static event CompleteHandler OnUsersFeedLoaded;
        public static event CompleteHandler OnFeedLoaded;
        public static event CompleteHandler UpdateCountFeed;
        public static event UpdateUserCheck OnUserFeedLoaded;
        public static event CompleteHandler OnUserInfoLoaded;
        public static event CompleteHandler OnUserArchivePostsLoaded;

        public static int CountFeed { get; set; } = 0;

        #endregion

        #region Internet
        public static bool IsInternetConnected()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }


        #endregion

        #region Progress

        public static bool IsFollowersLoaded { get; set; }
        public static bool IsSavedPostsAllLoaded { get; set; }
        public static bool IsStoriesLoaded { get; set; }
        public static bool IsSavedPostsLoaded { get; set; }
        public static bool IsFriendsLoaded { get; set; }
        public static bool IsUnfollowersLoaded { get; set; }
        public static bool IsPostsLoaded { get; set; }
        public static bool IsFeedLoading { get; set; }

        #endregion  

        #region Login instagram and API

        public static async Task LoginInstagram(User userObject, LoginPage page)
        {
            if (ExistsConnection())
            {
                string savedAPI = await GetSavedApi();

                var api = InstaApiBuilder.CreateBuilder().SetUser(new UserSessionData()
                {
                    UserName = userObject.LoginUser,
                    Password = userObject.PasswordUser
                }).UseLogger(new DebugLogger(LogLevel.Exceptions)).Build();
                api.LoadStateDataFromString(savedAPI.ToString());

                userObject.API = api;
                page.Frame.Navigate(typeof(MenuPage), userObject);
            }
            else
            {
                if (userObject != null && userObject.LoginUser != null && userObject.PasswordUser != null)
                {
                    var api = InstaApiBuilder.CreateBuilder().SetUser(new UserSessionData()
                    {
                        UserName = userObject.LoginUser,
                        Password = userObject.PasswordUser
                    }).UseLogger(new DebugLogger(LogLevel.Exceptions)).Build();
                    userObject.API = api;

                    if (!userObject.API.IsUserAuthenticated)
                    {
                        await userObject.API.SendRequestsBeforeLoginAsync();
                        await Task.Delay(5000);

                        var logResult = await userObject.API.LoginAsync();
                        if (logResult.Succeeded)
                        {
                            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                            localSettings.Values["Login"] = userObject.LoginUser;
                            localSettings.Values["Password"] = userObject.PasswordUser;

                            await userObject.API.SendRequestsAfterLoginAsync();
                            SaveSession(userObject.API);

                            await SaveApiString(userObject.API);
                            page.Frame.Navigate(typeof(MenuPage), userObject);

                        }
                        else
                        {

                            switch (logResult.Value)
                            {
                                case InstaLoginResult.InvalidUser:
                                    _ = new CustomDialog("Warning", logResult.Info.Message, "Ok");
                                    break;
                                case InstaLoginResult.Success:
                                    page.Frame.Navigate(typeof(MenuPage), userObject);
                                    break;
                                case InstaLoginResult.Exception:
                                    _ = new CustomDialog("Warning", logResult.Info.Message, "Ok");
                                    break;
                                case InstaLoginResult.BadPassword:
                                    _ = new CustomDialog("Warning", "Bad password", "Ok");
                                    break;
                                case InstaLoginResult.ChallengeRequired:
                                    if (logResult.Value == InstaLoginResult.ChallengeRequired)
                                    {
                                        var challenge = await userObject.API.GetChallengeRequireVerifyMethodAsync();
                                        if (challenge.Succeeded)
                                        {
                                            if (challenge.Value.SubmitPhoneRequired)
                                            {
                                                var submitPhone = await userObject.API.SubmitPhoneNumberForChallengeRequireAsync("+375333085326");
                                                if (submitPhone.Succeeded)
                                                {
                                                }
                                            }
                                            else
                                            {
                                                page.Frame.Navigate(typeof(VerifyPage), userObject);

                                            }
                                        }
                                    }
                                    break;
                                case InstaLoginResult.TwoFactorRequired:
                                    userObject.API = await LoginByTwoFactor(userObject);
                                    if (userObject.API != null)
                                    {
                                        page.Frame.Navigate(typeof(MenuPage), userObject);
                                    }
                                    break;
                                case InstaLoginResult.CheckpointLoggedOut:
                                    break;
                                default:
                                    _ = new CustomDialog("Warning", logResult.Info.Message, "Ok");
                                    break;
                            }
                        }
                    }
                }
            }
        }

        static void LoadSession(IInstaApi InstaApi)
        {
            InstaApi?.SessionHandler?.Load();

            //// Old load session
            //try
            //{
            //    if (File.Exists(StateFile))
            //    {
            //        Debug.WriteLine("Loading state from file");
            //        using (var fs = File.OpenRead(StateFile))
            //        {
            //            InstaApi.LoadStateDataFromStream(fs);
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine(ex);
            //}
        }

        static void SaveSession(IInstaApi InstaApi)
        {
            if (InstaApi == null)
            {
                return;
            }

            if (!InstaApi.IsUserAuthenticated)
            {
                return;
            }

            InstaApi.SessionHandler.Save();
        }
        private static bool ExistsConnection()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            if (File.Exists(localFolder.Path + @"\dataFile.txt"))
                return true;
            else
                return false;
        }
        private static async Task<string> GetSavedApi()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile sampleFile = await localFolder.GetFileAsync("dataFile.txt");
            return await FileIO.ReadTextAsync(sampleFile);
        }

        public static async Task<bool> SendSMSVerify(IInstaApi api)
        {

            var result = await api.RequestVerifyCodeToSMSForChallengeRequireAsync();
            if (result.Succeeded)
            {
                _ = new CustomDialog("Message", $"Code sent on phone {result.Value.StepData.PhoneNumberPreview}",
                    "All right");
            }

            return result.Succeeded;
        }

        public static async Task<bool> SendEmailVerify(IInstaApi api)
        {
            var challenge = await api.GetChallengeRequireVerifyMethodAsync();
            if (challenge.Value.StepData != null && challenge.Value.StepData.Email != null)
            {
                var result = await api.RequestVerifyCodeToEmailForChallengeRequireAsync();
                if (result.Succeeded)
                    _ = new CustomDialog("Message", $"Code sent on email {challenge.Value.StepData.Email}",
                        "All right");
                return result.Succeeded;
            }
            return false;
        }

        public static async Task<IInstaApi> LoginByCode(User user, string code)
        {
            var result = await user.API.VerifyCodeForChallengeRequireAsync(code);
            if (!result.Succeeded)
            {
                if (result.Value == InstaLoginResult.TwoFactorRequired)
                {
                    // TwoFactorRequired
                    //await user.API.SendTwoFactorLoginSMSAsync();
                    var dialog = new InputDialog("Two factor required code:", "Send");
                    if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                    {
                        var res = await user.API.TwoFactorLoginAsync(dialog.ResultText);
                        if (res.Succeeded)
                        {
                            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                            localSettings.Values["Login"] = user.LoginUser;
                            localSettings.Values["Password"] = user.PasswordUser;
                            await SaveApiString(user.API);
                            return user.API;
                        }
                    }
                }
            }
            else
            {
                ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["Login"] = user.LoginUser;
                localSettings.Values["Password"] = user.PasswordUser;

                await SaveApiString(user.API);
                return user.API;
            }

            return null;
        }

        public static async Task<IInstaApi> LoginByTwoFactor(User user)
        {
            await user.API.SendTwoFactorLoginSMSAsync();
            var dialog = new InputDialog("Two factor required code:", "Send");
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var res = await user.API.TwoFactorLoginAsync(dialog.ResultText);
                if (res.Succeeded)
                {
                    ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    localSettings.Values["Login"] = user.LoginUser;
                    localSettings.Values["Password"] = user.PasswordUser;
                    await SaveApiString(user.API);
                    return user.API;
                }
            }

            return null;
        }

        public static async Task SaveApiString(IInstaApi api)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile sampleFile = await localFolder.CreateFileAsync("dataFile.txt",
                CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(sampleFile, api.GetStateDataAsString());
        }

        public static async Task<bool> RemoveConnection(IInstaApi api)
        {
            var x = await api.LogoutAsync();
            if (ExistsConnection())
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.GetFileAsync("dataFile.txt");
                await file.DeleteAsync();

                ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["Login"] = null;
                localSettings.Values["Password"] = null;
                return true;
            }
            else
                return false;
        }

        #endregion

        #region Main data

        public static async Task GetUserData(User userObject, bool isSync = false)
        {
            if (!IsInternetConnected())
            {
                userObject.UserData = new UserData();
                _ = new CustomDialog("Warning", "Chech your internet connection", "Ok");
                return;
            }

            userObject.UserData.UserFollowers = new ObservableCollection<InstaUserShort>();
            userObject.UserData.SavedPostItems = new ObservableCollection<PostItem>();

            var user = await userObject.API.UserProcessor
                .GetUserInfoByUsernameAsync(userObject.LoginUser);
            userObject.UserData.Pk = user.Value.Pk;
            userObject.UserData.UrlPicture = user.Value.HdProfilePicVersions[0].Uri;

            if (!isSync)
            {
                var listTasks = new List<Task>()
                {
                    GetBookmarksAsync(userObject),
                    GetUserFollowers(userObject),
                    GetUserFriendsAndUnfollowers(userObject, true)
                };
                await Task.WhenAll(listTasks);
            }
            else
            {
                IsSavedPostsLoaded = false;
                IsSavedPostsAllLoaded = false;
                var listTasks = new List<Task>()
                {
                    GetUserFollowers(userObject),
                    GetUserFriendsAndUnfollowers(userObject, true)
                };
                await Task.WhenAll(listTasks);
            }
        }
        private static async Task GetUserFollowers(User user)
        {
            var f = await user.API.UserProcessor.GetUserFollowersAsync(user.LoginUser,
                PaginationParameters.Empty);
            foreach (var item in f.Value)
            {
                if (!user.UserData.UserFollowers.Contains(item))
                    user.UserData.UserFollowers.Add(item);
            }
            OnUserFollowersLoaded?.Invoke();
            IsFollowersLoaded = true;
        }

        public static string UserSavedMediaMaxId;
        private static bool IsSavedMediasLoading = false;
        private static PaginationParameters paginationParameters = PaginationParameters.MaxPagesToLoad(2);
        public static async Task GetUserPostItems(User user, long collectionId = 0, bool isRefresh = false)
        {
            if (!IsSavedMediasLoading)
            {
                if (isRefresh)
                {
                    paginationParameters = paginationParameters.StartFromMaxId(UserSavedMediaMaxId);
                }

                IsSavedMediasLoading = true;
                if (collectionId != 0)
                {
                    paginationParameters = PaginationParameters.MaxPagesToLoad(3);
                    var items = await user.API.CollectionProcessor.GetSingleCollectionAsync(collectionId,
                        paginationParameters.StartFromMinId(UserSavedMediaMaxId));
                    UserSavedMediaMaxId = paginationParameters.NextMaxId;

                    int i = user.UserData.SavedPostItems.Count != 0 ? user.UserData.SavedPostItems.Last().Id + 1 : 1;
                    foreach (var post in items.Value.Media)
                    {
                        user.UserData.SavedPostItems.Add(GetPostItem(post, i));
                        i++;
                    }
                    OnUserSavedPostsLoaded?.Invoke();
                }
                else
                {
                    paginationParameters = PaginationParameters.MaxPagesToLoad(2);
                    var items = await user.API.FeedProcessor.GetSavedFeedAsync(paginationParameters.StartFromMaxId(UserSavedMediaMaxId));
                    UserSavedMediaMaxId = paginationParameters.NextMaxId;

                    int i = user.UserData.SavedPostItems.Count != 0 ? user.UserData.SavedPostItems.Last().Id + 1 : 1;
                    foreach (var post in items.Value)
                    {
                        user.UserData.SavedPostItems.Add(GetPostItem(post, i));
                        i++;
                    }
                    OnUserSavedPostsLoaded?.Invoke();
                }

                IsSavedMediasLoading = false;
            }
        }
        private static async Task GetUserFriendsAndUnfollowers(User user, bool all = false, int count = 30)
        {
            var fling = await user.API.UserProcessor.GetUserFollowingAsync(user.LoginUser,
                PaginationParameters.Empty);
            if (all)
            {
                count = fling.Value.Count;
            }

            var tasks = new List<Task>();
            foreach (var item in fling.Value.Take(count))
            {
                if (!user.UserData.UserFollowing.Contains(item))
                {
                    user.UserData.UserFollowing.Add(item);
                }
                tasks.Add(AddPerson(user, item));
            }
            await Task.WhenAll(tasks);

            if (all)
            {
                OnUserUnfollowersLoaded?.Invoke();
                OnUserFriendsLoaded?.Invoke();
                IsUnfollowersLoaded = true;
                IsFriendsLoaded = true;
            }
        }

        private static async Task AddPerson(User user, InstaUserShort item)
        {
            var statusR = await user.API.UserProcessor.GetFriendshipStatusAsync(item.Pk);
            var status = statusR.Value;
            if (status.Following && status.FollowedBy && user.UserData.UserFriends.IndexOf(item) == -1)
            {
                user.UserData.UserFriends.Add(item);
            }
            else if (status.Following && !status.FollowedBy && user.UserData.UserUnfollowers.IndexOf(item) == -1)
            {
                user.UserData.UserUnfollowers.Add(item);
            }
        }
        public static async Task<InstaFullUserInfo> GetCurrentUserInfo(User user)
        {
            var info = await user.API.UserProcessor.GetFullUserInfoAsync(user.UserData.Pk);
            return info.Value;
        }

        #endregion

        #region UserProcessor

        public static async Task UnFollowFromList(User currentUser, ObservableCollection<InstaUserShort> instaUsers)
        {
            _ = new CustomDialog("Message", "Process started", "All right");
            foreach (var item in instaUsers.Take(30))
            {
                await currentUser.API.UserProcessor.UnFollowUserAsync(item.Pk);
            }
            await GetUserData(currentUser);
            _ = new CustomDialog("Message", "Un followed from 30 unfollowers", "All right");
        }

        public static async Task<InstaUserInfo> GetInfoUser(User userObject, string nick)
        {
            if (userObject != null)
            {
                IResult<InstaUserInfo> userInfo = await userObject.API.UserProcessor.GetUserInfoByUsernameAsync(nick);
                userInfo.Value.FriendshipStatus = await GetFriendshipStatus(userObject, userInfo.Value);

                return userInfo.Value;
            }

            return null;
        }

        public static async Task UnfollowUser(User userObject, InstaUserShort unfUser)
        {
            try
            {
                if (userObject != null && unfUser != null)
                {
                    var unF = await userObject.API.UserProcessor.UnFollowUserAsync(unfUser.Pk);
                    if (unF.Succeeded)
                    {
                        var person = await GetInstaUserShortById(userObject, unfUser.Pk);
                        if (userObject.UserData.UserUnfollowers.Contains(person))
                        {
                            userObject.UserData.UserUnfollowers.Remove(person);
                        }
                        if (userObject.UserData.UserFriends.Contains(person))
                        {
                            userObject.UserData.UserFriends.Remove(person);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static async Task FollowUser(User userObject, InstaUserInfo unfUser)
        {
            if (userObject != null && unfUser != null)
            {
                var unF = await userObject.API.UserProcessor.FollowUserAsync(unfUser.Pk);
                if (unF.Succeeded)
                {
                    var person = await GetInstaUserShortById(userObject, unfUser.Pk);
                    if (unF.Value.Following && unF.Value.FollowedBy
                        && !userObject.UserData.UserFriends.Contains(person))
                    {
                        userObject.UserData.UserFriends.Add(person);
                    }
                    else if (unF.Value.Following && !unF.Value.FollowedBy && !userObject.UserData.UserUnfollowers.Contains(person))
                    {
                        userObject.UserData.UserUnfollowers.Add(person);
                    }
                }
            }
        }

        public static async Task<InstaStoryFriendshipStatus> GetFriendshipStatus(User userObject, InstaUserInfo unfUser)
        {
            if (userObject == null || unfUser == null)
            {
                return null;
            }

            var status = await userObject.API.UserProcessor.GetFriendshipStatusAsync(unfUser.Pk);
            return status.Succeeded ? status.Value : null;
        }

        public static async Task<InstaUserShort> GetInstaUserShortById(User user, long id)
        {
            var userInfo = await user.API.UserProcessor.GetUserInfoByIdAsync(id);
            return new InstaUserShort()
            {
                UserName = userInfo.Value.Username,
                FullName = userInfo.Value.FullName,
                IsPrivate = userInfo.Value.IsPrivate,
                IsVerified = userInfo.Value.IsVerified,
                Pk = userInfo.Value.Pk,
                ProfilePicture = userInfo.Value.ProfilePicUrl,
                ProfilePictureId = userInfo.Value.ProfilePicId,
                ProfilePicUrl = userInfo.Value.HdProfilePicUrlInfo.Uri
            };
        }

        public static async Task<InstaUserShort> GetInstaUserShortByName(User user, string name)
        {
            var userInfo = await user.API.UserProcessor.GetUserInfoByUsernameAsync(name);
            return new InstaUserShort()
            {
                UserName = userInfo.Value.Username,
                FullName = userInfo.Value.FullName,
                IsPrivate = userInfo.Value.IsPrivate,
                IsVerified = userInfo.Value.IsVerified,
                Pk = userInfo.Value.Pk,
                ProfilePicture = userInfo.Value.ProfilePicUrl,
                ProfilePictureId = userInfo.Value.ProfilePicId,
                ProfilePicUrl = userInfo.Value.ProfilePicUrl
            };
        }

        public static IEnumerable<InstaUserShort> SearchByUserName(ObservableCollection<InstaUserShort> collection,
            string srt)
        {
            if (string.IsNullOrEmpty(srt))
            {
                return collection;
            }

            var items = collection.Where(x => x.UserName.Contains(srt));
            return items;
        }

        public static async Task<InstaUserShort> SearchByUserName(User currentUser, string srt)
        {
            if (string.IsNullOrEmpty(srt))
            {
                return null;
            }

            var item = await currentUser.API.UserProcessor.GetUserInfoByUsernameAsync(srt);
            return item.Succeeded ? await GetInstaUserShortById(currentUser, item.Value.Pk) : null;
        }

        #endregion

        #region Media

        public static async Task UnlikeProfile(User currentUser, InstaUserShort selectUser, ObservableCollection<PostItem> medias)
        {
            try
            {
                _ = new CustomDialog("Message", "Process started", "All right");
                if (medias != null)
                {
                    foreach (var item in medias?.Take(30))
                    {
                        await currentUser.API.MediaProcessor.UnLikeMediaAsync(item.Items[0].Pk);
                    }
                }
                _ = new CustomDialog("Message", $"30 posts of {selectUser.UserName} unlike", "All right");
            }
            catch (Exception e)
            {
                _ = new CustomDialog("Message", e.Message, "Ok");
            }
        }

        public static string MediasUserMaxId { get; set; }
        public static CompleteHandler OnDynamicUserMediaLoaded;
        public static bool IsUserMediasLoading = false;
        public static async Task GetDynamicMediaUser(User user, InstaUserInfo unfUser)
        {
            if (!IsUserMediasLoading)
            {
                IsUserMediasLoading = true;
                var response =
                    await user.API.UserProcessor.GetUserMediaAsync(unfUser.Username,
                        PaginationParameters.MaxPagesToLoad(1)
                            .StartFromMaxId(MediasUserMaxId));
                MediasUserMaxId = response.Value?.NextMaxId;
                int i = user.UserData.PostsLastUser.Count != 0 ? user.UserData.PostsLastUser.Last().Id + 1 : 1;

                if (response.Value != null)
                {
                    foreach (var media in response.Value)
                    {
                        var m = user.UserData.PostsLastUser.FirstOrDefault(x => x.Items[0].Pk == media.Pk);
                        if (m == null)
                        {
                            user.UserData.PostsLastUser.Add(GetPostItem(media, i));
                            i++;
                        }
                    }
                }

                OnDynamicUserMediaLoaded?.Invoke();
                IsUserMediasLoading = false;
            }
        }

        public static async Task<ObservableCollection<PostItem>> GetMediaUser(User userObject, InstaUserInfo unfUser, int count)
        {
            if (userObject == null || unfUser == null)
            {
                return null;
            }

            InstaMediaList media;
            if (count == 0)
            {
                var response = await userObject.API.UserProcessor.GetUserMediaAsync(unfUser.Username,
                    PaginationParameters.MaxPagesToLoad(1));
                media = response.Value;
            }
            else
            {
                media = await GetMediaUserAll(userObject, unfUser);
                OnUserAllPostsLoaded?.Invoke();
            }

            OnUserPostsLoaded?.Invoke();
            return GetUrlsMediasUser(media, unfUser);
        }

        private static async Task<InstaMediaList> GetMediaUser(User userObject, InstaUserInfo unfUser)
        {
            var media = await userObject.API.UserProcessor.GetUserMediaAsync(unfUser.Username,
                PaginationParameters.MaxPagesToLoad(1).StartFromMaxId(LatestMediaMaxId));
            LatestMediaMaxId = media.Value.NextMaxId;
            return media.Value;
        }

        public static async Task SaveMediaInProfile(User userObject, string mediaPk)
        {
            _ = await userObject.API.MediaProcessor.SaveMediaAsync(mediaPk);
        }

        private static async Task<InstaMediaList> GetMediaUserAll(User userObject, InstaUserInfo unfUser)
        {
            cancellationTokenMedia = new CancellationTokenSource();
            CancellationToken token = cancellationTokenMedia.Token;
            try
            {
                var instaMedias = new InstaMediaList();
                await Task.Run(async () =>
                    {
                        do
                        {
                            token.ThrowIfCancellationRequested();
                            var medias = await GetMediaUser(userObject, unfUser);
                            instaMedias.AddRange(medias);
                        }
                        while (LatestMediaMaxId != null);
                    }, token);
                return instaMedias;
            }
            catch
            {
                return new InstaMediaList();
            }
        }

        public static PostItem GetPostItem(InstaMedia item, int id)
        {
            var postItem = new PostItem()
            {
                Id = id,
                UserNamePost = item.User.UserName,
                UserPk = item.User.Pk,
                UserPicture = item.User.ProfilePicture,
                Items = new ObservableCollection<CustomMedia>()
            };
            if (item.Images != null && item.Images.Count != 0)
            {
                var post = new CustomMedia()
                {
                    Pk = item.Pk.ToString(),
                    Name = $"{postItem.UserNamePost}_savedPost_{item.Pk}",
                    UrlSmallImage = item.Images[1].Uri,
                    UrlBigImage = item.Images[0].Uri,
                    CountLikes = item.LikesCount,
                    CountComments = item.CommentsCount != null ? int.Parse(item.CommentsCount) : 0,
                    MediaType = MediaType.Image,
                    Liked = item.HasLiked,
                    Date = item.TakenAt.AddHours((DateTime.Now - DateTime.UtcNow).Hours)
                };
                if (item.Videos != null && item.Videos.Count != 0)
                {
                    post.MediaType = MediaType.Video;
                    post.UrlVideo = item.Videos[0].Uri;
                }
                postItem.Items.Add(post);
            }
            else if (item.Carousel != null && item.Carousel.Count != 0)
            {
                int x = 0;
                foreach (var car in item.Carousel)
                {
                    x++;
                    if (car.Images != null && car.Images.Count != 0)
                    {
                        var postCar = new CustomMedia()
                        {
                            Pk = item.Pk.ToString(),
                            Name = $"{postItem.UserNamePost}_savedPost_{item.Pk}_Carousel_{x + 1}",
                            UrlSmallImage = car.Images[1].Uri,
                            UrlBigImage = car.Images[0].Uri,
                            CountLikes = item.LikesCount,
                            CountComments = item.CommentsCount != null ? int.Parse(item.CommentsCount) : 0,
                            MediaType = MediaType.Image,
                            Liked = item.HasLiked,
                            Date = item.TakenAt.AddHours((DateTime.Now - DateTime.UtcNow).Hours)
                        };
                        if (car.Videos != null && car.Videos.Count != 0)
                        {
                            postCar.MediaType = MediaType.Video;
                            postCar.UrlVideo = car.Videos[0].Uri;
                        }
                        postItem.Items.Add(postCar);
                    }
                }
            }
            return postItem;
        }

        public static ObservableCollection<PostItem> GetUrlsMediasUser(InstaMediaList medias,
            InstaUserInfo instaUserInfo = null, InstaUserShort userShort = null)
        {
            if (userShort != null)
            {
                instaUserInfo = new InstaUserInfo()
                {
                    Pk = userShort.Pk,
                    Username = userShort.UserName,
                    ProfilePicUrl = userShort.ProfilePicUrl
                };
            }
            var mediaList = new ObservableCollection<PostItem>();
            var i = 0;

            foreach (var item in medias)
            {
                var postItem = new PostItem()
                {
                    Id = i + 1,
                    UserPk = instaUserInfo.Pk,
                    UserNamePost = instaUserInfo.Username,
                    UserPicture = instaUserInfo.ProfilePicUrl,
                    Items = new ObservableCollection<CustomMedia>()
                };
                if (item.Images != null && item.Images.Count != 0)
                {
                    var post = new CustomMedia()
                    {
                        Pk = item.Pk,
                        Name = $"{postItem.UserNamePost}_imagePost_{item.Pk}",
                        UrlSmallImage = item.Images[1].Uri,
                        UrlBigImage = item.Images[0].Uri,
                        CountLikes = item.LikesCount,
                        CountComments = item.CommentsCount != null ? int.Parse(item.CommentsCount) : 0,
                        MediaType = MediaType.Image,
                        Date = item.TakenAt.AddHours((DateTime.Now - DateTime.UtcNow).Hours),
                        Liked = item.HasLiked
                    };
                    if (item.Videos != null && item.Videos.Count != 0)
                    {
                        post.MediaType = MediaType.Video;
                        post.UrlVideo = item.Videos[0].Uri;
                    }
                    postItem.Items.Add(post);
                    mediaList.Add(postItem);
                }

                else if (item.Carousel != null && item.Carousel.Count != 0)
                {
                    var x = 0;
                    foreach (var car in item.Carousel)
                    {
                        x++;
                        if (car.Images != null && car.Images.Count != 0)
                        {
                            var postCar = new CustomMedia()
                            {
                                Pk = item.Pk,
                                Name = $"{postItem.UserNamePost}_imagePost_{item.Pk}_carousel_{x + 1}",
                                UrlSmallImage = car.Images[1].Uri,
                                UrlBigImage = car.Images[0].Uri,
                                CountLikes = item.LikesCount,
                                CountComments = item.CommentsCount != null ? int.Parse(item.CommentsCount) : 0,
                                MediaType = MediaType.Image,
                                Liked = item.HasLiked,
                                Date = item.TakenAt.AddHours((DateTime.Now - DateTime.UtcNow).Hours)
                            };
                            if (car.Videos != null && car.Videos.Count != 0)
                            {
                                postCar.MediaType = MediaType.Video;
                                postCar.UrlVideo = car.Videos[0].Uri;
                            }
                            postItem.Items.Add(postCar);
                        }
                    }
                    mediaList.Add(postItem);
                }
                i++;
            }
            return mediaList;
        }

        private static async Task<InstaCommentList> GetCommentsByMediaId(User user, string mediaPk)
        {
            IResult<InstaCommentList> comments = await user.API.CommentProcessor.GetMediaCommentsAsync(mediaPk,
                PaginationParameters.MaxPagesToLoad(5));
            return comments.Value;
        }

        public static async void ShowComments(User user, Page page, string mediaPk)
        {
            InstaCommentList comments = await GetCommentsByMediaId(user, mediaPk);
            var commentDialog = new CommentsPage(user, page, comments);
            if (comments.Comments != null && comments.Comments.Count != 0)
            {
                await commentDialog.ShowAsync();
            }
        }

        public static async Task<bool> LikeMedia(User user, CustomMedia media)
        {
            var like = await user.API.MediaProcessor.LikeMediaAsync(media.Pk);
            return like.Succeeded;
        }

        public static async Task<bool> UnlikeMedia(User user, CustomMedia media)
        {
            var like = await user.API.MediaProcessor.UnLikeMediaAsync(media.Pk);
            return like.Succeeded;
        }

        #endregion

        #region Messaging Processor

        private static async Task<bool> SharedInDirect(User userObject, string id, InstaMediaType mediaType,
            long idUser)
        {
            var shared = await userObject.API.MessagingProcessor.ShareMediaToUserAsync(id, mediaType, string.Empty,
                idUser);
            return shared.Succeeded;
        }

        public static async Task<bool> ShareMedia(User currentUser, ObservableCollection<CustomMedia> medias)
        {
            var contentShared = new ContentDialog()
            {
                PrimaryButtonText = "Send",
                SecondaryButtonText = "Cancel",
                FullSizeDesired = true
            };
            InstaMediaType mediaType = InstaMediaType.Image;
            CustomMedia media = medias[0];
            if (medias.Count > 1)
            {
                mediaType = InstaMediaType.Carousel;
            }
            else if (medias.Count == 1)
            {
                mediaType = (media.MediaType == MediaType.Image) ? InstaMediaType.Image : InstaMediaType.Video;
            }

            var frame = new Frame()
            {
                //Width = 1000,
                //Height = 400
            };
            frame.Navigate(typeof(SharedPage), new object[]
                {
                    currentUser,
                    media.MediaType,
                    media.Pk
                });
            contentShared.Content = frame;
            var dialog = await contentShared.ShowAsync();
            if (dialog == ContentDialogResult.Primary)
            {
                if (frame.Content is SharedPage page && page.SelectedUser != null)
                {
                    bool shared = await InstaServer.SharedInDirect(currentUser, media.Pk, mediaType, page.SelectedUser.Pk);
                    return shared;
                }
            }
            return false;
        }

        #endregion

        #region Story
        public static async Task GetCurrentUserStories(User userObject)
        {
            var currentStories = await userObject.API.StoryProcessor.GetStoryFeedAsync();
            if (currentStories.Succeeded)
            {
                userObject.UserData.Stories = GetUserStoriesCustom(currentStories.Value);
            }

            OnUserStoriesLoaded?.Invoke();
            IsStoriesLoaded = true;
        }
        public static ObservableCollection<UserStory> GetUserStoriesCustom(InstaStoryFeed instaStoryFeed)
        {
            var userStories = new ObservableCollection<UserStory>();
            foreach (var story in instaStoryFeed.Items)
            {
                var userStoryItem = new UserStory()
                {
                    User = story.User,
                    Story = GetUrlsStoriesUser(story.Items)
                };
                userStories.Add(userStoryItem);
            }
            return userStories;
        }
        public static async Task<ObservableCollection<CustomMedia>> GetStoryUser(User userObject, InstaUserInfo unfUser)
        {
            if (userObject != null && unfUser != null)
            {
                var stories = await userObject.API.StoryProcessor.GetUserStoryAsync(unfUser.Pk);
                return stories.Succeeded ? GetUrlsStoriesUser(stories.Value.Items) : null;
            }
            return null;
        }
        public static async Task<ObservableCollection<CustomMedia>> GetStoryUser(User userObject, long userPk)
        {
            if (userObject != null)
            {
                var stories = await userObject.API.StoryProcessor.GetUserStoryAsync(userPk);
                return stories.Succeeded ? GetUrlsStoriesUser(stories.Value.Items) : null;
            }
            return null;
        }
        public static ObservableCollection<CustomMedia> GetUrlsStoriesUser(List<InstaStoryItem> stories, int numberId = 0)
        {
            var storiesList = new ObservableCollection<CustomMedia>();
            int i = numberId;
            foreach (var story in stories)
            {
                var cutM = new CustomMedia()
                {
                    Pk = story.Pk.ToString(),
                    Name = $"{story.User.UserName}_story_{story.Pk}",
                    UrlBigImage = story.ImageList[0].Uri,
                    UrlSmallImage = story.ImageList[1].Uri,
                    MediaType = MediaType.Image,
                    Date = story.TakenAt.AddHours((DateTime.Now - DateTime.UtcNow).Hours)
                };
                if (story.VideoList.Count > 0)
                {
                    cutM.MediaType = MediaType.Video;
                    cutM.UrlVideo = story.VideoList[0].Uri;
                }
                storiesList.Add(cutM);
                i++;
            }
            return storiesList;
        }
        public static async Task<InstaHighlightFeeds> GetArchiveCollectionStories(User instUser, long userId)
        {
            var collection = await instUser.API.StoryProcessor.GetHighlightFeedsAsync(userId);
            return collection.Value;
        }
        public static async Task<ObservableCollection<CustomMedia>> GetHighlightStories(User instUser, string highlightId)
        {
            var stories = await instUser.API.StoryProcessor.GetHighlightMediasAsync(highlightId);
            return GetUrlsStoriesUser(stories.Value.Items);
        }
        public static async Task<bool> AnswerToStory(User user, string text, string storyPk, long userPk)
        {
            IResult<bool> result = await user.API.StoryProcessor.ReplyToStoryAsync(storyPk, userPk, text);
            return result.Value;
        }
        #endregion

        #region TaskCancel

        public static void CancelTasks()
        {
            try
            {
                cancellationTokenMedia.Cancel();
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region Download medias

        public static async Task DownloadCarousel(ObservableCollection<CustomMedia> images, InstaUserShort instUser)
        {
            try
            {
                var folderPicker = new FolderPicker
                {
                    SuggestedStartLocation = PickerLocationId.Desktop
                };
                folderPicker.FileTypeFilter.Add("*");

                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                if (folder != null)
                {
                    var userFolder = await folder.CreateFolderAsync(instUser.UserName,
                        CreationCollisionOption.ReplaceExisting);

                    foreach (var item in images)
                    {
                        string urlForSave;
                        StorageFile storageFile;
                        if (item.MediaType == MediaType.Image)
                        {
                            storageFile = await userFolder.CreateFileAsync($"{item.Name}.jpg",
                                CreationCollisionOption.FailIfExists);
                            urlForSave = item.UrlBigImage;
                        }
                        else
                        {
                            storageFile = await userFolder.CreateFileAsync($"{item.Name}.mp4",
                                CreationCollisionOption.FailIfExists);
                            urlForSave = item.UrlVideo;
                        }

                        StorageFile file = storageFile;
                        var task = Task.Run(async () =>
                        {
                            var webRequest = WebRequest.CreateHttp(urlForSave);
                            var webResponse = await Task.Factory.FromAsync(webRequest.BeginGetResponse,
                                webRequest.EndGetResponse, null);
                            using (var responseStream = webResponse.GetResponseStream())
                            {
                                using (var resultFileStream = await file.OpenStreamForWriteAsync())
                                {
                                    if (responseStream != null)
                                    {
                                        await responseStream.CopyToAsync(resultFileStream)
                                            .ContinueWith((e) => { });
                                    }
                                }
                            }
                        });
                        task.Wait();
                    }
                }
                _ = new CustomDialog("Message", $"Post/s of {instUser.UserName} downloaded", "All right");
            }
            catch (Exception e)
            {
                _ = new CustomDialog("Warning",
                    $"Error - {e}", "All right");
            }
        }

        public static async Task<bool> DownloadMedia(CustomMedia media)
        {
            string url;
            switch (media.MediaType)
            {
                case MediaType.Image:
                    url = media.UrlBigImage;
                    break;
                case MediaType.Video:
                    url = media.UrlVideo;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };

            switch (media.MediaType)
            {
                case MediaType.Image:
                    savePicker.FileTypeChoices.Add("jpeg image", new List<string>()
                    {
                        ".jpg"
                    });
                    break;
                case MediaType.Video:
                    savePicker.FileTypeChoices.Add("mp4 video", new List<string>()
                    {
                        ".mp4"
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            savePicker.SuggestedFileName = media.Name;
            StorageFile file = await savePicker.PickSaveFileAsync();

            bool? result = null;
            if (file != null)
            {
                var task = Task.Run(async () =>
                {
                    var webRequest = WebRequest.CreateHttp(url);
                    var webResponse = await Task.Factory.FromAsync(webRequest.BeginGetResponse,
                        webRequest.EndGetResponse, null);
                    using (var responseStream = webResponse.GetResponseStream())
                    {
                        using (var resultFileStream = await file.OpenStreamForWriteAsync())
                        {
                            await responseStream.CopyToAsync(resultFileStream)
                                      .ContinueWith((e) => { result = e.IsCompletedSuccessfully; });
                        }
                    }
                });
                task.Wait();
            }
            _ = result != null && result.Value ? new CustomDialog("Message", $"Media downloaded\n", "All right", url)
                : new CustomDialog("Message", "Failed\n", "All right");

            return result ?? false;
        }

        public static async Task DownloadAnyPost(InstaUserShort selectedUser, ObservableCollection<CustomMedia> medias)
        {
            if (medias.Count > 1)
            {
                await DownloadCarousel(medias, selectedUser);
            }
            else if (medias.Count == 1)
            {
                await DownloadMedia(medias[0]);
            }
        }

        public static async Task Download(string url)
        {
            await DownloadMedia(new CustomMedia()
            {
                Pk = "123",
                MediaType = MediaType.Image,
                Name = "ProfileImage",
                UrlBigImage = url
            });
        }

        public static async Task DownloadAnyPosts(InstaUserShort selectedUser, ObservableCollection<PostItem> medias)
        {
            try
            {
                var folderPicker = new FolderPicker
                {
                    SuggestedStartLocation = PickerLocationId.Desktop
                };
                folderPicker.FileTypeFilter.Add("*");

                StorageFolder folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    var userFolder = await folder.CreateFolderAsync(selectedUser.UserName,
                        CreationCollisionOption.ReplaceExisting);
                    foreach (var post in medias)
                    {
                        foreach (var item in post.Items)
                        {
                            string urlForSave;
                            StorageFile storageFile;
                            if (item.MediaType == MediaType.Image)
                            {
                                storageFile = await userFolder.CreateFileAsync($"{item.Name}.jpg",
                                    CreationCollisionOption.FailIfExists);
                                urlForSave = item.UrlBigImage;
                            }
                            else
                            {
                                storageFile = await userFolder.CreateFileAsync($"{item.Name}.mp4",
                                    CreationCollisionOption.FailIfExists);
                                urlForSave = item.UrlVideo;
                            }

                            StorageFile file = storageFile;
                            var task = Task.Run(async () =>
                            {
                                var webRequest = WebRequest.CreateHttp(urlForSave);
                                var webResponse = await Task.Factory.FromAsync(webRequest.BeginGetResponse,
                                    webRequest.EndGetResponse, null);
                                using (var responseStream = webResponse.GetResponseStream())
                                {
                                    using (var resultFileStream = await file.OpenStreamForWriteAsync())
                                    {
                                        if (responseStream != null)
                                        {
                                            await responseStream.CopyToAsync(resultFileStream)
                                                .ContinueWith((e) => { });
                                        }
                                    }
                                }
                            });
                            task.Wait();
                        }
                    }
                }
                _ = new CustomDialog("Message", $"Posts ({medias.Count}) of {selectedUser.UserName} downloaded", "All right");
            }
            catch (Exception e)
            {
                _ = new CustomDialog("Warning",
                    $"Error - {e}", "All right");
            }
        }

        #endregion

        #region Preview Post

        public static async Task<ObservableCollection<PreviewPost>> GetPreviewPosts(User instaUser)
        {
            var previews = new ObservableCollection<PreviewPost>();
            var posts = await instaUser.API.UserProcessor.GetUserMediaAsync(instaUser.LoginUser,
                PaginationParameters.MaxPagesToLoad(1));
            if (posts.Succeeded)
            {
                int i = 1;
                foreach (var item in posts.Value)
                {
                    var preview = GetPreviewPost(item, i);
                    previews.Add(preview);
                    i++;
                }
            }
            return previews;
        }

        private static PreviewPost GetPreviewPost(InstaMedia item, int id)
        {
            var previewPost = new PreviewPost();
            if (item.Images != null && item.Images.Count != 0)
            {
                previewPost = new PreviewPost()
                {
                    Id = id,
                    Url = item.Images[0].Uri
                };
            }
            else if (item.Carousel != null && item.Carousel.Count != 0 && item.Carousel[0].Images != null
                && item.Carousel[0].Images.Count != 0)
            {
                previewPost = new PreviewPost()
                {
                    Id = id,
                    Url = item.Carousel[0].Images[0].Uri
                };
            }
            return previewPost;
        }

        #endregion

        #region Location

        public static async Task Location(User currentUser, double latitude, double longitude)
        {
            var users = await currentUser.API.LocationProcessor.SearchUserByLocationAsync(latitude, longitude,
                string.Empty);
        }

        #endregion

        #region Bookmarks

        public static async Task SaveBookmarksAsync(User user)
        {
            string longIds = string.Empty;
            foreach (var item in user.UserData.Bookmarks)
            {
                longIds += item.Pk.ToString() + ",";
            }
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile sampleFile = await localFolder.CreateFileAsync("dataBookmarks.txt",
                CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(sampleFile, longIds);
        }

        public static async Task GetBookmarksAsync(User user)
        {
            var results = new ObservableCollection<InstaUserShort>();
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            if (File.Exists(localFolder.Path + @"\dataBookmarks.txt"))
            {
                StorageFile sampleFile = await localFolder.GetFileAsync("dataBookmarks.txt");
                string ids = await FileIO.ReadTextAsync(sampleFile);
                string[] longIds = ids.Split(',');
                foreach (var item in longIds)
                {
                    if (!string.IsNullOrEmpty(item))
                        results.Add(await GetInstaUserShortById(user, long.Parse(item)));
                }
                user.UserData.Bookmarks = results;
            }
        }

        public static bool IsContrainsAccount(User user, long id)
        {
            return user.UserData.Bookmarks.Where(x => x.Pk == id).ToList().Count > 0 ? true : false;
        }

        #endregion

        #region Saved Posts

        public static async Task<InstaCollections> GetListCollections(User instaUser)
        {
            var collects = await instaUser.API.CollectionProcessor.GetCollectionsAsync(PaginationParameters.Empty);
            return collects.Value;
        }

        public static async Task<IEnumerable<PostItem>> GetMediasByCollection(User instUser, InstaCollectionItem instCollection)
        {
            if (instCollection == null)
            {
                throw new ArgumentNullException(nameof(instCollection));
            }

            var mediaC = await instUser.API.CollectionProcessor.GetSingleCollectionAsync(instCollection.CollectionId,
                PaginationParameters.MaxPagesToLoad(5));
            var mediasIds = mediaC.Value.Media.Select(x => new
            {
                x.Pk
            }).ToList();
            OnUserCollectionLoaded?.Invoke();
            return instUser.UserData.SavedPostItems
                       .Where(sItem => mediasIds.Where(x => x.Pk == sItem.Items[0].Pk).ToList().Count != 0)
                       .Select(sItem => sItem);
        }

        public static async Task GetAllSavedPosts(User instUser)
        {
            IResult<InstaMediaList> posts = await instUser.API.FeedProcessor.GetSavedFeedAsync(PaginationParameters.Empty);
            var i = 1;
            if (posts.Value.Count != 0)
            {
                instUser.UserData.SavedPostItems = new ObservableCollection<PostItem>();
                foreach (var itemS in posts.Value)
                {
                    PostItem savedPost = GetPostItem(itemS, i);
                    i++;
                    instUser.UserData.SavedPostItems.Add(savedPost);
                }

                IsSavedPostsAllLoaded = true;
                IsSavedPostsLoaded = true;
                OnUserSavedPostsAllLoaded?.Invoke();
            }
        }

        #endregion

        #region Feed

        private static async void GetFeed(User user)
        {
            var b = await user.API.FeedProcessor.GetLikedFeedAsync(PaginationParameters.MaxPagesToLoad(10));
            var a = await user.API.FeedProcessor.GetUserTimelineFeedAsync(PaginationParameters.MaxPagesToLoad(10), new[]
                {
                    "2171160902940863145"
                });
        }

        #endregion

        #region Direct

        public static async Task<ObservableCollection<InstaDirectInboxThread>> GetDirectDialogsAsync(User user)
        {
            IResult<InstaDirectInboxContainer> a = await user.API.MessagingProcessor
                                                             .GetDirectInboxAsync(PaginationParameters.MaxPagesToLoad(1));
            return new ObservableCollection<InstaDirectInboxThread>(a.Value.Inbox.Threads);
        }

        public static async Task<ObservableCollection<InstaDirectInboxItem>> GetDialogAudioAsync(User user,
            string threadId)
        {
            var b = await user.API.MessagingProcessor.GetDirectInboxThreadAsync(threadId,
                PaginationParameters.MaxPagesToLoad(10));

            return new ObservableCollection<InstaDirectInboxItem>(b.Value.Items
                                                                      .Where(x
                => x.ItemType == InstaDirectThreadItemType.VoiceMedia)
                                                                      .ToList());
        }

        #endregion

        #region Explore
        public static async Task<ObservableCollection<PostItem>> GetExploreFeed(User user)
        {
            var feed = await user.API.FeedProcessor.GetExploreFeedAsync(PaginationParameters.MaxPagesToLoad(1));
            var i = 0;
            var result = new ObservableCollection<PostItem>();
            foreach (var item in feed.Value.Medias)
            {
                result.Add(GetPostItem(item, i));
                i++;
            }

            OnUserExploreFeedLoaded?.Invoke();
            return result;
        }

        public static async Task<ObservableCollection<PostItem>> GetFeedByTag(User user, string tag)
        {
            var feed = await user.API.FeedProcessor.GetTagFeedAsync(tag, PaginationParameters.MaxPagesToLoad(1));

            var i = 0;
            var result = new ObservableCollection<PostItem>();
            foreach (var item in feed.Value.Medias)
            {
                result.Add(GetPostItem(item, i));
                i++;
            }

            OnUserExploreFeedLoaded?.Invoke();
            return result;
        }
        #endregion

        #region  Show
        public static async Task ShowFollowers(User instaUser, string userName, Frame parentFrame)
        {
            await ShowData(instaUser, userName, parentFrame, DataType.Followers);
        }

        public static async Task ShowFollowing(User instaUser, string userName, Frame parentFrame)
        {
            await ShowData(instaUser, userName, parentFrame, DataType.Following);
        }

        public static async Task ShowLikers(User instaUser, string mediaPk, Frame parentFrame)
        {
            await ShowData(instaUser, mediaPk, parentFrame, DataType.Likers);
        }

        public static async Task ShowData(User instaUser, string userName, Frame parentFrame, DataType type)
        {
            var contentDialog = new ContentDialog()
            {
                FullSizeDesired = true,
                PrimaryButtonText = "Close"
            };

            var frame = new Frame();
            frame.Navigate(typeof(ListPersonsPage), new object[]
            {
                instaUser,
                userName,
                type,
                parentFrame
            });
            contentDialog.Content = frame;
            await contentDialog.ShowAsync();
        }
        #endregion

        #region Common data
        public static async Task<ObservableCollection<InstaUserShort>> GetFollowers(User instaUser, string userName, bool all = false)
        {
            var followers =
                await instaUser.API.UserProcessor.GetUserFollowersAsync(userName,
                    all ? PaginationParameters.Empty : PaginationParameters.MaxPagesToLoad(1));
            OnCommonDataLoaded?.Invoke();
            return new ObservableCollection<InstaUserShort>(followers.Value.ToArray());
        }

        public static async Task<ObservableCollection<InstaUserShort>> GetFollowing(User instaUser, string userName, bool all = false)
        {
            var following =
                await instaUser.API.UserProcessor.GetUserFollowingAsync(userName,
                    all ? PaginationParameters.Empty : PaginationParameters.MaxPagesToLoad(1));
            OnCommonDataLoaded?.Invoke();
            return new ObservableCollection<InstaUserShort>(following.Value.ToArray());
        }

        public static async Task<ObservableCollection<InstaUserShort>> GetLikers(User instaUser, string mediaPk)
        {
            var likes =
                await instaUser.API.MediaProcessor.GetMediaLikersAsync(mediaPk);
            OnCommonDataLoaded?.Invoke();
            return new ObservableCollection<InstaUserShort>(likes.Value.ToArray());
        }
        #endregion

        #region  Custom feed

        public static string FeedMaxLoadedId;
        public static async Task GetCustomFeed(User user, bool refresh = false)
        {
            if (!IsFeedLoading)
            {
                if (refresh)
                {
                    FeedMaxLoadedId = "";
                }

                IsFeedLoading = true;
                var timeLineFeed =
                    await user.API.FeedProcessor.GetUserTimelineFeedAsync(PaginationParameters.MaxPagesToLoad(1)
                        .StartFromMaxId(FeedMaxLoadedId));
                FeedMaxLoadedId = timeLineFeed.Value.NextMaxId;

                int i = user.UserData.Feed.Count != 0 ? user.UserData.Feed.Last().Id + 1 : 1;
                foreach (var media in timeLineFeed.Value.Medias)
                {
                    //var userMedia = user.UserData.UserFollowing?.FirstOrDefault(x => x.UserName == media.User.UserName);
                    //Debug.Print(user.UserData.UserFollowing.Count.ToString());

                    if (media.User.FriendshipStatus.Following)
                    {
                        var m = user.UserData.Feed.FirstOrDefault(x => x.Items[0].Pk == media.Pk);
                        if (m != null)
                        {
                            user.UserData.Feed.Remove(m);
                        }

                        user.UserData.Feed.Add(GetPostItem(media, i));
                        OnUserFeedLoaded?.Invoke(i);
                        i++;
                    }
                }

                IsFeedLoading = false;
                UpdateCountFeed?.Invoke();
            }
        }

        public static async Task<ObservableCollection<PostItem>> GetCustomFeedA(User user, InstaUserShort username, int days = 3)
        {
            var medias = new ObservableCollection<PostItem>();
            var id = 1;
            var m = await user.API.UserProcessor.GetUserMediaAsync(username.UserName, PaginationParameters.MaxPagesToLoad(0));
            foreach (var media in GetUrlsMediasUser(m.Value, userShort: username))
            {
                if (media.Items[0].Date >= DateTime.Now.AddDays(-days))
                {
                    media.Id = id;
                    medias.Add(media);
                    id++;
                }
            }
            OnFeedLoaded?.Invoke();
            return new ObservableCollection<PostItem>(medias.OrderBy(x => x.Items[0].Date).ToList());
        }

        public static async Task<ObservableCollection<InstaUserShort>> GetUserInstaShortsByNames(User user, ObservableCollection<string> names)
        {
            var ob = new ObservableCollection<InstaUserShort>();
            foreach (string name in names)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    ob.Add(await GetInstaUserShortByName(user, name));
                }
            }

            OnUsersFeedLoaded?.Invoke();
            return ob;
        }

        public static async Task<ObservableCollection<UserFeed>> GetUserInstaShortsFeed(User user, ObservableCollection<string> names)
        {
            var ob = new ObservableCollection<UserFeed>();
            foreach (string name in names)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    ob.Add(new UserFeed()
                    {
                        InstaUserShort = await GetInstaUserShortByName(user, name),
                        Received = false
                    });
                }
            }

            OnUsersFeedLoaded?.Invoke();
            return ob;
        }

        public static async Task GetFeedUsers(User instaUser)
        {
            var results = new ObservableCollection<InstaUserShort>();
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            if (File.Exists(localFolder.Path + @"\dataFeedUsers.txt"))
            {
                StorageFile sampleFile = await localFolder.GetFileAsync("dataFeedUsers.txt");
                string ids = await FileIO.ReadTextAsync(sampleFile);
                string[] longIds = ids.Split(',');

                foreach (string user in longIds)
                {
                    if (!string.IsNullOrEmpty(user))
                    {
                        instaUser.UserData.FeedUsers.Add(user);
                    }
                }
            }
        }
        private static async Task SaveFeedUsers(User instaUser)
        {
            string longIds = string.Empty;
            foreach (string item in instaUser.UserData.FeedUsers)
            {
                longIds += item + ",";
            }

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile sampleFile = await localFolder.CreateFileAsync("dataFeedUsers.txt",
                CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(sampleFile, longIds);
        }

        public static async Task RemoveFeedUser(User instaUser, string name)
        {
            instaUser.UserData.FeedUsers.Remove(name);
            await SaveFeedUsers(instaUser);
        }

        public static async Task<ObservableCollection<InstaUserShort>> AddFeedUsers(User instaUser, int countPersons)
        {
            var contentDialog = new ReturnPersonPage(instaUser, DataType.Following);
            await contentDialog.ShowAsync();
            var resultCollection = new ObservableCollection<InstaUserShort>();
            if (contentDialog.DialogResult == MyResult.Add)
            {
                foreach (var user in contentDialog.SelectedUserShorts)
                {
                    if (instaUser.UserData.FeedUsers.Count < countPersons)
                    {
                        instaUser.UserData.FeedUsers.Add(user.UserName);
                        resultCollection.Add(user);
                    }
                }
            }

            await SaveFeedUsers(instaUser);
            return resultCollection;
        }

        private static void RemoveEvents(UpdateUserCheck eventHandler)
        {
            foreach (Delegate d in eventHandler.GetInvocationList())
            {
                eventHandler -= d as UpdateUserCheck;
            }
        }
        #endregion

        #region Archive

        public static string ArchivePostMaxId = "";
        private static bool IsArchivePostsLoading = false;
        public static async Task GetArchivePosts(User user)
        {
            if (!IsArchivePostsLoading)
            {
                IsArchivePostsLoading = true;
                var archivePosts =
                    await user.API.MediaProcessor.GetArchivedMediaAsync(PaginationParameters.MaxPagesToLoad(1)
                        .StartFromMaxId(ArchivePostMaxId));

                ArchivePostMaxId = archivePosts.Value.NextMaxId;

                int i = user.UserData.ArchivePosts.Count != 0 ? user.UserData.ArchivePosts.Last().Id + 1 : 1;
                foreach (var post in archivePosts.Value)
                {
                    if (user.UserData.ArchivePosts.FirstOrDefault(x => x.Items[0].Pk == post.Pk) != null)
                    {
                        ArchivePostMaxId = null;
                        continue;
                    }

                    user.UserData.ArchivePosts.Add(GetPostItem(post, i));
                    i++;
                }

                IsArchivePostsLoading = false;
                OnUserArchivePostsLoaded?.Invoke();
            }
        }

        public static event CompleteHandler OnUserArchiveStoriesListLoaded;
        public static async Task GetArchiveStoriesList(User user, bool isRefresh)
        {
            IResult<InstaHighlightShortList> archiveStoriesList = await user.API.StoryProcessor.GetHighlightsArchiveAsync();
            user.UserData.ArchiveHigh = archiveStoriesList.Value;
            OnUserArchiveStoriesListLoaded?.Invoke();

            if (isRefresh)
            {
                lastArchiveStoryId = 0;
                lastArchiveStoryMaxId = 0;
            }
        }

        private static string archiveNameMaxId = null;
        public static event CompleteHandler OnUserArchiveStoriesLoaded;
        public static int lastArchiveStoryId = 0;
        public static int lastArchiveStoryMaxId = 0;
        private static bool IsArchiveStoryLoading = false;
        public static async Task GetArchiveStories(User user, InstaHighlightShortList archiveStoriesList)
        {
            if (!IsArchiveStoryLoading)
            {
                IsArchiveStoryLoading = true;
                await Task.Delay(1000);
                foreach (var archiveName in archiveStoriesList.Items.Skip(lastArchiveStoryMaxId).Take(15))
                {
                    var story = await user.API.StoryProcessor.GetHighlightsArchiveMediasAsync(archiveName.Id);
                    var medias = GetUrlsStoriesUser(story.Value.Items, lastArchiveStoryId);

                    foreach (CustomMedia media in medias)
                    {
                        user.UserData.ArchiveStories.Add(media);
                        OnUserArchiveStoriesLoaded?.Invoke();
                    }

                    archiveNameMaxId = archiveName.Id;
                    lastArchiveStoryId += medias.Count;
                    lastArchiveStoryMaxId += 1;
                }

                IsArchiveStoryLoading = false;
            }
        }

        #endregion
    }
}
