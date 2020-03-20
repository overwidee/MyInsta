using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using InstagramApiSharp.Classes.Models;
using MyInsta.Logic;
using MyInsta.Model;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MyInsta.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CommentsPage : ContentDialog
    {
        public CommentsPage(User user, Page personPage, InstaCommentList commentList)
        {
            this.InitializeComponent();

            SetComments(commentList);
            InstaUser = user;
            PersonPage = personPage;
        }

        public ObservableCollection<Comment> CommentList { get; set; } = new ObservableCollection<Comment>();
        public User InstaUser { get; set; }
        public Page PersonPage { get; set; }

        public new async Task ShowAsync()
        {
            await dialog.ShowAsync();
        }

        private void SetComments(InstaCommentList commentList)
        {
            foreach (var item in commentList.Comments)
            {
                CommentList.Add(new Comment()
                    {
                        UserId = item.User.Pk,
                        UserName = item.User.UserName,
                        UserUrl = item.User.ProfilePicUrl,
                        CommentText = item.Text
                    });
                foreach (var child in item.PreviewChildComments)
                {
                    CommentList.Add(new Comment()
                        {
                            UserId = child.User.Pk,
                            UserName = child.User.UserName,
                            UserUrl = child.User.ProfilePicUrl,
                            CommentText = child.Text
                        });
                }
            }
            listComments.ItemsSource = CommentList;
        }


        private async void ListComments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedComment = e.AddedItems[0] as Comment;

            dialog.Hide();
            PersonPage.Frame.Navigate(typeof(PersonPage),
                new object[] { await InstaServer.GetInstaUserShortById(InstaUser, selectedComment.UserId), InstaUser });
        }
    }


    public class Comment
    {
        public long UserId { get; set; }
        public string UserUrl { get; set; }
        public string UserName { get; set; }
        public string CommentText { get; set; }
    }
}
