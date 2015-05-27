using System;
using Disa.Framework.Mobile;
using Xamarin.Forms;

namespace Disa.Framework.Telegram.Mobile
{
    [PluginSettingsUI(typeof(Telegram))]
    public class Settings : IPluginPage
    {
        public Page Fetch(Service service)
        {
            NavigationPage navigationPage;
//            if (ServiceManager.IsManualSettingsNeeded(service))
//            {
//                navigationPage = new NavigationPage(Setup.Fetch(service));
//            }
//            else 
            {
                navigationPage = new NavigationPage(new Main(service));
            }
            return navigationPage;
        }

        private class Main : ContentPage
        {
            private Label _status;
            private Label _info;
            private Button _activate;
            private Button _setupWizard;
            private Button _profileSettings;
            private StackLayout _toolsLayout;

            public Main(Service service) 
            {
                var running = ServiceManager.IsRunning(service);
                var internetConnected = Platform.HasInternetConnection();

                _status = new Label();
                if (running)
                {
                    _status.Text = Localize.GetString("TelegramRunning");
                    _status.TextColor = Color.Green;
                }
                else
                {
                    _status.Text = Localize.GetString("TelegramNotRunning");
                    _status.TextColor = Color.Red;
                }
                _status.Font = Font.SystemFontOfSize(20);
                _status.XAlign = TextAlignment.Center;

                _info = new Label();
                if (running)
                {
                    _info.Text = Localize.GetString("TelegramAllGood");
                }
                else
                {
                    if (internetConnected)
                    {
                        _info.Text = Localize.GetString("TelegramPleaseStart");
                    }
                    else
                    {
                        _info.Text = Localize.GetString("TelegramPleaseStartNoInternet");
                    }
                }
                _info.Font = Font.SystemFontOfSize(16);
                _info.XAlign = TextAlignment.Center;

                _toolsLayout = new StackLayout();
                _toolsLayout.Orientation = StackOrientation.Vertical;
                _toolsLayout.VerticalOptions = LayoutOptions.EndAndExpand;
                _toolsLayout.Spacing = 10;
                _setupWizard = new Button();
                _setupWizard.Text = Localize.GetString("TelegramSetupWizard");
                _setupWizard.TextColor = Color.White;
                _setupWizard.BackgroundColor = Color.FromHex("c50923");
                _setupWizard.Clicked += async (sender, e) =>
                    {
                        //await Navigation.PushAsync(Setup.Fetch(service));
                    };
                if (running)
                {
                    _profileSettings = new Button();
                    _profileSettings.Text = Localize.GetString("TelegramProfileSettings");
                    _profileSettings.TextColor = Color.White;
                    _profileSettings.BackgroundColor = Color.FromHex("77D065");
                    _profileSettings.Clicked += async (sender, e) =>
                        {
                            //await Navigation.PushAsync(new ProfileSettings(service));
                        };
                    _toolsLayout.Children.Add(_profileSettings);
                }
                _toolsLayout.Children.Add(_setupWizard);

                var stackLayout = new StackLayout();
                stackLayout.Spacing = 10;
                stackLayout.Padding = 25;
                stackLayout.VerticalOptions = LayoutOptions.FillAndExpand;
                var children = stackLayout.Children;
                children.Add(_status);
                children.Add(_info);
                children.Add(_toolsLayout);


                Content = new ScrollView { Content = stackLayout };
                Title = Localize.GetString("TelegramSettingsTitle");
            }
        }
    }
}

