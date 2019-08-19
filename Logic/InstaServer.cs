using InstagramApiSharp;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Logger;
using MyInsta.Model;
using MyInsta.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using System.IO;
using Windows.Web.Http;
using Windows.Storage;
using System.Text.RegularExpressions;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;
using InstagramApiSharp.API;
using System.Threading;

namespace MyInsta.Logic
{
    public static class InstaServer
    {
        public static string LatestMediaMaxId = "";
        private static CancellationTokenSource cancellationTokenMedia;
        public static async Task LoginInstagram(User userObject, LoginPage page)
        {
            if (ExistsConnection())
            {
                string savedAPI = await GetSavedApi();

                var api = InstaApiBuilder.CreateBuilder()
                        .SetUser(new UserSessionData() { UserName = userObject.LoginUser, Password = userObject.PasswordUser })
                        .UseLogger(new DebugLogger(LogLevel.Exceptions))
                        .Build();
                api.LoadStateDataFromString(savedAPI.ToString());
                userObject.API = api;
                page.Frame.Navigate(typeof(MenuPage), userObject);
            }
            else
            {
                if (userObject != null && userObject.LoginUser != null && userObject.PasswordUser != null)
                {
                    var api = InstaApiBuilder.CreateBuilder()
                        .SetUser(new UserSessionData() { UserName = userObject.LoginUser, Password = userObject.PasswordUser })
                        .UseLogger(new DebugLogger(LogLevel.Exceptions))
                        .Build();
                    userObject.API = api;

                    var logResult = await userObject.API.LoginAsync();
                    if (logResult.Succeeded)
                    {
                        await SaveApiString(userObject.API);
                        page.Frame.Navigate(typeof(MenuPage), userObject);
                    }
                    else
                    {
                        if (logResult.Value == InstaLoginResult.ChallengeRequired)
                        {
                            var resultApi = await AccountVerify(userObject.API);
                            if (resultApi != null)
                            {
                                userObject.API = resultApi;
                                page.Frame.Navigate(typeof(MenuPage), userObject);
                            }
                        }
                    }
                }
            }
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

        private static async Task<IInstaApi> AccountVerify(IInstaApi api)
        {
            var challenge = await api.GetChallengeRequireVerifyMethodAsync();
            if (challenge.Value.StepData != null && challenge.Value.StepData.PhoneNumber != null)
            {
                var request = await api.RequestVerifyCodeToSMSForChallengeRequireAsync();
                ContentDialog dialog = new ContentDialog()
                {
                    Width = 500,
                    CloseButtonText = "Cancel",
                    PrimaryButtonText = "Send"
                };
                StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Vertical };
                TextBlock textBlock = new TextBlock() { Text = "Write SMS code from phone", Margin = new Windows.UI.Xaml.Thickness(10) };
                TextBox inputTextBox = new TextBox() { TextAlignment = Windows.UI.Xaml.TextAlignment.Center, PlaceholderText = "Code", Margin = new Windows.UI.Xaml.Thickness(10) };
                stackPanel.Children.Add(textBlock);
                stackPanel.Children.Add(inputTextBox);
                dialog.Content = stackPanel;
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    var code = await api.VerifyCodeForChallengeRequireAsync(inputTextBox.Text);
                    if (code.Succeeded)
                    {
                        await SaveApiString(api);
                        return api;
                    }
                }
                return null;
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
            else return false;
        }

        public static async Task GetUserData(User userObject)
        {
            userObject.UserData.UserFollowers = new ObservableCollection<InstaUserShort>();
            //userObject.UserData.UserFriends = new ObservableCollection<InstaUserShort>();
            //userObject.UserData.UserUnfollowers = new ObservableCollection<InstaUserShort>();
            userObject.UserData.SavedItems = new ObservableCollection<SavedItem>();

            await GetUserSavedItems(userObject);
            await GetUserFollowers(userObject);
            await GetUserFriendsAndUnfollowers(userObject);
        }

        private static async Task GetUserFollowers(User user)
        {
            var f = await user.API.UserProcessor.GetUserFollowersAsync(user.LoginUser, PaginationParameters.MaxPagesToLoad(5));
            foreach (var item in f.Value)
            {
                if (!user.UserData.UserFollowers.Contains(item))
                    user.UserData.UserFollowers.Add(item);
            }
        }

        private static async Task GetUserSavedItems(User user)
        {
            var items = await user.API.FeedProcessor.GetSavedFeedAsync(PaginationParameters.MaxPagesToLoad(5));
            int i = 1;
            foreach (var itemS in items.Value)
            {
                var savedPost = GetSavedItem(itemS, i);
                i++;
                user.UserData.SavedItems.Add(savedPost);
            }
        }

        private static async Task GetUserFriendsAndUnfollowers(User user)
        {
            var fling = await user.API.UserProcessor.GetUserFollowingAsync(user.LoginUser, PaginationParameters.MaxPagesToLoad(5));
            foreach (var item in fling.Value)
            {
                var status = await user.API.UserProcessor.GetFriendshipStatusAsync(item.Pk);
                if (status.Value.Following && status.Value.FollowedBy && !user.UserData.UserFriends.Contains(item))
                    user.UserData.UserFriends.Add(item);
                else
                if (status.Value.Following && !status.Value.FollowedBy && !user.UserData.UserUnfollowers.Contains(item))
                    user.UserData.UserUnfollowers.Add(item);
            }
        }

        public static async Task<InstaUserInfo> GetInfoUser(User userObject, string nick)
        {
            if (userObject != null)
            {
                IResult<InstaUserInfo> userInfo = await userObject.API.UserProcessor.GetUserInfoByUsernameAsync(nick);
                return userInfo.Value;
            }
            else return null;
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
                            userObject.UserData.UserUnfollowers.Remove(person);
                        if (userObject.UserData.UserFriends.Contains(person))
                            userObject.UserData.UserFriends.Remove(person);
                    }
                }
            }
            catch
            {

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
                    if (unF.Value.Following && unF.Value.FollowedBy && !userObject.UserData.UserFriends
                        .Contains(person))
                        userObject.UserData.UserFriends.Add(person);
                    else
                        if (unF.Value.Following && !unF.Value.FollowedBy && !userObject.UserData.UserUnfollowers
                        .Contains(person))
                        userObject.UserData.UserUnfollowers.Add(person);
                }
            }
        }

        public static async Task<InstaStoryFriendshipStatus> GetFriendshipStatus(User userObject, InstaUserInfo unfUser)
        {
            if (userObject != null && unfUser != null)
            {
                var status = await userObject.API.UserProcessor.GetFriendshipStatusAsync(unfUser.Pk);
                if (status.Succeeded)
                    return status.Value;
                else
                    return null;
            }
            else
                return null;
        }

        public static async Task<ObservableCollection<CustomMedia>> GetMediaUser(User userObject, InstaUserInfo unfUser, int count)
        {
            if (userObject != null && unfUser != null)
            {
                InstaMediaList media = new InstaMediaList();
                if (count == 0)
                {
                    var response = await userObject.API.UserProcessor.GetUserMediaAsync(unfUser.Username, PaginationParameters.MaxPagesToLoad(5));
                    media = response.Value;
                }
                else
                    media = await GetMediaUserAll(userObject, unfUser);
                if (media.Count != 0)
                    return GetUrlsMediasUser(media);
                else
                    return null;
            }
            else
                return null;
        }

        private static async Task<InstaMediaList> GetMediaUser(User userObject, InstaUserInfo unfUser)
        {
            var media = await userObject.API.UserProcessor.GetUserMediaAsync(unfUser.Username, PaginationParameters.MaxPagesToLoad(1).StartFromMaxId(LatestMediaMaxId));
            LatestMediaMaxId = media.Value.NextMaxId;
            return media.Value;
        }

        public static async Task<bool> SaveMediaInProfile(User userObject, string mediaPk)
        {
            var result = await userObject.API.MediaProcessor.SaveMediaAsync(mediaPk);
            if (result.Succeeded)
                return true;
            else
                return false;
        }

        public static async Task<bool> SharedInDirect(User userObject, string id, MediaType mediaType, long idUser)
        {
            InstaMediaType instaMedia = InstaMediaType.Video;
            if (mediaType == MediaType.Video)
                instaMedia = InstaMediaType.Video;
            else
                if (mediaType == MediaType.Image)
                instaMedia = InstaMediaType.Image;
            var shared = await userObject.API.MessagingProcessor.ShareMediaToUserAsync(id, instaMedia, "", idUser);
            if (shared.Succeeded)
                return true;
            else
                return false;
        }


        private static async Task<InstaMediaList> GetMediaUserAll(User userObject, InstaUserInfo unfUser)
        {
            cancellationTokenMedia = new CancellationTokenSource();
            CancellationToken token = cancellationTokenMedia.Token;
            try
            {
                InstaMediaList instaMedias = new InstaMediaList();
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

        public static async Task<ObservableCollection<CustomMedia>> GetStoryUser(User userObject, InstaUserInfo unfUser)
        {
            if (userObject != null && unfUser != null)
            {
                var stories = await userObject.API.StoryProcessor.GetUserStoryAsync(unfUser.Pk);
                if (stories.Succeeded)
                    return GetUrlsStoriesUser(stories.Value);
                else
                    return null;
            }
            else
                return null;
        }

        public static ObservableCollection<CustomMedia> GetUrlsStoriesUser(InstaStory stories)
        {
            ObservableCollection<CustomMedia> storiesList = new ObservableCollection<CustomMedia>();
            int i = 0;
            foreach (var story in stories.Items)
            {
                var custM = new CustomMedia()
                {
                    Pk = story.Pk.ToString(),
                    Name = $"Story_{i + 1}",
                    UrlBigImage = story.ImageList[0].Uri,
                    UrlSmallImage = story.ImageList[1].Uri,
                    MediaType = MediaType.Image
                };
                if (story.VideoList.Count > 0)
                {
                    custM.MediaType = MediaType.Video;
                    custM.UrlVideo = story.VideoList[0].Uri;
                }
                storiesList.Add(custM);
                i++;
            }
            return storiesList;
        }

        public static SavedItem GetSavedItem(InstaMedia item, int id)
        {
            SavedItem savedItem = new SavedItem()
            {
                Id = id,
                UserNamePost = item.User.UserName,
                UserPk = item.User.Pk,
                UserPicture = item.User.ProfilePicture
            };
            if (item.Images != null && item.Images.Count != 0)
            {
                var post = new CustomMedia()
                {
                    Pk = item.Pk.ToString(),
                    Name = $"SavedPost_{id}",
                    UrlSmallImage = item.Images[1].Uri,
                    UrlBigImage = item.Images[0].Uri,
                    CountLikes = item.LikesCount,
                    CountComments = item.CommentsCount != null ? int.Parse(item.CommentsCount) : 0,
                    MediaType = MediaType.Image
                };
                if (item.Videos != null && item.Videos.Count != 0)
                {
                    post.MediaType = MediaType.Video;
                    post.UrlVideo = item.Videos[0].Uri;
                }
                savedItem.Item = post;
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
                            Name = $"SavedPost_{id}_Carousel_{x + 1}",
                            UrlSmallImage = car.Images[0].Uri,
                            UrlBigImage = car.Images[1].Uri,
                            CountLikes = item.LikesCount,
                            CountComments = int.Parse(item.CommentsCount),
                            MediaType = MediaType.Image
                        };
                        if (car.Videos != null && car.Videos.Count != 0)
                        {
                            postCar.MediaType = MediaType.Video;
                            postCar.UrlVideo = car.Videos[0].Uri;
                        }
                        savedItem.Item = postCar;
                    }
                }
            }
            return savedItem;
        }

        public static ObservableCollection<CustomMedia> GetUrlsMediasUser(InstaMediaList medias)
        {
            ObservableCollection<CustomMedia> mediaList = new ObservableCollection<CustomMedia>();
            int i = 0;

            foreach (var item in medias)
            {
                if (item.Images != null && item.Images.Count != 0)
                {
                    var post = new CustomMedia()
                    {
                        Pk = item.Pk,
                        Name = $"ImagePost_{i + 1}",
                        UrlSmallImage = item.Images[1].Uri,
                        UrlBigImage = item.Images[0].Uri,
                        CountLikes = item.LikesCount,
                        CountComments = item.CommentsCount != null ? int.Parse(item.CommentsCount) : 0,
                        MediaType = MediaType.Image
                    };
                    if (item.Videos != null && item.Videos.Count != 0)
                    {
                        post.MediaType = MediaType.Video;
                        post.UrlVideo = item.Videos[0].Uri;
                    }
                    mediaList.Add(post);
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
                                Pk = item.Pk,
                                Name = $"ImagePost_{i + 1}_Carousel_{x + 1}",
                                UrlSmallImage = car.Images[0].Uri,
                                UrlBigImage = car.Images[1].Uri,
                                CountLikes = item.LikesCount,
                                CountComments = int.Parse(item.CommentsCount),
                                MediaType = MediaType.Image
                            };
                            if (car.Videos != null && car.Videos.Count != 0)
                            {
                                postCar.MediaType = MediaType.Video;
                                postCar.UrlVideo = car.Videos[0].Uri;
                            }
                            mediaList.Add(postCar);
                        }
                    }
                }
                i++;
            }
            return mediaList;
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
                ProfilePicUrl = userInfo.Value.ProfilePicUrl
            };
        }


        public static IEnumerable<InstaUserShort> SearchByUserName(ObservableCollection<InstaUserShort> collection, string srt)
        {
            if (string.IsNullOrEmpty(srt))
                return collection;
            else
            {
                var items = collection.Where(x => x.UserName.Contains(srt));
                return items;
            }
        }

        public static async Task<InstaUserShort> SearchByUserName(User currentUser, string srt)
        {
            if (string.IsNullOrEmpty(srt))
                return null;
            else
            {
                var item = await currentUser.API.UserProcessor.GetUserInfoByUsernameAsync(srt);
                if (item.Succeeded)
                    return await GetInstaUserShortById(currentUser, item.Value.Pk);
                else
                    return null;
            }
        }

        public static async Task UnlikeProfile(User currentUser, InstaUserShort selectUser, ObservableCollection<CustomMedia> medias)
        {
            try
            {
                CustomDialog customDialog = new CustomDialog("Message", "Process started", "All right");
                //var mediaUser = await currentUser.API.UserProcessor.GetUserMediaByIdAsync(selectUser.Pk, PaginationParameters.MaxPagesToLoad(30));
                string unlike = "";
                foreach (var item in medias.Take(30))
                {
                    var p = await currentUser.API.MediaProcessor.UnLikeMediaAsync(item.Pk);
                    if (p.Succeeded)
                    {
                        unlike += item.Name + "\n";
                    }
                }
                customDialog = new CustomDialog("Message", $"30 posts of {selectUser.UserName} unliked \n {unlike}", "All right");
            }
            catch (Exception e)
            {
                CustomDialog dialog = new CustomDialog("Message", e.Message, "Ok");
            }

        }

        public static async Task UnFollowFromList(User currentUser, ObservableCollection<InstaUserShort> instaUsers)
        {
            CustomDialog customDialog = new CustomDialog("Message", "Process started", "All right");
            foreach (var item in instaUsers.Take(30))
            {
                var p = await currentUser.API.UserProcessor.UnFollowUserAsync(item.Pk);
            }
            await GetUserData(currentUser);
            customDialog = new CustomDialog("Message", "Unfollowed from all unfollowers", "All right");
        }

        public static async Task SaveImages(ObservableCollection<CustomMedia> images, InstaUserShort instaUser)
        {
            try
            {
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                folderPicker.FileTypeFilter.Add("*");

                Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    var userFolder = await folder.CreateFolderAsync(instaUser.UserName, CreationCollisionOption.ReplaceExisting);
                    CustomDialog custom = new CustomDialog("Message", $"Download (Media: {instaUser.UserName}) started", "All right");
                    foreach (var item in images)
                    {
                        var coverpic_file = await userFolder.CreateFileAsync($"{item.Name}.jpg", CreationCollisionOption.FailIfExists);
                        var httpWebRequest = HttpWebRequest.CreateHttp(item.UrlBigImage);
                        HttpWebResponse response = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
                        Stream resStream = response.GetResponseStream();
                        using (var stream = await coverpic_file.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            await resStream.CopyToAsync(stream.AsStreamForWrite());
                        }
                        response.Dispose();
                    }
                    CustomDialog customDialog = new CustomDialog("Message", "Profile's medias downloaded in folder \n" +
                    $"{userFolder.Path}", "All right");
                }
            }
            catch (Exception e)
            {
                CustomDialog customDialog = new CustomDialog("Warning", "Error. Wait while media loaded in profile. \n" +
                    $"Error - {e}", "All right");
            }
        }

        public static async Task DownloadPost(CustomMedia media)
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("jpeg image", new List<string>() { ".jpg" });
            savePicker.SuggestedFileName = media.Name;
            StorageFile file = await savePicker.PickSaveFileAsync();

            if (file != null)
            {
                var httpWebRequest = HttpWebRequest.CreateHttp(media.UrlBigImage);
                HttpWebResponse response = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
                Stream resStream = response.GetResponseStream();
                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await resStream.CopyToAsync(stream.AsStreamForWrite());
                }
                response.Dispose();

                CustomDialog customDialog = new CustomDialog("Message", "Post downloaded\n" +
                    $"{file.Path}", "All right");
            }
            else
            {
                CustomDialog customDialog = new CustomDialog("Warning", "Operation cancel."
                    , "All right");
            }
        }

        public static async Task DownloadMedia(CustomMedia media)
        {
            string url = "";
            if (media.MediaType == MediaType.Image)
                url = media.UrlBigImage;
            else if (media.MediaType == MediaType.Video)
                url = media.UrlVideo;

            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            if (media.MediaType == MediaType.Image)
                savePicker.FileTypeChoices.Add("jpeg image", new List<string>() { ".jpg" });
            else if (media.MediaType == MediaType.Video)
                savePicker.FileTypeChoices.Add("mp4 video", new List<string>() { ".mp4" });

            savePicker.SuggestedFileName = media.Name;
            StorageFile file = await savePicker.PickSaveFileAsync();

            if (file != null)
            {
                var httpWebRequest = HttpWebRequest.CreateHttp(url);
                HttpWebResponse response = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
                Stream resStream = response.GetResponseStream();
                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await resStream.CopyToAsync(stream.AsStreamForWrite());
                }
                response.Dispose();

                CustomDialog customDialog = new CustomDialog("Message", "Media downloaded\n" +
                    $"{file.Path}", "All right");
            }
            else
            {
                CustomDialog customDialog = new CustomDialog("Warning", "Operation cancel."
                    , "All right");
            }
        }
    }
}
