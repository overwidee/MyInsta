using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace MyInsta.Logic
{
    public class CustomDialog : ContentDialog 
    {
        ContentDialog ContentDialog { get; }
        public CustomDialog(string title, string content, string okButton)
        {
            ContentDialog = new ContentDialog()
            {
                Title = title,
                Content = content,
                CloseButtonText = okButton
            };
            _ = ContentDialog.ShowAsync();
        }
        
    }
}
