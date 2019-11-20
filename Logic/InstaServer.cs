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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MyInsta.Logic
{
    public static class InstaServer
    {
        public static string latestMediaMaxId = string.Empty;
        private static CancellationTokenSource cancellationTokenMedia;
        public delegate void CompleteHandler();


        #region Events

        public static event CompleteHandler OnUserFollowersLoaded;
        public static event CompleteHandler OnUserStoriesLoaded;
        public static event CompleteHandler OnUserSavedPostsLoaded;
        public static event CompleteHandler OnUserUnfollowersLoaded;
        public static event CompleteHandler OnUserFriendsLoaded;
        public static event CompleteHandler OnUserPostsLoaded;
        public static event CompleteHandler OnUserCollectionLoaded;

        #endregion

        #region Progress

        public static bool IsFollowersLoaded { get; set; }
        public static bool IsStoriesLoaded { get; set; }
        public static bool IsSavedPostsLoaded { get; set; }
        public static bool IsFriendsLoaded { get; set; }
        public static bool IsUnfollowersLoaded { get; set; }
        public static bool IsPostsLoaded { get; set; }

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

                    var logResult = await userObject.API.LoginAsync();
                    if (logResult.Succeeded)
                    {
                        ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                        localSettings.Values["Login"] = userObject.LoginUser;
                        localSettings.Values["Password"] = userObject.PasswordUser;

                        await SaveApiString(userObject.API);
                        page.Frame.Navigate(typeof(MenuPage), userObject);
                    }
                    else
                    {
                        if (logResult.Value == InstaLoginResult.ChallengeRequired)
                        {
                            page.Frame.Navigate(typeof(VerifyPage), userObject);
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


        public static async Task<bool> SendSMSVerify(IInstaApi api)
        {
            var challenge = await api.GetChallengeRequireVerifyMethodAsync();
            if (challenge.Value.StepData != null && challenge.Value.StepData.PhoneNumber != null)
            {
                var result = await api.RequestVerifyCodeToSMSForChallengeRequireAsync();
                if (result.Succeeded)
                    _ = new CustomDialog("Message", $"Code sent on email {challenge.Value.StepData.PhoneNumber}",
                        "All right");
                return result.Succeeded;
            }
            return false;
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
            if (result.Succeeded)
            {
                ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["Login"] = user.LoginUser;
                localSettings.Values["Password"] = user.PasswordUser;

                await SaveApiString(user.API);
                return user.API;
            }
            return null;
        }

        private static async Task<IInstaApi> AccountVerify(IInstaApi api)
        {
            var challenge = await api.GetChallengeRequireVerifyMethodAsync();
            if (challenge.Value.StepData != null && challenge.Value.StepData.PhoneNumber != null)
            {
                var request = await api.RequestVerifyCodeToSMSForChallengeRequireAsync();
                var dialog = new ContentDialog()
                {
                    Width = 500,
                    CloseButtonText = "Cancel",
                    PrimaryButtonText = "Send"
                };
                var stackPanel = new StackPanel()
                {
                    Orientation = Orientation.Vertical
                };
                var textBlock = new TextBlock()
                {
                    Text = "Write SMS code from phone",
                    Margin = new Windows.UI.Xaml.Thickness(10)
                };
                var inputTextBox = new TextBox()
                {
                    TextAlignment = Windows.UI.Xaml.TextAlignment.Center,
                    PlaceholderText = "Code",
                    Margin = new Windows.UI.Xaml.Thickness(10)
                };
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
            else if (challenge.Value.StepData != null && challenge.Value.StepData.Email != null)
            {
                var request = await api.RequestVerifyCodeToEmailForChallengeRequireAsync();
                var dialog = new ContentDialog()
                {
                    Width = 500,
                    CloseButtonText = "Cancel",
                    PrimaryButtonText = "Send"
                };
                var stackPanel = new StackPanel()
                {
                    Orientation = Orientation.Vertical
                };
                var textBlock = new TextBlock()
                {
                    Text = "Write code from email",
                    Margin = new Windows.UI.Xaml.Thickness(10)
                };
                var inputTextBox = new TextBox()
                {
                    TextAlignment = Windows.UI.Xaml.TextAlignment.Center,
                    PlaceholderText = "Code",
                    Margin = new Windows.UI.Xaml.Thickness(10)
                };
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
            else
                return false;
        }

        #endregion

        #region Main data

        public static async Task GetUserData(User userObject)
        {
            userObject.UserData.UserFollowers = new ObservableCollection<InstaUserShort>();
            userObject.UserData.SavedPostItems = new ObservableCollection<PostItem>();

            await GetCurrentUserStories(userObject);
            await GetUserPostItems(userObject);
            await GetBookmarksAsync(userObject);
            await GetUserFollowers(userObject);
            await GetUserFriendsAndUnfollowers(userObject, true);
        }

        private static async Task GetUserFollowers(User user)
        {
            var f = await user.API.UserProcessor.GetUserFollowersAsync(user.LoginUser,
                PaginationParameters.MaxPagesToLoad(5));
            foreach (var item in f.Value)
            {
                if (!user.UserData.UserFollowers.Contains(item))
                    user.UserData.UserFollowers.Add(item);
            }
            OnUserFollowersLoaded?.Invoke();
            IsFollowersLoaded = true;
        }

        private static async Task GetUserPostItems(User user)
        {
            var items = await user.API.FeedProcessor.GetSavedFeedAsync(PaginationParameters.MaxPagesToLoad(5));
            int i = 1;
            foreach (var itemS in items.Value)
            {
                PostItem savedPost = GetPostItem(itemS, i);
                i++;
                user.UserData.SavedPostItems.Add(savedPost);
            }
            OnUserSavedPostsLoaded?.Invoke();
            IsSavedPostsLoaded = true;
        }

        private static async Task GetUserFriendsAndUnfollowers(User user, bool all = false, int count = 30)
        {
            var fling = await user.API.UserProcessor.GetUserFollowingAsync(user.LoginUser,
                PaginationParameters.MaxPagesToLoad(5));
            if (all)
                count = fling.Value.Count;
            ////var statuses = await GetStatuses(user, fling.Value.Select(x => x.Pk).Take(count).ToArray());
            int i = 0;
            foreach (var item in fling.Value.Take(count))
            {
                var statusR = await user.API.UserProcessor.GetFriendshipStatusAsync(item.Pk);
                var status = statusR.Value;
                if (status.Following && status.FollowedBy && user.UserData.UserFriends.IndexOf(item) == -1)
                    user.UserData.UserFriends.Add(item);
                else
                if (status.Following && !status.FollowedBy && user.UserData.UserUnfollowers.IndexOf(item) == -1)
                    user.UserData.UserUnfollowers.Add(item);
                i++;
            }
            if (all)
            {
                OnUserUnfollowersLoaded?.Invoke();
                OnUserFriendsLoaded?.Invoke();
                IsUnfollowersLoaded = true;
                IsFriendsLoaded = true;
            }
        }

        private static async Task<List<InstaStoryFriendshipStatus>> GetStatuses(User user, params long[] ids)
        {
            var statuses = new List<InstaStoryFriendshipStatus>();
            for (int i = 0; i < ids.Length; i++)
            {
                var status = await user.API.UserProcessor.GetFriendshipStatusAsync(ids[0]);
                statuses.Add(status.Value);
            }
            return statuses;
        }

        #endregion

        #region UserProcessor

        public static async Task UnFollowFromList(User currentUser, ObservableCollection<InstaUserShort> instaUsers)
        {
            var customDialog = new CustomDialog("Message", "Process started", "All right");
            foreach (var item in instaUsers.Take(30))
            {
                var p = await currentUser.API.UserProcessor.UnFollowUserAsync(item.Pk);
            }
            await GetUserData(currentUser);
            customDialog = new CustomDialog("Message", "Un followed from all unfollowers", "All right");
        }

        public static async Task<InstaUserInfo> GetInfoUser(User userObject, string nick)
        {
            if (userObject != null)
            {
                IResult<InstaUserInfo> userInfo = await userObject.API.UserProcessor.GetUserInfoByUsernameAsync(nick);
                return userInfo.Value;
            }
            else
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
                    if (unF.Value.Following && unF.Value.FollowedBy
                        && !userObject.UserData.UserFriends.Contains(person))
                        userObject.UserData.UserFriends.Add(person);
                    else
                    if (unF.Value.Following && !unF.Value.FollowedBy
                        && !userObject.UserData.UserUnfollowers.Contains(person))
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

        public static IEnumerable<InstaUserShort> SearchByUserName(ObservableCollection<InstaUserShort> collection,
            string srt)
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

        #endregion

        #region Media

        public static async Task UnlikeProfile(User currentUser, InstaUserShort selectUser,
            ObservableCollection<PostItem> medias)
        {
            try
            {
                var customDialog = new CustomDialog("Message", "Process started", "All right");
                foreach (var item in medias?.Take(30))
                {
                    var p = await currentUser.API.MediaProcessor.UnLikeMediaAsync(item.Items[0].Pk);
                }
                customDialog = new CustomDialog("Message", $"30 posts of {selectUser.UserName} unlike", "All right");
            }
            catch (Exception e)
            {
                var dialog = new CustomDialog("Message", e.Message, "Ok");
            }
        }

        public static async Task<ObservableCollection<PostItem>> GetMediaUser(User userObject, InstaUserInfo unfUser,
            int count)
        {
            if (userObject != null && unfUser != null)
            {
                var media = new InstaMediaList();
                if (count == 0)
                {
                    var response = await userObject.API.UserProcessor.GetUserMediaAsync(unfUser.Username,
                        PaginationParameters.MaxPagesToLoad(5));
                    media = response.Value;
                }
                else
                    media = await GetMediaUserAll(userObject, unfUser);

                OnUserPostsLoaded?.Invoke();
                return GetUrlsMediasUser(media, unfUser);
            }
            else
                return null;
        }

        private static async Task<InstaMediaList> GetMediaUser(User userObject, InstaUserInfo unfUser)
        {
            var media = await userObject.API.UserProcessor.GetUserMediaAsync(unfUser.Username,
                PaginationParameters.MaxPagesToLoad(1).StartFromMaxId(latestMediaMaxId));
            latestMediaMaxId = media.Value.NextMaxId;
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
                        while (latestMediaMaxId != null);
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
                    MediaType = MediaType.Image
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
                        postItem.Items.Add(postCar);
                    }
                }
            }
            return postItem;
        }

        public static ObservableCollection<PostItem> GetUrlsMediasUser(InstaMediaList medias,
            InstaUserInfo instaUserInfo)
        {
            var mediaList = new ObservableCollection<PostItem>();
            int i = 0;

            foreach (var item in medias)
            {
                var postItem = new PostItem()
                {
                    Id = i + 1,
                    UserPk = instaUserInfo.Pk,
                    UserNamePost = instaUserInfo.Username,
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
                        MediaType = MediaType.Image
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
                    int x = 0;
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
                                CountComments = int.Parse(item.CommentsCount),
                                MediaType = MediaType.Image
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

        public static async void ShowComments(User user, PersonPage page, string mediaPk)
        {
            InstaCommentList comments = await GetCommentsByMediaId(user, mediaPk);
            var commentDialog = new CommentsPage(user, page, comments);
            if (comments.Comments == null || comments.Comments.Count == 0)
                return;
            await commentDialog.ShowAsync();
        }

        #endregion

        #region Messaging Porcessor

        private static async Task<bool> SharedInDirect(User userObject, string id, InstaMediaType mediaType,
            long idUser)
        {
            var shared = await userObject.API.MessagingProcessor.ShareMediaToUserAsync(id, mediaType, string.Empty,
                idUser);
            if (shared.Succeeded)
                return true;
            else
                return false;
        }

        public static async Task<bool> ShareMedia(User currentUser, ObservableCollection<CustomMedia> medias)
        {
            var contentShared = new ContentDialog()
            {
                PrimaryButtonText = "Send",
                SecondaryButtonText = "Cancel",
                Width = 1200
            };
            InstaMediaType mediaType = InstaMediaType.Image;
            CustomMedia media = medias[0];
            if (medias.Count > 1)
                mediaType = InstaMediaType.Carousel;
            else if (medias.Count == 1)
                mediaType = (media.MediaType == MediaType.Image) ? InstaMediaType.Image : InstaMediaType.Video;

            var frame = new Frame()
            {
                Width = 1000,
                Height = 400
            };
            frame.Navigate(typeof(SharedPage), new object[] { currentUser, media.MediaType, media.Pk });
            contentShared.Content = frame;
            var dialog = await contentShared.ShowAsync();
            if (dialog == ContentDialogResult.Primary)
            {
                var page = frame.Content as SharedPage;
                if (page.SelectedUser != null)
                {
                    var b = await InstaServer.SharedInDirect(currentUser, media.Pk, mediaType, page.SelectedUser.Pk);
                    if (b)
                        return true;
                }
                ;
            }
            return false;
        }

        #endregion

        #region Story

        public static async Task GetCurrentUserStories(User userObject)
        {
            var currentStories = await userObject.API.StoryProcessor.GetStoryFeedAsync();
            if (currentStories.Succeeded)
                userObject.UserData.Stories = GetUserStoriesCustom(currentStories.Value);
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
                if (stories.Succeeded)
                    return GetUrlsStoriesUser(stories.Value.Items);
                else
                    return null;
            }
            else
                return null;
        }

        public static async Task<ObservableCollection<CustomMedia>> GetStoryUser(User userObject, long userPk)
        {
            if (userObject != null)
            {
                var stories = await userObject.API.StoryProcessor.GetUserStoryAsync(userPk);
                if (stories.Succeeded)
                    return GetUrlsStoriesUser(stories.Value.Items);
                else
                    return null;
            }
            else
                return null;
        }

        public static ObservableCollection<CustomMedia> GetUrlsStoriesUser(List<InstaStoryItem> stories)
        {
            var storiesList = new ObservableCollection<CustomMedia>();
            int i = 0;
            foreach (var story in stories)
            {
                var custM = new CustomMedia()
                {
                    Pk = story.Pk.ToString(),
                    Name = $"{story.User.UserName}_story_{story.Pk}",
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

        public static async Task<InstaHighlightFeeds> GetArchiveCollectionStories(User instaUser, long userId)
        {
            var collection = await instaUser.API.StoryProcessor.GetHighlightFeedsAsync(userId);
            return collection.Value;
        }

        public static async Task<ObservableCollection<CustomMedia>> GetHighlightStories(User instaUser,
            string highlightId)
        {
            var stories = await instaUser.API.StoryProcessor.GetHighlightMediasAsync(highlightId);
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

        public static async Task DownloadCarousel(ObservableCollection<CustomMedia> images, InstaUserShort instaUser)
        {
            try
            {
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                folderPicker.FileTypeFilter.Add("*");

                Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                bool? result = null;
                if (folder != null)
                {
                    var userFolder = await folder.CreateFolderAsync(instaUser.UserName,
                        CreationCollisionOption.ReplaceExisting);

                    foreach (var item in images)
                    {
                        StorageFile coverpic_file;
                        string urlForSave = string.Empty;
                        if (item.MediaType == MediaType.Image)
                        {
                            coverpic_file = await userFolder.CreateFileAsync($"{item.Name}.jpg",
                                CreationCollisionOption.FailIfExists);
                            urlForSave = item.UrlBigImage;
                        }
                        else
                        {
                            coverpic_file = await userFolder.CreateFileAsync($"{item.Name}.mp4",
                                CreationCollisionOption.FailIfExists);
                            urlForSave = item.UrlVideo;
                        }
                        var task = Task.Run(async () =>
                        {
                            var webRequest = WebRequest.CreateHttp(urlForSave);
                            var webResponse = await Task.Factory.FromAsync(webRequest.BeginGetResponse,
                                webRequest.EndGetResponse, null);
                            using (var responseStream = webResponse.GetResponseStream())
                            {
                                using (var resultFileStream = await coverpic_file.OpenStreamForWriteAsync())
                                {
                                    await responseStream.CopyToAsync(resultFileStream)
                                              .ContinueWith((e) => { result = e.IsCompletedSuccessfully; });
                                }
                            }
                        });
                        task.Wait();
                    }
                }
                var customDialog = new CustomDialog("Message", "Post/s downloaded", "All right");
            }
            catch (Exception e)
            {
                var customDialog = new CustomDialog("Warning",
                    "Error. Wait while media loaded in profile. \n" + $"Error - {e}", "All right");
            }
        }

        public static async Task DownloadPost(CustomMedia media)
        {
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("jpeg image", new List<string>() { ".jpg" });
            savePicker.SuggestedFileName = media.Name;
            StorageFile file = await savePicker.PickSaveFileAsync();

            if (file != null)
            {
                var httpWebRequest = HttpWebRequest.CreateHttp(media.UrlBigImage);
                var response = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
                Stream resStream = response.GetResponseStream();
                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await resStream.CopyToAsync(stream.AsStreamForWrite());
                }
                response.Dispose();

                var customDialog = new CustomDialog("Message", "Post downloaded\n" + $"{file.Path}", "All right");
            }
            else
            {
                var customDialog = new CustomDialog("Warning", "Operation cancel.", "All right");
            }
        }

        public static async Task<bool> DownloadMedia(CustomMedia media)
        {
            string url = string.Empty;
            if (media.MediaType == MediaType.Image)
                url = media.UrlBigImage;
            else if (media.MediaType == MediaType.Video)
                url = media.UrlVideo;

            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            if (media.MediaType == MediaType.Image)
                savePicker.FileTypeChoices.Add("jpeg image", new List<string>() { ".jpg" });
            else if (media.MediaType == MediaType.Video)
                savePicker.FileTypeChoices.Add("mp4 video", new List<string>() { ".mp4" });

            savePicker.SuggestedFileName = media.Name;
            StorageFile file = await savePicker.PickSaveFileAsync();

            bool? result = null;
            if (file != null)
            {
                var task = Task.Run(async() =>
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
            if (result.Value)
                _ = new CustomDialog("Message", "Media downloaded\n", "All right");
            else
                _ = new CustomDialog("Message", "Failed\n", "All right");
            return result.Value;
        }

        private static async Task Pause()
        {
            await Task.Delay(1000);
        }

        public static async Task DownloadAnyPost(InstaUserShort selectedUser, ObservableCollection<CustomMedia> medias)
        {
            if (medias.Count > 1)
                await DownloadCarousel(medias, selectedUser);
            else if (medias.Count == 1)
            {
                var b = await DownloadMedia(medias[0]);
            }
        }

        public static async Task DownloadAnyPosts(InstaUserShort selectedUser, ObservableCollection<PostItem> medias)
        {
            try
            {
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                folderPicker.FileTypeFilter.Add("*");

                Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    var userFolder = await folder.CreateFolderAsync(selectedUser.UserName,
                        CreationCollisionOption.ReplaceExisting);
                    foreach (var post in medias)
                    {
                        foreach (var item in post.Items)
                        {
                            StorageFile coverpic_file;
                            string urlForSave = string.Empty;
                            if (item.MediaType == MediaType.Image)
                            {
                                coverpic_file = await userFolder.CreateFileAsync($"{item.Name}.jpg",
                                    CreationCollisionOption.FailIfExists);
                                urlForSave = item.UrlBigImage;
                            }
                            else
                            {
                                coverpic_file = await userFolder.CreateFileAsync($"{item.Name}.mp4",
                                    CreationCollisionOption.FailIfExists);
                                urlForSave = item.UrlVideo;
                            }
                            var httpWebRequest = WebRequest.CreateHttp(urlForSave);
                            var response = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
                            Stream resStream = response.GetResponseStream();
                            using (var stream = await coverpic_file.OpenAsync(FileAccessMode.ReadWrite))
                            {
                                await resStream.CopyToAsync(stream.AsStreamForWrite());
                            }
                            response.Dispose();
                        }
                    }
                }
                var customDialog = new CustomDialog("Message", "Posts downloaded", "All right");
            }
            catch (Exception e)
            {
                var customDialog = new CustomDialog("Warning",
                    "Error. Wait while media loaded in profile. \n" + $"Error - {e}", "All right");
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

        public async static Task<InstaCollections> GetListCollections(User instaUser)
        {
            var collects = await instaUser.API.CollectionProcessor.GetCollectionsAsync(PaginationParameters.Empty);
            return collects.Value;
        }

        public async static Task<IEnumerable<PostItem>> GetMediasByCollection(User instaUser,
            InstaCollectionItem instaCollection)
        {
            var mediaC = await instaUser.API.CollectionProcessor.GetSingleCollectionAsync(instaCollection.CollectionId,
                PaginationParameters.MaxPagesToLoad(1));
            var mediasIds = mediaC.Value.Media.Select(x => new
            {
                x.Pk
            }).ToList();
            OnUserCollectionLoaded?.Invoke();
            return instaUser.UserData.SavedPostItems
                       .Where(sItem => mediasIds.Where(x => x.Pk == sItem.Items[0].Pk).ToList().Count != 0)
                       .Select(sItem => sItem);
        }

        #endregion

        #region Feed

        private static async void GetFeed(User user)
        {
            var b = await user.API.FeedProcessor.GetLikedFeedAsync(PaginationParameters.MaxPagesToLoad(10));
            var a = await user.API.FeedProcessor.GetUserTimelineFeedAsync(PaginationParameters.MaxPagesToLoad(10),
                new[] { "2171160902940863145" });
        }

        #endregion

        #region Direct

        public static async Task<ObservableCollection<InstaDirectInboxThread>> GetDirectDialogsAsync(User user)
        {
            IResult<InstaDirectInboxContainer> a = await user.API.MessagingProcessor
                                                             .GetDirectInboxAsync(PaginationParameters.MaxPagesToLoad(1));
            return new ObservableCollection<InstaDirectInboxThread>(a.Value.Inbox.Threads);
        }

        public static async Task<ObservableCollection<InstaDirectInboxItem>> GetDialogThreadAsync(User user,
            string threadId)
        {
            var b = await user.API.MessagingProcessor.GetDirectInboxThreadAsync(threadId,
                PaginationParameters.MaxPagesToLoad(1));
            long last = 0;
            var curr = await user.API.GetCurrentUserAsync();
            foreach (var item in b.Value.Items)
            {
                if (last != item.UserId)
                {
                    last = item.UserId;
                    if (last == curr.Value.Pk)
                        item.ProfileMedia = new InstaUserShort()
                        {
                            ProfilePicUrl = curr.Value.ProfilePicture
                        };
                    else
                        item.ProfileMedia = new InstaUserShort()
                        {
                            ProfilePicUrl = b.Value.Users[0].ProfilePicUrl
                        };
                }
                else
                    item.ProfileMedia = new InstaUserShort()
                    {
                        ProfilePicUrl = ""
                    };
            }
            return new ObservableCollection<InstaDirectInboxItem>(b.Value.Items);
        }

        #endregion
    }
}
