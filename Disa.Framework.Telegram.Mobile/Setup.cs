using System;
using System.Collections.Generic;
using Disa.Framework.Mobile;
using Xamarin.Forms;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Disa.Framework.Telegram.Mobile
{
    public static class Setup 
    {
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

        private class ActivationResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public uint AccountId { get; set; }
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

            var code = new Code(service, tabs);
            var verify = new Verify(service, tabs, code);
            var info = new Info(service, tabs, verify);

            tabs.Children.Add(info);
            tabs.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == "CurrentPage")
                    {
                        var selected = tabs.CurrentPage;
                        if (selected is Info)
                        {
                            tabs.Children.Remove(verify);
                            tabs.Children.Remove(code);
                        }
                        if (selected is Verify)
                        {
                            tabs.Children.Remove(code);
                        }
                    }
                };

            tabs.Title = Localize.GetString("TelegramSetupWizardTitle");
            _cachedPage = tabs;
            return tabs;
        }

        private class Info : ContentPage
        {
            private PhoneEntry _phoneNumber;
            private Label _phoneNumberPlus;
            private StackLayout _phoneNumberContainer;
            //private CheckBox _loadConversations;

            private Entry _firstName;
            private Entry _lastName;
            private Button _next;
            private Image _image;
            private ActivityIndicator _progressBar;

            public Info(Service service, TabbedPage tabs, Verify verify)
            {
                _phoneNumberContainer = new StackLayout();
                _phoneNumberContainer.Orientation = StackOrientation.Horizontal;
                _phoneNumberContainer.HorizontalOptions = LayoutOptions.FillAndExpand;
                _phoneNumber = new PhoneEntry();
                _phoneNumber.Placeholder = Localize.GetString("TelegramPhoneNumber");
                _phoneNumber.HorizontalOptions = LayoutOptions.FillAndExpand;
                _phoneNumberContainer.Children.Add(_phoneNumber);
                var programmaticChange = false;

                _firstName = new Entry();
                _firstName.Placeholder = Localize.GetString("TelegramFirstName");

                _lastName = new Entry();
                _lastName.Placeholder = Localize.GetString("TelegramLastName");

//                _loadConversations = new CheckBox();
//                _loadConversations.DefaultText = Localize.GetString("TelegramLoadConversations");
//                _loadConversations.CheckedChanged += (sender, e) =>
//                    {
//                        //TODO:
//                    };
//                _loadConversations.Checked = true;

                _next = new Button();
                _next.HorizontalOptions = LayoutOptions.FillAndExpand;
                _next.Text = Localize.GetString("TelegramNext");
                _next.TextColor = Color.White;
                _next.BackgroundColor = Color.FromHex("77D065");
                _next.Clicked += async (sender, e) =>
                    {
                        if (String.IsNullOrWhiteSpace(_firstName.Text))
                        {
                            await DisplayAlert(Localize.GetString("TelegramInvalidFirstNameTitle"), Localize.GetString("TelegramInvalidFirstNameMessage"), Localize.GetString("TelegramOkay"));
                            return;
                        }

                        if (String.IsNullOrWhiteSpace(_lastName.Text))
                        {
                            await DisplayAlert(Localize.GetString("TelegramInvalidLastNameTitle"), Localize.GetString("TelegramInvalidLastNameMessage"), Localize.GetString("TelegramOkay"));
                            return;
                        }

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
                        _firstName.IsEnabled = false;
                        _lastName.IsEnabled = false;
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

                        var firstName = _firstName.Text.Trim();
                        var lastName = _lastName.Text.Trim();

                        DependencyService.Get<IPluginPageControls>().BackPressEnabled = true;
                        _firstName.IsEnabled = true;
                        _lastName.IsEnabled = true;
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

                        verify.CountryCode = formattedNumber.Item1;
                        verify.NationalNumber = nationalNumber;
                        verify.FirstName = firstName;
                        verify.LastName = lastName;
                        tabs.Children.Add(verify);
                        tabs.CurrentPage = verify;
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
                children.Add(_firstName);
                children.Add(_lastName);
                children.Add(_phoneNumberContainer);
                //children.Add(_loadConversations);
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

        private class Verify : ContentPage
        {
            public string CountryCode { get; set; }
            private string _nationalNumber;
            public string NationalNumber 
            {
                get
                {
                    return _nationalNumber;
                }
                set
                {
                    _nationalNumber = value;
                    SetVerifyPhoneState();
                }
            }
            public string FirstName { get; set; }
            public string LastName { get; set; }

            private Label _info;
            private Button _verifyPhone;
            private Button _verifySms;
            private Button _haveCode;
            private ActivityIndicator _progressBar;
            private Label _error;

            private Service _service;

            public Verify(Service service, TabbedPage tabs, Code code)
            {
                _service = service;

                _info = new Label();
                _info.Text = Localize.GetString("TelegramVerifyQuestion");
                _info.Font = Font.SystemFontOfSize(18);
                _info.XAlign = TextAlignment.Center;

                Action<bool, Action> doVerify = async (viaSms, postAction) =>
                    {
                        _error.IsVisible = false;
                        _progressBar.IsVisible = true;

                        DependencyService.Get<IPluginPageControls>().BackPressEnabled = false;
                        _verifyPhone.IsEnabled = false;
                        _verifySms.IsEnabled = false;
                        _haveCode.IsEnabled = false;
                        tabs.IsEnabled = false;

                        var result = await DoVerify(viaSms ? 
                            VerificationOption.Sms : VerificationOption.Voice);

                        DependencyService.Get<IPluginPageControls>().BackPressEnabled = true;
                        _verifyPhone.IsEnabled = true;
                        _verifySms.IsEnabled = true;
                        _haveCode.IsEnabled = true;
                        tabs.IsEnabled = true;

                        _progressBar.IsVisible = false;

                        if (postAction != null)
                        {
                            postAction();
                        }

                        if (!result.Success)
                        {
                            _error.IsVisible = true;
                            _error.Text = result.ErrorMessage;
                            return;
                        }

                        code.CountryCode = CountryCode;
                        code.NationalNumber = NationalNumber;
                        code.FirstName = FirstName;
                        code.LastName = LastName;
                        code.ViaSms = viaSms;
                        code.CodeSent = true;
                        tabs.Children.Add(code);
                        tabs.CurrentPage = code;
                    };
                _verifySms = new Button();
                _verifySms.Text = Localize.GetString("TelegramVerifyViaSms");
                _verifySms.TextColor = Color.White;
                _verifySms.BackgroundColor = Color.FromHex("77D065");
                _verifySms.Clicked += (sender, e) =>
                    {
                        doVerify(true, SetVerifyPhoneState);
                    };
                _verifyPhone = new Button();
                _verifyPhone.Text = Localize.GetString("TelegramVerifyViaPhone");
                _verifyPhone.TextColor = Color.White;
                _verifyPhone.Clicked += (sender, e) =>
                    {
                        doVerify(false, null);
                    };

                _progressBar = new ActivityIndicator();
                _progressBar.VerticalOptions = LayoutOptions.CenterAndExpand;
                _progressBar.IsRunning = true;
                _progressBar.IsVisible = false;

                _error = new Label();
                _error.VerticalOptions = LayoutOptions.CenterAndExpand;
                _error.IsVisible = false;
                _error.Font = Font.SystemFontOfSize(18);
                _error.XAlign = TextAlignment.Center;
                _error.TextColor = Color.Red;

                _haveCode = new Button();
                _haveCode.VerticalOptions = LayoutOptions.EndAndExpand;
                _haveCode.Text = Localize.GetString("TelegramVerifyHaveCode");
                _haveCode.TextColor = Color.White;
                _haveCode.BackgroundColor = Color.FromHex("c50923");
                _haveCode.Clicked += (sender, e) =>
                    {
                        code.CountryCode = CountryCode;
                        code.NationalNumber = NationalNumber;
                        code.FirstName = FirstName;
                        code.LastName = LastName;
                        code.ViaSms = true;
                        code.CodeSent = false;
                        tabs.Children.Add(code);
                        tabs.CurrentPage = code;
                    };
                
                var stackLayout = new StackLayout();
                stackLayout.Spacing = 20;
                stackLayout.Padding = 25;
                stackLayout.VerticalOptions = LayoutOptions.FillAndExpand;
                var children = stackLayout.Children;
                children.Add(_info);
                children.Add(_verifySms);
                children.Add(_verifyPhone);
                children.Add(_progressBar);
                children.Add(_error);
                children.Add(_haveCode);

                Content = new ScrollView { Content = stackLayout };
                Title = Localize.GetString("TelegramVerificationTitle");
            }

            private TelegramSettings GetSettingsTelegramSettings()
            {
                var state = _settings.States.FirstOrDefault(x => x.NationalNumber == NationalNumber);
                if (state == null)
                    return null;
                return state.Settings;
            }

            private string GetSettingsCodeHash()
            {
                var state = _settings.States.FirstOrDefault(x => x.NationalNumber == NationalNumber);
                if (state == null)
                    return null;
                return state.CodeHash;
            }

            private void SetVerifyPhoneState()
            {
                _verifyPhone.IsEnabled = GetSettingsCodeHash() != null;
                _verifyPhone.BackgroundColor = _verifyPhone.IsEnabled ? Color.FromHex("77D065") : Color.Gray;
                _haveCode.IsEnabled = _verifyPhone.IsEnabled;
                _haveCode.BackgroundColor = _haveCode.IsEnabled ? Color.FromHex("c50923") : Color.Gray;
            }

            private void SetSettingsCodeHash(string codeHash)
            {
                var state = _settings.States.FirstOrDefault(x => x.NationalNumber == NationalNumber);
                if (state == null)
                    return;
                state.CodeHash = codeHash;
                SaveSettings();
            }

            private void SetSettingsRegistered(bool registered)
            {
                var state = _settings.States.FirstOrDefault(x => x.NationalNumber == NationalNumber);
                if (state == null)
                    return;
                state.Registered = registered;
                SaveSettings();
            }

            private enum VerificationOption { Sms, Voice };

            private Task<ActivationResult> DoVerify(VerificationOption option)
            {
                return Task<ActivationResult>.Factory.StartNew(() =>
                    {
                        var response = Telegram.RequestCode(_service, CountryCode + NationalNumber,
                            GetSettingsCodeHash(),
                            GetSettingsTelegramSettings(), option == VerificationOption.Voice);

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

                        if (option == VerificationOption.Sms)
                        {
                            SetSettingsCodeHash(response.CodeHash);
                            SetSettingsRegistered(response.Registered);
                        }
                            
                        return new ActivationResult
                        {
                            Success = true
                        };
                    });
            }
        }

        private class Code : ContentPage 
        {
            private Label _label;
            private Label _label2;
            private Entry _code;
            private Button _submit;
            private ActivityIndicator _progressBar;
            private Label _error;

            private Service _service;

            private readonly string SentViaSms = Localize.GetString("TelegramCodeSentViaSms");
            private readonly string SentViaPhone = Localize.GetString("TelegramCodeSentViaPhone");
            private readonly string SentNotComingSms = Localize.GetString("TelegramCodeSentNotComingSms");
            private readonly string ManualEnter = Localize.GetString("TelegramCodeManualEnter");

            public string CountryCode { get; set; }
            public string NationalNumber { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }

            public bool ViaSms { get; set; }

            private bool _codeSent;
            public bool CodeSent 
            {
                get
                {
                    return _codeSent;
                }
                set
                {
                    _codeSent = value;
                    if (_codeSent)
                    {
                        _label.Text = ViaSms ? SentViaSms : SentViaPhone;
                        if (ViaSms)
                        {
                            _label2.Text = SentNotComingSms;
                            _label2.IsVisible = true;
                        }
                        else
                        {
                            _label2.IsVisible = false;
                        }
                    }
                    else
                    {
                        _label.Text = ManualEnter;
                        _label2.IsVisible = true;
                    }
                }
            }

            private bool GetSettingsRegistered()
            {
                var state = _settings.States.FirstOrDefault(x => x.NationalNumber == NationalNumber);
                if (state == null)
                    return false;
                return state.Registered;
            }

            private string GetSettingsCodeHash()
            {
                var state = _settings.States.FirstOrDefault(x => x.NationalNumber == NationalNumber);
                if (state == null)
                    return null;
                return state.CodeHash;
            }

            private TelegramSettings GetSettingsTelegramSettings()
            {
                var state = _settings.States.FirstOrDefault(x => x.NationalNumber == NationalNumber);
                if (state == null)
                    return null;
                return state.Settings;
            }

            private Task<ActivationResult> Register(string code)
            {
                return Task<ActivationResult>.Factory.StartNew(() =>
                    {
                        var result = Telegram.RegisterCode(_service, GetSettingsTelegramSettings(), CountryCode + NationalNumber, 
                            GetSettingsCodeHash(), code, FirstName, LastName, GetSettingsRegistered());

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
                                default:
                                    errorMessage = Localize.GetString("TelegramCodeError");
                                    break;
                            }
                            return new ActivationResult
                            {
                                Success = false,
                                ErrorMessage = errorMessage
                            };
                        }

                        return new ActivationResult
                        {
                            Success = true,
                            AccountId = result.AccountId,
                        };
                    });
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
                    ServiceManager.Start(service, true);
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint("Failed to save the Telegram service: " + ex);
                }

                MutableSettingsManager.Delete<TelegramSetupSettings>();
            }

            public Code(Service service, TabbedPage tabs)
            {
                _service = service;

                _label = new Label();
                _label.Font = Font.SystemFontOfSize(18);
                _label.XAlign = TextAlignment.Center;

                _label2 = new Label();
                _label2.Font = Font.SystemFontOfSize(18);
                _label2.XAlign = TextAlignment.Center;
                _label2.TextColor = Color.Red;

                CodeSent = CodeSent; // set the labels

                _code = new Entry();
                _code.Placeholder = Localize.GetString("TelegramCode");

                _submit = new Button();
                _submit.Text = Localize.GetString("TelegramSubmit");
                _submit.TextColor = Color.White;
                _submit.BackgroundColor = Color.FromHex("77D065");
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

                        DependencyService.Get<IPluginPageControls>().BackPressEnabled = false;
                        _code.IsEnabled = false;
                        _submit.IsEnabled = false;
                        tabs.IsEnabled = false;

                        var result = await Register(code);

                        DependencyService.Get<IPluginPageControls>().BackPressEnabled = true;
                        _code.IsEnabled = true;
                        _submit.IsEnabled = true;
                        tabs.IsEnabled = true;

                        _progressBar.IsVisible = false;

                        if (!result.Success)
                        {
                            _error.IsVisible = true;
                            _error.Text = result.ErrorMessage;
                            return;
                        }

                        Save(service, result.AccountId, GetSettingsTelegramSettings());
                        DependencyService.Get<IPluginPageControls>().Finish();
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
                children.Add(_label2);
                children.Add(_code);
                children.Add(_submit);
                children.Add(_progressBar);
                children.Add(_error);

                Content = new ScrollView { Content = stackLayout };
                Title = Localize.GetString("TelegramCodeTitle");
            }
        }
    }
}

