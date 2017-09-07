using System;
using System.Collections.Generic;
using Disa.Framework.Mobile;
using Xamarin.Forms;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Linq;
using SharpTelegram.Schema;
using SharpMTProto.Transport;
using SharpTelegram;
using SharpMTProto;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using Xamarin;
using Disa.Framework;

namespace Disa.Framework.Telegram.Mobile
{
    public static class Setup 
    {
        private static readonly string TelegramPackage = "org.telegram.messenger";

        private static Page _cachedPage;
        private static TelegramSetupSettings _settings;

        [Serializable]
        public class TelegramSetupSettings : DisaMutableSettings
        {
            public class State
            {
                public string NationalNumber { get; set; }

                public string CodeHash { get; set; }

                public bool Registered { get; set; }

                public TelegramSettings Settings { get; set; }
            }

            public List<State> States { get; private set; }

            public TelegramSetupSettings()
            {
                States = new List<State>();
            }
        }

		//Setup exception class for Xamarin Insights Exception
		public class SetupException : Exception
		{ 
			public SetupException()
			{
		    }

		    public SetupException(string message)
		        : base(message)
		    {
			}

			public SetupException(string message, Exception inner)
		        : base(message, inner)
		    {
			}
		}

        private static Task<ActivationResult> Register(Service service, string nationalNumber, string countryCode, string code, string firstName = null, string lastName = null)
        {
            return Task<ActivationResult>.Factory.StartNew(() =>
                {
                    var result = Telegram.RegisterCode(service, GetSettingsTelegramSettings(nationalNumber), countryCode + nationalNumber,
                                                       GetSettingsCodeHash(nationalNumber), code, firstName, lastName, GetSettingsRegistered(nationalNumber));

                    if (result == null)
                    {
                        return new ActivationResult
                        {
                            Success = false,
                            ErrorMessage = Localize.GetString("TelegramCodeError")
                        };
                    }

                    if (result.Response != Telegram.CodeRegister.Type.Success)
                    {
                        string errorMessage = null;
                        var info = new PasswordInformation();
                        switch (result.Response)
                        {
                            case Telegram.CodeRegister.Type.NumberInvalid:
                                errorMessage = Localize.GetString("TelegramCodeNumberInvalid");
                                break;
                            case Telegram.CodeRegister.Type.CodeEmpty:
                                errorMessage = Localize.GetString("TelegramCodeInvalidMessage");
                                break;
                            case Telegram.CodeRegister.Type.CodeExpired:
                                errorMessage = Localize.GetString("TelegramCodeCodeExpired");
                                break;
                            case Telegram.CodeRegister.Type.CodeInvalid:
                                errorMessage = Localize.GetString("TelegramCodeCodeInvalid");
                                break;
                            case Telegram.CodeRegister.Type.FirstNameInvalid:
                                errorMessage = Localize.GetString("TelegramCodeFirstNameInvalid");
                                break;
                            case Telegram.CodeRegister.Type.LastNameInvalid:
                                errorMessage = Localize.GetString("TelegramCodeLastNameInvalid");
                                break;
                            case Telegram.CodeRegister.Type.PasswordNeeded:
                                info.CurrentSalt = result.CurrentSalt;
                                info.HasRecovery = result.HasRecovery;
                                info.Hint = result.Hint;
                                info.NewSalt = result.NewSalt;
                                break;
                            default:
                                errorMessage = Localize.GetString("TelegramCodeError");
                                break;
                        }
                        return new ActivationResult
                        {
                            Success = false,
                            ErrorMessage = errorMessage,
                            PasswordNeeded = result.Response == Telegram.CodeRegister.Type.PasswordNeeded,
                            PasswordInformation = info
                        };
                    }

                    return new ActivationResult
                    {
                        Success = true,
                        AccountId = result.AccountId,
                    };
                });
        }


        private static bool GetSettingsRegistered(string nationalNumber)
        {
            var state = _settings.States.FirstOrDefault(x => x.NationalNumber == nationalNumber);
            if (state == null)
                return false;
            return state.Registered;
        }

        private static  TelegramSettings GetSettingsTelegramSettings(string nationalNumber)
        {
            var state = _settings.States.FirstOrDefault(x => x.NationalNumber == nationalNumber);
            if (state == null)
                return null;
            return state.Settings;
        }

        private static string GetSettingsCodeHash(string nationalNumber)
        {
            var state = _settings.States.FirstOrDefault(x => x.NationalNumber == nationalNumber);
            if (state == null)
                return null;
            return state.CodeHash;
        }

        private static void SetSettingsCodeHash(string codeHash, bool save, string nationalNumber)
        {
            var state = _settings.States.FirstOrDefault(x => x.NationalNumber == nationalNumber);
            if (state == null)
                return;
            state.CodeHash = codeHash;
            if (save)
            {
                SaveSettings();
            }
        }

        private static void SetSettingsSettings(TelegramSettings newSettings, bool save, string nationalNumber)
        {
            var state = _settings.States.FirstOrDefault(x => x.NationalNumber == nationalNumber);
            if (state == null)
                return;
            state.Settings = newSettings;
            if (save)
            {
                SaveSettings();
            }
        }

        private static void SetSettingsRegistered(bool registered, bool save, string nationalNumber)
        {
            var state = _settings.States.FirstOrDefault(x => x.NationalNumber == nationalNumber);
            if (state == null)
                return;
            state.Registered = registered;
            if (save)
            {
                SaveSettings();
            }
        }

        private static void Save(Service service, uint accountId, TelegramSettings settings)
        {
            try
            {
                settings.AccountId = accountId;

                SettingsManager.Save(service, settings);

                if (ServiceManager.IsRunning(service))
                {
                    Utils.DebugPrint("Service is running. Aborting....");
                    ServiceManager.Abort(service).Wait();
                }

                Utils.DebugPrint("Starting the service...!");
				Insights.Report(new SetupException("Telegram Setup Ended"));
                ServiceManager.Start(service, true);

                _cachedPage = null;
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Failed to save the Telegram service: " + ex);
            }
        }

        private class ActivationResult
        {
            public enum ActivationType
            {
                Telegram,
                Text,
                PhoneCall,
                Unknown
            }
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public uint AccountId { get; set; }
            public ActivationType CurrentType { get; set; }
            public ActivationType NextType { get; set; }
            public bool PasswordNeeded { get; set; }
            public PasswordInformation PasswordInformation { get; set; }
        }

        private class RecoverResult
        {
            public string EmailPattern { get; set; }
        }

        private class PasswordInformation
        { 
            public byte[] CurrentSalt { get; set; }
            public byte[] NewSalt { get; set; }
            public string Hint { get; set; }
            public bool HasRecovery { get; set; }    
        }

        private static void SaveSettings()
        {
            MutableSettingsManager.Save(_settings);
        }

        private static void LoadSettingsIfNeeded()
        {
            if (_settings != null)
            {
                return;
            }
            _settings = MutableSettingsManager.Load<TelegramSetupSettings>();
        }

        public static Page Fetch(Service service)
        {
            if (!Platform.HasInternetConnection())
            {
                return new ContentPage
                { 
                    Content = new Label
                        {
                            Text = Localize.GetString("TelegramPleaseConnectToInternet"),
                            VerticalOptions = LayoutOptions.CenterAndExpand,
                            HorizontalOptions = LayoutOptions.CenterAndExpand,
                            XAlign = TextAlignment.Center,
                            Font = Font.SystemFontOfSize(18),
                        },
                    Title = Localize.GetString("TelegramSetupWizardTitle"),
                    Padding = 30,
                };
            }

            LoadSettingsIfNeeded();

            if (_cachedPage != null)
            {
                return _cachedPage;
            }

            var tabs = new TabbedPage();
            var userInformation = new UserInformation(service, tabs);
            var passwordCode = new PasswordCode(service, tabs, userInformation);
            var password = new Password(service, tabs, passwordCode);
            var code = new Code(service, tabs, password, userInformation);
            var info = new Info(service, tabs, code);

            tabs.Children.Add(info);
			var help = new ToolbarItem();
			help.Text = Localize.GetString("TelegramHelp");
			help.Order = ToolbarItemOrder.Primary;
			help.Clicked += (sender, e) =>
			{
                Platform.LaunchViewIntent("http://www.disa.im/faq.html?s=telegram");
            };
			tabs.ToolbarItems.Add(help);
            tabs.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == "CurrentPage")
                    {
                        var selected = tabs.CurrentPage;
                        if (selected is Info)
                        {
                            tabs.Children.Remove(code);
                            tabs.Children.Remove(password);
                            tabs.Children.Remove(passwordCode);
                            tabs.Children.Remove(userInformation);
                        }
                        if (selected is Code)
                        {
                            tabs.Children.Remove(password);
                            tabs.Children.Remove(passwordCode);
                            tabs.Children.Remove(userInformation);
                        }
                        if (selected is Password)
                        {
                            tabs.Children.Remove(passwordCode);
                            tabs.Children.Remove(userInformation);
                        }
                        if (selected is PasswordCode)
                        {
                            tabs.Children.Remove(userInformation);
                        }
                    }
                };

            tabs.Title = Localize.GetString("TelegramSetupWizardTitle");
            _cachedPage = tabs;
			Insights.Report(new SetupException("Telegram Setup Started"));
            return tabs;
        }


        private class UserInformation : ContentPage
        {
            private Entry _firstName;
            private Entry _lastName;    
            private Button _finish;
            private Label _error;

            public string CountryCode { get; set; }
            public string NationalNumber { get; set; }
            public string Code { get; set; }

            private ActivityIndicator _progressBar;

            private Service _service;

            public UserInformation(Service service, TabbedPage tabs)
            {
                _service = service;

                _firstName = new Entry();
                _firstName.Placeholder = Localize.GetString("TelegramFirstName");

                _lastName = new Entry();
                _lastName.Placeholder = Localize.GetString("TelegramLastName");

                _finish = new Button();
                _finish.Text = Localize.GetString("TelegramFinish");
                _finish.TextColor = Color.White;
                _finish.BackgroundColor = Color.FromHex("77D065");
                _finish.Clicked += async (sender, e) =>
                {
                    _firstName.IsEnabled = false;
                    _lastName.IsEnabled = false;
                    _finish.IsEnabled = false;
                    _progressBar.IsVisible = true;
                    DependencyService.Get<IPluginPageControls>().BackPressEnabled = false;
                    tabs.IsEnabled = false;
                    var firstName = _firstName.Text;
                    var lastName = _lastName.Text;

                    if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                    {
                        await DisplayAlert(Localize.GetString("TelegramCodeInvalidTitle"),
                                    Localize.GetString("TelegramInvalidCodeDetail"), Localize.GetString("TelegramOkay"));
                        return;
                    }

                    SetSettingsRegistered(false, true, NationalNumber);

                    var result = await Register(service, NationalNumber, CountryCode, Code, firstName, lastName);

                    _firstName.IsEnabled = true;
                    _lastName.IsEnabled = true;
                    _finish.IsEnabled = true;
                    _progressBar.IsVisible = false;
                    DependencyService.Get<IPluginPageControls>().BackPressEnabled = true;
                    tabs.IsEnabled = true;

                    if (result.Success)
                    {
						CleanUp(service);
                        Save(service, result.AccountId, GetSettingsTelegramSettings(NationalNumber));
                        if (Platform.DeviceHasApp(TelegramPackage))
                        {
                            await Navigation.PushAsync(new MuteNotifications());
                        }
                        else
                        {
                            await Navigation.PopToRootAsync();
                            DependencyService.Get<IPluginPageControls>().Finish();
                        }
                    }
                    else
                    {
                        _error.Text = result.ErrorMessage;
                    }
                };

                _error = new Label();
                _error.VerticalOptions = LayoutOptions.EndAndExpand;
                _error.IsVisible = false;
                _error.Font = Font.SystemFontOfSize(18);
                _error.XAlign = TextAlignment.Center;
                _error.TextColor = Color.Red;

                _progressBar = new ActivityIndicator();
                _progressBar.VerticalOptions = LayoutOptions.CenterAndExpand;
                _progressBar.IsRunning = true;
                _progressBar.IsVisible = false;

                var stackLayout = new StackLayout();
                stackLayout.Spacing = 20;
                stackLayout.Padding = 25;
                stackLayout.VerticalOptions = LayoutOptions.Start;
                var children = stackLayout.Children;
                children.Add(_firstName);
                children.Add(_lastName);
                children.Add(_finish);
                children.Add(_error);
                children.Add(_progressBar);

                Content = new ScrollView { Content = stackLayout };
                Title = Localize.GetString("TelegramUserInformationTitle");
            }
        }



        private class PasswordCode : ContentPage
        {
            private Label _passwordCodeLabel;
            private Entry _code;
            private Button _finish;
            private Label _error;
            private Button _resetAccount;

            public string CountryCode { get; set; }
            public string NationalNumber { get; set; }
            private string _emailPattern;
            public string EmailPattern 
            {   get
                {
                    return _emailPattern;
                }
                set 
                {
                    _emailPattern = value;
                    _passwordCodeLabel.Text = Localize.GetString("TelegramEnterCodeSentToEmail").Replace("[emailPattern]", _emailPattern);
                }
            }
            public string Code { get; set; }

            private ActivityIndicator _progressBar;

            private Service _service;
            UserInformation userInformation;

            public PasswordCode(Service service, TabbedPage tabs, UserInformation userInformation)
            {
                _service = service;

                _passwordCodeLabel = new Label();
                _passwordCodeLabel.VerticalOptions = LayoutOptions.CenterAndExpand;
                _passwordCodeLabel.Text = Localize.GetString("TelegramEnterCodeSentToEmail").Replace("[emailPattern]", EmailPattern);
                _passwordCodeLabel.IsVisible = true;
                _passwordCodeLabel.Font = Font.SystemFontOfSize(18);
                _passwordCodeLabel.XAlign = TextAlignment.Center;

                _code = new Entry();
                _code.Placeholder = Localize.GetString("TelegramCode");

                _finish = new Button();
                _finish.Text = Localize.GetString("TelegramFinish");
                _finish.TextColor = Color.White;
                _finish.BackgroundColor = Color.FromHex("77D065");
                _finish.Clicked += async (sender, e) =>
                {
                    _code.IsEnabled = false;
                    _finish.IsEnabled = false;
                    _progressBar.IsVisible = true;
                    DependencyService.Get<IPluginPageControls>().BackPressEnabled = false;
                    tabs.IsEnabled = false;
                    var code = _code.Text;

                    if (string.IsNullOrEmpty(code))
                    {
                        await DisplayAlert(Localize.GetString("TelegramCodeInvalidTitle"),
                                    Localize.GetString("TelegramInvalidCodeDetail"), Localize.GetString("TelegramOkay"));
                        return;
                    }

                    var result = await VerifyCode(code);

                    _code.IsEnabled = true;
                    _finish.IsEnabled = true;
                    _progressBar.IsVisible = false;
                    DependencyService.Get<IPluginPageControls>().BackPressEnabled = true;
                    tabs.IsEnabled = true;

                    if (result.Success)
                    {
						CleanUp(service);
                        Save(service, result.AccountId, GetSettingsTelegramSettings(NationalNumber));
                        if (Platform.DeviceHasApp(TelegramPackage))
                        {
                            await Navigation.PushAsync(new MuteNotifications());
                        }
                        else
                        {
                            await Navigation.PopToRootAsync();
                            DependencyService.Get<IPluginPageControls>().Finish();
                        }
                    }
                    else
                    {
                        _error.Text = result.ErrorMessage;
                    }
                };

                _error = new Label();
                _error.VerticalOptions = LayoutOptions.EndAndExpand;
                _error.IsVisible = false;
                _error.Font = Font.SystemFontOfSize(18);
                _error.XAlign = TextAlignment.Center;
                _error.TextColor = Color.Red;

                _resetAccount = new Button();
                _resetAccount.VerticalOptions = LayoutOptions.EndAndExpand;
                _resetAccount.Text = Localize.GetString("TelegramCannotAccessEmail");
                _resetAccount.TextColor = Color.White;
                _resetAccount.BackgroundColor = Color.FromHex("77D065");
                _resetAccount.Clicked += async (sender, e) =>
                {
                    _code.IsEnabled = false;
                    _finish.IsEnabled = false;
                    _progressBar.IsVisible = true;
                    DependencyService.Get<IPluginPageControls>().BackPressEnabled = false;
                    tabs.IsEnabled = false;

                    bool returnValue = await DisplayAlert(Localize.GetString("TelegramResetAccount"),
                                       Localize.GetString("TelegramNeedToResetAccount"), Localize.GetString("TelegramOkay") , Localize.GetString("TelegramCancel"));

                    if (!returnValue)
                    {
                        _code.IsEnabled = true;
                        _finish.IsEnabled = true;
                        _progressBar.IsVisible = false;
                        DependencyService.Get<IPluginPageControls>().BackPressEnabled = true;
                        tabs.IsEnabled = true;
                        return;
                    }

                    bool resetted = await ResetAccount();

                    if (resetted)
                    {
                        userInformation.CountryCode = CountryCode;
                        userInformation.NationalNumber = NationalNumber;
                        userInformation.Code = Code;
                        tabs.Children.Add(userInformation);
                        tabs.CurrentPage = userInformation;
                    }
                    else
                    {
                        _error.Text = Localize.GetString("TelegramVerifyError");
                    }

                    _code.IsEnabled = true;
                    _finish.IsEnabled = true;
                    _progressBar.IsVisible = false;
                    DependencyService.Get<IPluginPageControls>().BackPressEnabled = true;
                    tabs.IsEnabled = true;

                };

                _progressBar = new ActivityIndicator();
                _progressBar.VerticalOptions = LayoutOptions.CenterAndExpand;
                _progressBar.IsRunning = true;
                _progressBar.IsVisible = false;

                var stackLayout = new StackLayout();
                stackLayout.Spacing = 20;
                stackLayout.Padding = 25;
                stackLayout.VerticalOptions = LayoutOptions.Start;
                var children = stackLayout.Children;
                children.Add(_passwordCodeLabel);
                children.Add(_code);
                children.Add(_finish);
                children.Add(_error);
                children.Add(_progressBar);
                children.Add(_resetAccount);

                Content = new ScrollView { Content = stackLayout };
                Title = Localize.GetString("TelegramPasswordCodeTitle");
            }

            private Task<bool> ResetAccount()
            {
                return Task<bool>.Factory.StartNew(() =>
                {
                    var result = Telegram.RequestAccountReset(_service, GetSettingsTelegramSettings(NationalNumber));
                    return result;
                });
            }

            private Task<ActivationResult> VerifyCode(string code)
            {
                return Task<ActivationResult>.Factory.StartNew(() =>
                {
                    var result = Telegram.VerifyCode(_service, GetSettingsTelegramSettings(NationalNumber), code);
                    if (result.Response == Telegram.CodeRegister.Type.Success)
                    {
                        return new ActivationResult
                        {
                            AccountId = result.AccountId,
                            Success = true
                        };
                    }
                    else
                    {
                        return new ActivationResult
                        {
                            ErrorMessage = Localize.GetString("TelegramCodeError")
                        };
                    }
                });
            }
        }

        private class MuteNotifications : ContentPage
        {
            private static Tuple<int, int> FirstIndexOf(string str, string[] tokens)
            {
                int smallest = int.MaxValue;
                int currentI = -1;
                for (int i = 0; i < tokens.Length; i++)
                {
                    var index = str.IndexOf(tokens[i], StringComparison.InvariantCulture);
                    if (index > -1 && index < smallest)
                    {
                        smallest = index;
                        currentI = i;
                    }
                }
                if (currentI == -1)
                {
                    smallest = -1;
                }
                return Tuple.Create(currentI, smallest);
            }

            private static FormattedString GenerateString(string str, double fontSize)
            {
                var spans = new List<Span>();
                var newStr = string.Copy(str);

                var openTokens = new[] { "<i>", "<b>", "<u>" };
                var closedTokens = new[] { "</i>", "</b>", "</u>" };

                while (true)
                {
                    var openTokenIndex = FirstIndexOf(newStr, openTokens);
                    if (openTokenIndex.Item2 > -1)
                    {
                        var leftPart = newStr.Remove(openTokenIndex.Item2);
                        var rightPart = newStr.Remove(0, openTokenIndex.Item2);
                        spans.Add(new Span { Text = leftPart, FontSize = fontSize });
                        var closedTokenIndex = rightPart.IndexOf(closedTokens[openTokenIndex.Item1], StringComparison.InvariantCulture);
                        if (closedTokenIndex > -1)
                        {
                            var item = rightPart.Remove(closedTokenIndex);
                            item = item.Replace(openTokens[openTokenIndex.Item1], "");
                            var attribute = FontAttributes.None;
                            if (openTokenIndex.Item1 == 0)
                            {
                                attribute = FontAttributes.Italic;
                            }
                            else
                            {
                                attribute = FontAttributes.Bold;
                            }
                            spans.Add(new Span { Text = item, FontSize = fontSize, FontAttributes = attribute });
                            var rightPartAdjusted = rightPart.Remove(0, closedTokenIndex + 4);
                            newStr = rightPartAdjusted;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(newStr))
                        {
                            spans.Add(new Span { Text = newStr, FontSize = fontSize });
                        }
                        break;
                    }
                }

                var formattedString = new FormattedString();
                foreach (var span in spans)
                {
                    formattedString.Spans.Add(span);
                }
                return formattedString;
            }

            private ScrollView BuildNotificationTutorial(Action launchVideo, Action launchTelegram)
            {
                var fontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label));

                var scrollView = new ScrollView();

                var mainLayout = new StackLayout { Orientation = StackOrientation.Vertical };
                mainLayout.Padding = new Thickness(20, 20);

                var headerLayout = new StackLayout { Orientation = StackOrientation.Horizontal };
                var headerLabel = new Label();
                headerLabel.FormattedText = GenerateString(Localize.GetString("TelegramMuteNotificationsHeader"), fontSize);
                headerLabel.Margin = new Thickness(0, 0, 0, 15);
                headerLayout.Children.Add(headerLabel);
                mainLayout.Children.Add(headerLayout);

                var stepOne = new StackLayout { Orientation = StackOrientation.Vertical };
                var stepOneTitle = new Label();
                stepOneTitle.Text = Localize.GetString("TelegramMuteNotificationsStep1Title");
                stepOneTitle.FontSize = fontSize;
                stepOneTitle.FontAttributes = FontAttributes.Bold;
                stepOne.Children.Add(stepOneTitle);
                var stepOneDescription = new Label();
                stepOneDescription.FormattedText = GenerateString(Localize.GetString("TelegramMuteNotificationsStep1Text"), fontSize);
                stepOneDescription.FontSize = fontSize;
                stepOneDescription.Margin = new Thickness(5, 0, 5, 0);
                stepOne.Children.Add(stepOneDescription);
                var stepOneButtonContainer = new StackLayout { Orientation = StackOrientation.Horizontal };
                stepOneButtonContainer.Margin = new Thickness(5, 0, 5, 0);
                var stepOneWatchVideo = new Button();
                stepOneWatchVideo.Text = Localize.GetString("TelegramMuteNotificationsWatchVideo");
                stepOneWatchVideo.Clicked += (sender, e) =>
                {
                    launchVideo();
                };
                stepOneButtonContainer.Children.Add(stepOneWatchVideo);
                stepOne.Children.Add(stepOneButtonContainer);
                mainLayout.Children.Add(stepOne);

                scrollView.Content = mainLayout;
                return scrollView;
            }

            private void OpenTelegram()
            {
                Platform.OpenApp(TelegramPackage);
            }

            private void DisableNotificationsVideo()
            {
                Platform.LaunchViewIntent("https://www.youtube.com/watch?v=HJVJG7onFb0");
            }

            public MuteNotifications()
            {
                Content = BuildNotificationTutorial(DisableNotificationsVideo, OpenTelegram);
                Title = Localize.GetString("TelegramMuteNotificationsTitle");
                ToolbarItems.Add(new ToolbarItem(Localize.GetString("TelegramFinish"), null, async () =>
                {
                    await Navigation.PopToRootAsync();
                    DependencyService.Get<IPluginPageControls>().Finish();
                }));
            }
        }

        private class Password : ContentPage
        {
            private Label _passwordLabel;
            private Entry _password;
            private Button _finish;
            private Label _error;
            private Button _forgotPassword;

            private byte[] _currentSalt;
            private byte[] _newSalt;

            public string CountryCode { get; set; }
            public string NationalNumber { get; set; }
            public string Code { get; internal set; }

            private ActivityIndicator _progressBar;

            private Service _service;

            public Password(Service service, TabbedPage tabs, PasswordCode passwordCode)
            {
                _service = service;

                _passwordLabel = new Label();
                _passwordLabel.VerticalOptions = LayoutOptions.CenterAndExpand;
                _passwordLabel.Text = Localize.GetString("TelegramEnterPassword");
                _passwordLabel.IsVisible = true;
                _passwordLabel.Font = Font.SystemFontOfSize(18);
                _passwordLabel.XAlign = TextAlignment.Center; 

                _password = new Entry();
                _password.IsPassword = true;
                _password.Placeholder = Localize.GetString("TelegramPassword");

                _finish = new Button();
                _finish.Text = Localize.GetString("TelegramFinish");
                _finish.TextColor = Color.White;
                _finish.BackgroundColor = Color.FromHex("77D065");
                _finish.Clicked += async (sender, e) =>
                {
                    _password.IsEnabled = false;
                    _finish.IsEnabled = false;
                    _progressBar.IsVisible = true;
                    DependencyService.Get<IPluginPageControls>().BackPressEnabled = false;
                    tabs.IsEnabled = false;
                    var password = _password.Text;

                    if (string.IsNullOrEmpty(password))
                    {
                        await DisplayAlert(Localize.GetString("TelegramInvalidPassword"),
                                    Localize.GetString("TelegramInvalidPasswordDetail"), Localize.GetString("TelegramOkay"));
                    }

                    var result = await VerifyPassword(password);

                    password = null;

                    _password.IsEnabled = true;
                    _finish.IsEnabled = true;
                    _progressBar.IsVisible = false;
                    DependencyService.Get<IPluginPageControls>().BackPressEnabled = true;
                    tabs.IsEnabled = true;

                    if (result.Success)
                    {
						CleanUp(service);
                        Save(service, result.AccountId, GetSettingsTelegramSettings(NationalNumber));
                        if (Platform.DeviceHasApp(TelegramPackage))
                        {
                            await Navigation.PushAsync(new MuteNotifications());
                        }
                        else
                        {
                            await Navigation.PopToRootAsync();
                            DependencyService.Get<IPluginPageControls>().Finish();
                        }
                    }
                    else
                    {
                        _error.Text = result.ErrorMessage;
                    }
                };

                _error = new Label();
                _error.VerticalOptions = LayoutOptions.EndAndExpand;
                _error.IsVisible = false;
                _error.Font = Font.SystemFontOfSize(18);
                _error.XAlign = TextAlignment.Center;
                _error.TextColor = Color.Red;


                _forgotPassword = new Button();
                _forgotPassword.VerticalOptions = LayoutOptions.EndAndExpand;
                _forgotPassword.Text = Localize.GetString("TelegramForgotPassword");
                _forgotPassword.TextColor = Color.White;
                _forgotPassword.BackgroundColor = Color.FromHex("77D065");
                _forgotPassword.Clicked += async (sender, e) =>
                {
                    _password.IsEnabled = false;
                    _finish.IsEnabled = false;
                    _progressBar.IsVisible = true;
                    DependencyService.Get<IPluginPageControls>().BackPressEnabled = false;
                    tabs.IsEnabled = false;

                    var result = await RecoverPassword();

                    _password.IsEnabled = true;
                    _finish.IsEnabled = true;
                    _progressBar.IsVisible = false;
                    DependencyService.Get<IPluginPageControls>().BackPressEnabled = true;
                    tabs.IsEnabled = true;

                    if (result != null)
                    {
                        passwordCode.CountryCode = CountryCode;
                        passwordCode.NationalNumber = NationalNumber;
                        passwordCode.EmailPattern = result.EmailPattern;
                        passwordCode.Code = Code;
                        tabs.Children.Add(passwordCode);
                        tabs.CurrentPage = passwordCode;
                    }

                };

                _progressBar = new ActivityIndicator();
                _progressBar.VerticalOptions = LayoutOptions.CenterAndExpand;
                _progressBar.IsRunning = true;
                _progressBar.IsVisible = false;

                var stackLayout = new StackLayout();
                stackLayout.Spacing = 20;
                stackLayout.Padding = 25;
                stackLayout.VerticalOptions = LayoutOptions.Start;
                var children = stackLayout.Children;
                children.Add(_passwordLabel);
                children.Add(_password);
                children.Add(_finish);
                children.Add(_error);
                children.Add(_progressBar);
                children.Add(_forgotPassword);

                Content = new ScrollView { Content = stackLayout };
                Title = Localize.GetString("TelegramPasswordTitle");
            }



            private Task<RecoverResult> RecoverPassword() 
            {
                return Task<RecoverResult>.Factory.StartNew(() =>
                {
                    var result = Telegram.RequestPasswordRecovery(_service, GetSettingsTelegramSettings(NationalNumber));
                    if (result == null)
                    {
                        return null;
                    }
                    return new RecoverResult
                    {
                        EmailPattern = result.EmailPattern
                    };
                });
            }

            private Task<ActivationResult> VerifyPassword(string password)
            {
                return Task<ActivationResult>.Factory.StartNew(() =>
                {
                    var passwordBytes = Encoding.UTF8.GetBytes(password);
                    var hashInput = new byte[_currentSalt.Length * 2 + passwordBytes.Length];

                    Array.Copy(_currentSalt, 0, hashInput, 0, _currentSalt.Length);
                    Array.Copy(passwordBytes, 0, hashInput, _currentSalt.Length, passwordBytes.Length);
                    Array.Copy(_currentSalt, 0, hashInput, hashInput.Length - _currentSalt.Length, _currentSalt.Length);
                    SHA256Managed sha = new SHA256Managed();
                    var hash = sha.ComputeHash(hashInput);
                    var result = Telegram.VerifyPassword(_service, GetSettingsTelegramSettings(NationalNumber), hash);
                    if (result.Response == Telegram.CodeRegister.Type.Success)
                    {
                        return new ActivationResult
                        {
                            AccountId = result.AccountId,
                            Success = true
                        };
                    }
                    else if (result.Response == Telegram.CodeRegister.Type.InvalidPassword)
                    {
                        return new ActivationResult
                        {
                            ErrorMessage = Localize.GetString("TelegramInvalidPassword")
                        };
                    }
                    else
                    {
                        return new ActivationResult
                        {
                            ErrorMessage = Localize.GetString("TelegramPasswordError")
                        };
                    }
                });
            }

            public void SetFields(PasswordInformation info)
            {
                if (!string.IsNullOrWhiteSpace(info.Hint))
                {
                    _password.Placeholder = info.Hint;
                }
                _forgotPassword.IsVisible = info.HasRecovery;
                _currentSalt = info.CurrentSalt;
                _newSalt = info.NewSalt;
            }
        }


        private class Info : ContentPage
        {
            private PhoneEntry _phoneNumber;
            private Label _phoneNumberPlus;
            private StackLayout _phoneNumberContainer;
            private StackLayout _loadConversationsLayout;
            private Label _loadConversationsTitle;
            private Switch _loadConversationsSwitch;
            private Button _next;
            private Image _image;
            private ActivityIndicator _progressBar;

            public Info(Service service, TabbedPage tabs, Code code)
            {
                _phoneNumberContainer = new StackLayout();
                _phoneNumberContainer.Orientation = StackOrientation.Horizontal;
                _phoneNumberContainer.HorizontalOptions = LayoutOptions.FillAndExpand;
                _phoneNumber = new PhoneEntry();
                _phoneNumber.Placeholder = Localize.GetString("TelegramPhoneNumber");
                _phoneNumber.HorizontalOptions = LayoutOptions.FillAndExpand;
                _phoneNumberContainer.Children.Add(_phoneNumber);
                var programmaticChange = false;

                _loadConversationsLayout = new StackLayout();
                _loadConversationsLayout.Orientation = StackOrientation.Horizontal;
                _loadConversationsLayout.HorizontalOptions = LayoutOptions.End;
                _loadConversationsLayout.Padding = new Thickness(15, 0);
                _loadConversationsLayout.Spacing = 10;

                _loadConversationsTitle = new Label();
                _loadConversationsTitle.Text = Localize.GetString("TelegramLoadConversations");
                _loadConversationsSwitch = new Switch();
                _loadConversationsSwitch.Toggled += (sender, e) =>
                {
                    (service as Telegram).LoadConversations = e.Value;
                };
                _loadConversationsSwitch.IsToggled = true;
                _loadConversationsLayout.Children.Add(_loadConversationsTitle);
                _loadConversationsLayout.Children.Add(_loadConversationsSwitch);

                _next = new Button();
                _next.HorizontalOptions = LayoutOptions.FillAndExpand;
                _next.Text = Localize.GetString("TelegramNext");
                _next.TextColor = Color.White;
                _next.BackgroundColor = Color.FromHex("77D065");
                _next.Clicked += async (sender, e) =>
                {
                        Func<Task> invalidNumber = () =>
                            {
                                return DisplayAlert(Localize.GetString("TelegramInvalidNumberTitle"), 
                                    Localize.GetString("TelegramInvalidNumberMessage"), Localize.GetString("TelegramOkay"));
                            };

                        if (!PhoneBook.IsPossibleNumber(_phoneNumber.Text))
                        {
                            await invalidNumber();
                            return;
                        }

                        var number = PhoneBook.TryGetPhoneNumberLegible(_phoneNumber.Text);
                        var formattedNumber = PhoneBook.FormatPhoneNumber(number);

                        if (formattedNumber == null)
                        {
                            await invalidNumber();
                            return;
                        }

                        var nationalNumber = new string(formattedNumber.Item2.Where(Char.IsDigit).ToArray());

                        if (!await DisplayAlert(Localize.GetString("TelegramConfirmNumberTitle"), 
                            Localize.GetString("TelegramConfirmNumberMessage").Replace("[number]", number), 
                            Localize.GetString("TelegramYes"), 
                            Localize.GetString("TelegramNo")))
                        {
                            return;
                        }

                        _progressBar.IsVisible = true;
                        _next.IsEnabled = false;
                        _phoneNumber.IsEnabled = false;
                        DependencyService.Get<IPluginPageControls>().BackPressEnabled = false;

                        TelegramSettings settings = null;

                        var skipSave = false;
                        var state = _settings.States.FirstOrDefault(x => x.NationalNumber == nationalNumber);
                        if (state != null && state.Settings != null)
                        {
                            skipSave = true;
                            settings = state.Settings;
                        }
                        else
                        {
                            settings = await Task<TelegramSettings>.Factory.StartNew(() => { return Telegram.GenerateAuthentication(service); });
                        }


                        DependencyService.Get<IPluginPageControls>().BackPressEnabled = true;
                        _phoneNumber.IsEnabled = true;
                        _next.IsEnabled = true;
                        _progressBar.IsVisible = false;

                        if (settings == null)
                        {
                            await DisplayAlert(Localize.GetString("TelegramAuthGenerationFailedTitle"), 
                                Localize.GetString("TelegramAuthGenerationFailedMessage"), Localize.GetString("TelegramOkay"));
                            return;
                        }   

                        if (!skipSave)
                        {
                            _settings.States.Add(new Setup.TelegramSetupSettings.State
                                {
                                    Settings = settings,
                                    NationalNumber = nationalNumber
                                });
                            SaveSettings();
                        }

                        code.CountryCode = formattedNumber.Item1;
                        code.NationalNumber = nationalNumber;
                        tabs.Children.Add(code);
                        tabs.CurrentPage = code;
                };

                _image = new Image();
                _image.Source = ImageSource.FromUri(
                    new Uri("https://lh4.ggpht.com/fuvTtxbZ1-dkEmzUMfKcgMJwW8PyY4fhJJ_NKT-NpIQJukszEY2GfCkJUF5ch6Co3w=w300"));
                _image.WidthRequest = 100;
                _image.HeightRequest = 100;

                _progressBar = new ActivityIndicator();
                _progressBar.VerticalOptions = LayoutOptions.EndAndExpand;
                _progressBar.IsRunning = true;
                _progressBar.IsVisible = false;

                var stackLayout = new StackLayout();
                stackLayout.Spacing = 20;
                stackLayout.Padding = 25;
                stackLayout.VerticalOptions = LayoutOptions.Start;
                var children = stackLayout.Children;
                children.Add(_image);
                children.Add(_phoneNumberContainer);
                children.Add(_loadConversationsLayout);
                var nextLayout = new StackLayout();
                nextLayout.Spacing = 20;
                nextLayout.Orientation = StackOrientation.Horizontal;
                nextLayout.Children.Add(_next);
                nextLayout.Children.Add(_progressBar);
                children.Add(nextLayout);

                Content = new ScrollView { Content = stackLayout };
                Title = Localize.GetString("TelegramInformationTitle");
            }
        }

        private class Code : ContentPage 
        {
            private Label _label;
            private Label _label2;
            private Entry _code;
            private Button _submit;
            private Button _verify;
            private ActivityIndicator _progressBar;
            private Label _error;

            private int _verificationAttempt;

            private Service _service;

            public string CountryCode { get; set; }
            public string NationalNumber { get; set; }


            public Code(Service service, TabbedPage tabs, Password password, UserInformation userInformation)
            {

                Action<Label, ActivationResult.ActivationType> setLabelText = (label, type) =>
                {
                    switch (type)
                    {
                        case ActivationResult.ActivationType.PhoneCall:
                            label.Text = Localize.GetString("TelegramCodeSentPhoneCall");
                            break;
                        case ActivationResult.ActivationType.Telegram:
                            label.Text = Localize.GetString("TelegramCodeSentTelegramApp");
                            break;
                        case ActivationResult.ActivationType.Text:
                            label.Text = Localize.GetString("TelegramCodeSentText");
                            break;
                        case ActivationResult.ActivationType.Unknown:
                            label.Text = "";
                            break;
                    }
                };

                Action<Label, ActivationResult.ActivationType> setLabelNextText = (label, type) =>
                {
                    switch (type)
                    {
                        case ActivationResult.ActivationType.PhoneCall:
                            label.Text = Localize.GetString("TelegramCodeResendViaPhoneCall");
                            break;
                        case ActivationResult.ActivationType.Text:
                            label.Text = Localize.GetString("TelegramCodeResendViaText");
                            break;
                        case ActivationResult.ActivationType.Unknown:
                            label.Text = "";
                            break;
                    }
                };

                Action<bool> doVerify = async (reVerify) =>
                {
                    _error.IsVisible = false;
                    _progressBar.IsVisible = true;
                    _code.IsEnabled = false;
                    _submit.IsEnabled = false;
                    _verify.IsEnabled = false;

                    tabs.IsEnabled = false;
                    DependencyService.Get<IPluginPageControls>().BackPressEnabled = false;

                    var result = await DoVerify(reVerify);

                    setLabelText(_label, result.CurrentType);

                    setLabelNextText(_label2, result.NextType);

                    _error.IsVisible = true;
                    _progressBar.IsVisible = false;
                    _code.IsEnabled = true;
                    _submit.IsEnabled = true;
                    if(result.NextType != ActivationResult.ActivationType.Unknown)
                        _verify.IsEnabled = true;
                    
                    tabs.IsEnabled = true;
                    DependencyService.Get<IPluginPageControls>().BackPressEnabled = true;

                    if (!result.Success)
                    {
                        _error.IsVisible = true;
                        _error.Text = result.ErrorMessage;
                        return;
                    }

                };

                _service = service;

                _label = new Label();
                _label.Font = Font.SystemFontOfSize(18);
                _label.XAlign = TextAlignment.Center;
                _label.Text = Localize.GetString("TelegramCodeIntitalText");

                _label2 = new Label();
                _label2.Font = Font.SystemFontOfSize(18);
                _label2.XAlign = TextAlignment.Center;

                _code = new Entry();
                _code.Placeholder = Localize.GetString("TelegramCode");
                _code.IsEnabled = false;

                _verify = new Button();
                _verify.Text = Localize.GetString("TelegramVerify");
                _verify.TextColor = Color.White;
                _verify.BackgroundColor = Color.FromHex("77D065");
                _verify.Clicked += (sender, e) => 
                {
                    if (_verificationAttempt == 0)
                    {
                        doVerify(false);
                    }
                    else
                    {
                        doVerify(true);
                    }
                    _verificationAttempt++;
                }; 


                _submit = new Button();
                _submit.Text = Localize.GetString("TelegramSubmit");
                _submit.TextColor = Color.White;
                _submit.BackgroundColor = Color.FromHex("77D065");
                _submit.IsEnabled = false;
                _submit.Clicked += async (sender, e) =>
                    {
                        Func<Task> invalidCode = () =>
                        {
                            return DisplayAlert(Localize.GetString("TelegramCodeInvalidTitle"),
                                Localize.GetString("TelegramCodeInvalidMessage"), Localize.GetString("TelegramOkay"));
                        };

                        if (String.IsNullOrWhiteSpace(_code.Text))
                        {
                            await invalidCode();
                            return;
                        }

                        var code = new string(_code.Text.Where(Char.IsDigit).ToArray());

                        if (String.IsNullOrWhiteSpace(code))
                        {
                            await invalidCode();
                            return;
                        }

                        _progressBar.IsVisible = true;
                        _error.IsVisible = false;

                        if (!GetSettingsRegistered(NationalNumber))
                        {
                            userInformation.CountryCode = CountryCode;
                            userInformation.NationalNumber = NationalNumber;
                            userInformation.Code = code;
                            tabs.Children.Add(userInformation);
                            tabs.CurrentPage = userInformation;
                            return;
                        }

                        DependencyService.Get<IPluginPageControls>().BackPressEnabled = false;
                        _code.IsEnabled = false;
                        _submit.IsEnabled = false;
                        tabs.IsEnabled = false;

                        var result = await Register(service, NationalNumber, CountryCode, code);

                        DependencyService.Get<IPluginPageControls>().BackPressEnabled = true;
                        _code.IsEnabled = true;
                        _submit.IsEnabled = true;
                        tabs.IsEnabled = true;

                        _progressBar.IsVisible = false;

                        if (!result.Success && result.PasswordNeeded)
                        {
                            //add a tab for the password selection
                            password.CountryCode = CountryCode;
                            password.NationalNumber = NationalNumber;
                            password.Code = code;
                            password.SetFields(result.PasswordInformation);
                            tabs.Children.Add(password);
                            tabs.CurrentPage = password;
                            return;
                        }

                        if (!result.Success)
                        {
                            _error.IsVisible = true;
                            _error.Text = result.ErrorMessage;
                            return;
                        }
						CleanUp(service);
                        Save(service, result.AccountId, GetSettingsTelegramSettings(NationalNumber));
                        if (Platform.DeviceHasApp(TelegramPackage))
                        {
                            await Navigation.PushAsync(new MuteNotifications());
                        }
                        else
                        {
                            await Navigation.PopToRootAsync();
                            DependencyService.Get<IPluginPageControls>().Finish();
                        }
                    };


                _progressBar = new ActivityIndicator();
                _progressBar.VerticalOptions = LayoutOptions.EndAndExpand;
                _progressBar.IsRunning = true;
                _progressBar.IsVisible = false;

                _error = new Label();
                _error.VerticalOptions = LayoutOptions.EndAndExpand;
                _error.IsVisible = false;
                _error.Font = Font.SystemFontOfSize(18);
                _error.XAlign = TextAlignment.Center;
                _error.TextColor = Color.Red;

                var stackLayout = new StackLayout();
                stackLayout.Spacing = 20;
                stackLayout.Padding = 25;
                stackLayout.VerticalOptions = LayoutOptions.Start;
                var children = stackLayout.Children;
                children.Add(_label);
                children.Add(_verify);
                children.Add(_label2);
                children.Add(_code);
                children.Add(_submit);
                children.Add(_progressBar);
                children.Add(_error);

                Content = new ScrollView { Content = stackLayout };
                Title = Localize.GetString("TelegramCodeTitle");
            }

			private Task<ActivationResult> DoVerify(bool reVerify)
            {
                return Task<ActivationResult>.Factory.StartNew(() =>
                    {
                        var settings = GetSettingsTelegramSettings(NationalNumber);
                        var response = Telegram.RequestCode(_service, CountryCode + NationalNumber,
                                                            GetSettingsCodeHash(NationalNumber),
                                                            GetSettingsTelegramSettings(NationalNumber), reVerify);

                        if (response.Response == Telegram.CodeRequest.Type.Migrate)
                        {
                            TelegramSettings newSettings;
                            using (var migratedClient = Telegram.GetNewClient(response.MigrateId, GetSettingsTelegramSettings(NationalNumber), out newSettings))
                            {
                                TelegramUtils.RunSynchronously(migratedClient.Connect());
                                response = Telegram.RequestCode(_service, CountryCode + NationalNumber,
                                                                GetSettingsCodeHash(NationalNumber),
                                           newSettings, reVerify);
                                Utils.DebugPrint(">>>>> Response from the server " + ObjectDumper.Dump(response));

                                SetSettingsSettings(newSettings, false, NationalNumber);
                                SetSettingsCodeHash(response.CodeHash, true, NationalNumber);
                                SetSettingsRegistered(response.Registered, true, NationalNumber);

                                var type = GetActivationType(response.NextType);
                                if (type == ActivationResult.ActivationType.Unknown)
                                {
                                    _verify.IsEnabled = false;
                                }
                                return new ActivationResult
                                {
                                    Success = true,
                                    CurrentType = GetActivationType(response.CurrentType),
                                    NextType = GetActivationType(response.NextType)
                                };
                            }
                        }
                        if (response == null || response.Response != Telegram.CodeRequest.Type.Success)
                        {
                            var message = Localize.GetString("TelegramVerifyError");
                            if (response != null)
                            {
                                if (response.Response == Telegram.CodeRequest.Type.NumberInvalid)
                                {
                                    message = Localize.GetString("TelegramVerifyInvalidNumber");
                                }
                            }

                            return new ActivationResult
                            {
                                Success = false,
                                ErrorMessage = message,
                            };
                        }

                        SetSettingsCodeHash(response.CodeHash, true, NationalNumber);
                        SetSettingsRegistered(response.Registered, true, NationalNumber);
                        var nextType = GetActivationType(response.NextType);
                        if (nextType == ActivationResult.ActivationType.Unknown)
                        {
                            _verify.IsEnabled = false;
                        }
                        return new ActivationResult
                        {
                            Success = true,
                            CurrentType = GetActivationType(response.CurrentType),
                            NextType = GetActivationType(response.NextType)
                        };
                    });
            }

            private ActivationResult.ActivationType GetActivationType(Telegram.CodeRequest.AuthType type)
            {
                var result = ActivationResult.ActivationType.Unknown;
                switch (type)
                {
                    case Telegram.CodeRequest.AuthType.Phone:
                        result = ActivationResult.ActivationType.PhoneCall;
                        break;
                    case Telegram.CodeRequest.AuthType.Telegram:
                        result = ActivationResult.ActivationType.Telegram;
                        break;
                    case Telegram.CodeRequest.AuthType.Text:
                        result = ActivationResult.ActivationType.Text;
                        break;
                    default:
                        result = ActivationResult.ActivationType.Unknown;
                        break;
                }
                return result;
            }

        }

		private static void CleanUp(Service service)
		{
			var bubbleGroups = BubbleGroupManager.FindAll(service);
			//foreach(var groups in bubbleGroups)
			//{
			//	BubbleGroupFactory.Delete(groups);
			//}
			try
			{
				var databasePath = Platform.GetDatabasePath();
                var settingsPath = Platform.GetSettingsPath();
                try
                {
                    File.Delete(Path.Combine(databasePath, "userscache.db"));
                }
                catch
                {
                    // do-nothing
                }
                try
                {
                    File.Delete(Path.Combine(databasePath, "chatscache.db"));
                }
                catch
                {
                    // do-nothing
                }
                try
                {
                    File.Delete(Path.Combine(settingsPath, "TelegramDcs.db"));
                }   
                catch
                {
                    // do-nothing
                }
				MutableSettingsManager.Delete<TelegramMutableSettings>();
			}
			catch (Exception ex)
			{
				//nothing to do
			}
		}
    }
}

