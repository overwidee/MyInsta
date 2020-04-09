using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications; // Notifications library
using Microsoft.QueryStringDotNET;

namespace MyInsta.Logic
{
    public class CustomDialog : ContentDialog
    {
        public CustomDialog(string title, string content, string okButton, string mediaPk = "")
        {
            var visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = title
                        },

                        new AdaptiveText()
                        {
                            Text = content
                        },
                        new AdaptiveImage()
                        {
                            Source = mediaPk
                        }
                    },

                    AppLogoOverride = new ToastGenericAppLogo()
                    {
                        Source = "/Assets/instagram-notif.png",
                        HintCrop = ToastGenericAppLogoCrop.Circle
                    }
                }
            };

            var conversationId = 384928;

            var toastContent = new ToastContent()
            {
                Visual = visual,
                Actions = new ToastActionsCustom(),

                Launch = new QueryString()
                {
                    { "action", "viewConversation" },
                    { "conversationId", conversationId.ToString() }

                }.ToString(),
                Scenario = ToastScenario.Alarm
            };

            var toast = new ToastNotification(toastContent.GetXml());

            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

    }
}
