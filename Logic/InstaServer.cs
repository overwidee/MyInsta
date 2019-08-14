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

namespace MyInsta.Logic
{
    public static class InstaServer
    {
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
                        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                        StorageFile sampleFile = await localFolder.CreateFileAsync("dataFile.txt",
                            CreationCollisionOption.ReplaceExisting);
                        await FileIO.WriteTextAsync(sampleFile, api.GetStateDataAsString());
                        return api;
                    }
                }
                return null;
            }
            return null;
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
            var f = await userObject.API.UserProcessor.GetUserFollowersAsync(userObject.LoginUser, PaginationParameters.MaxPagesToLoad(1));
            foreach (var item in f.Value)
            {
                if (!userObject.UserData.UserFollowers.Contains(item))
                    userObject.UserData.UserFollowers.Add(item);
            }
            var fling = await userObject.API.UserProcessor.GetUserFollowingAsync(userObject.LoginUser, PaginationParameters.MaxPagesToLoad(1));
            foreach (var item in fling.Value)
            {
                var status = await userObject.API.UserProcessor.GetFriendshipStatusAsync(item.Pk);
                if (status.Value.Following && status.Value.FollowedBy && !userObject.UserData.UserFriends.Contains(item))
                    userObject.UserData.UserFriends.Add(item);
                else
                if (status.Value.Following && !status.Value.FollowedBy && !userObject.UserData.UserUnfollowers.Contains(item))
                    userObject.UserData.UserUnfollowers.Add(item);
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

        public static async Task<ObservableCollection<CustomMedia>> GetMediaUser(User userObject, InstaUserInfo unfUser)
        {
            if (userObject != null && unfUser != null)
            {
                var media = await userObject.API.UserProcessor.GetUserMediaAsync(unfUser.Username, PaginationParameters.MaxPagesToLoad(1));
                if (media.Succeeded)
                    return GetUrlsMediasUser(media.Value);
                else
                    return null;
            }
            else
                return null;
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

        public static ObservableCollection<CustomMedia> GetUrlsMediasUser(InstaMediaList medias)
        {
            ObservableCollection<CustomMedia> mediaList = new ObservableCollection<CustomMedia>();
            int i = 0;
            foreach (var item in medias)
            {
                if (item.Images != null && item.Images.Count != 0)
                    mediaList.Add(new CustomMedia()
                    {
                        Name = $"ImagePost_{i + 1}",
                        UrlSmallImage = item.Images[1].Uri,
                        UrlBigImage = item.Images[0].Uri,
                        CountLikes = item.LikesCount,
                        CountComments = int.Parse(item.CommentsCount)
                    });
                else if (item.Carousel != null && item.Carousel.Count != 0)
                {
                    int x = 0;
                    foreach (var car in item.Carousel)
                    {
                        x++;
                        if (car.Images != null && car.Images.Count != 0)
                            mediaList.Add(new CustomMedia()
                            {
                                Name = $"ImagePost_{i + 1}_Carousel_{x + 1}",
                                UrlSmallImage = car.Images[0].Uri,
                                UrlBigImage = car.Images[1].Uri,
                                CountLikes = item.LikesCount,
                                CountComments = int.Parse(item.CommentsCount)
                            });
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

        public static async Task UnlikeProfile(User currentUser, InstaUserShort selectUser)
        {
            CustomDialog customDialog = new CustomDialog("Message", "Process started", "All right");
            var mediaUser = await currentUser.API.UserProcessor.GetUserMediaByIdAsync(selectUser.Pk, PaginationParameters.MaxPagesToLoad(100));
            foreach (var item in mediaUser.Value)
            {
                var p = await currentUser.API.MediaProcessor.UnLikeMediaAsync(item.Pk);
            }
            customDialog = new CustomDialog("Message", $"Profile ({selectUser.UserName}) unliked", "All right");
        }

        public static async Task UnFollowFromList(User currentUser, ObservableCollection<InstaUserShort> instaUsers)
        {
            CustomDialog customDialog = new CustomDialog("Message", "Process started", "All right");
            foreach (var item in instaUsers)
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

        public static async Task DownloadStory(CustomMedia media)
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

                CustomDialog customDialog = new CustomDialog("Message", "Story downloaded\n" +
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
