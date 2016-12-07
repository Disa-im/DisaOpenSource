using System;
using Disa.Framework.Mobile;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Disa.Framework.Telegram;
using SharpMTProto.Messaging;
using SharpTelegram;
using SharpTelegram.Schema;

namespace Disa.Framework.Telegram.Mobile
{
    [PluginSettingsUI(typeof(Telegram))]
    public class Settings : IPluginPage
    {
        public Page Fetch(Service service)
        {
            NavigationPage navigationPage;
            if (ServiceManager.IsManualSettingsNeeded(service))
            {
                navigationPage = new NavigationPage(Setup.Fetch(service));
            }
            else
            {
				var main  = new Main(service);
				var help = new ToolbarItem();
				help.Text = Localize.GetString("TelegramHelp");
				help.Order = ToolbarItemOrder.Primary;
				help.Clicked += (sender, e) => 
				{
					Platform.LaunchViewIntent("http://www.disa.im/telegram.html");
				};
				main.ToolbarItems.Add(help);
                navigationPage = new NavigationPage(main);
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
                _setupWizard.Clicked += async (sender, e) => { await Navigation.PushAsync(Setup.Fetch(service)); };
                if (running)
                {
                    _profileSettings = new Button();
                    _profileSettings.Text = Localize.GetString("TelegramProfileSettings");
                    _profileSettings.TextColor = Color.White;
                    _profileSettings.BackgroundColor = Color.FromHex("77D065");
                    _profileSettings.Clicked +=
                        async (sender, e) => { await Navigation.PushAsync(new ProfileSettings(service)); };
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


                Content = new ScrollView {Content = stackLayout};
                Title = Localize.GetString("TelegramSettingsTitle");
            }
        }

        private class ProfileSettings : ContentPage
        {
            private StackLayout _groups;
            private StackLayout _lastSeen;
            private Label _error;
            private ActivityIndicator _progressBar;
            private Picker _lastSeenPicker;
            private Picker _groupsPicker;
            private UserProfile _userProfile;
            private Button _privacyList;
			private UserInformationEditor _userInformationEditor;

            private string[] _privacyOptionsLastSeenStrings =
            {
                Localize.GetString("TelegramEveryone"),
                Localize.GetString("TelegramContact"), Localize.GetString("TelegramNobody")
            };

            private string[] _privacyOptionsGroupStrings =
            {
                Localize.GetString("TelegramEveryone"),
                Localize.GetString("TelegramContact")
            };

            public ProfileSettings(Service service)
            {
                if (!ServiceManager.IsRunning(service))
                {
                    Content = new Label
                    {
                        Text = Localize.GetString("TelegramNotRunningLong"),
                        VerticalOptions = LayoutOptions.CenterAndExpand,
                        HorizontalOptions = LayoutOptions.CenterAndExpand,
                        XAlign = TextAlignment.Center,
                        Font = Font.SystemFontOfSize(18)
                    };
                    Title = Localize.GetString("TelegramSettingsTitle");
                    Padding = 30;
                    return;
                }

                Label groupsHeader = new Label
                {
                    Text = Localize.GetString("TelegramGroups"),
                    Font = Font.BoldSystemFontOfSize(16),
                };
                _groupsPicker = new Picker
                {
                    Title = Localize.GetString("TelegramSelect"),
                };

                foreach (var item in _privacyOptionsGroupStrings)
                {
                    _groupsPicker.Items.Add(item);
                }

                _groups = new StackLayout();
                _groups.Spacing = 10;
                _groups.Orientation = StackOrientation.Vertical;
                _groups.Children.Add(groupsHeader);
                _groups.Children.Add(_groupsPicker);


                Label lastSeenHeader = new Label
                {
                    Text = Localize.GetString("TelegramLastSeen"),
                    Font = Font.BoldSystemFontOfSize(16),
                };
                _lastSeenPicker = new Picker
                {
                    Title = Localize.GetString("TelegramSelect"),
                };
                foreach (var item in _privacyOptionsLastSeenStrings)
                {
                    _lastSeenPicker.Items.Add(item);
                }
                _lastSeen = new StackLayout();
                _lastSeen.Spacing = 10;
                _lastSeen.Orientation = StackOrientation.Vertical;
                _lastSeen.Children.Add(lastSeenHeader);
                _lastSeen.Children.Add(_lastSeenPicker);

                _error = new Label
                {
                    VerticalOptions = LayoutOptions.CenterAndExpand,
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    XAlign = TextAlignment.Center,
                    Font = Font.SystemFontOfSize(18),
                    IsVisible = false
                };

                _progressBar = new ActivityIndicator();
                _progressBar.VerticalOptions = LayoutOptions.CenterAndExpand;
                _progressBar.IsRunning = true;
                _progressBar.IsVisible = false;

                _userProfile = new UserProfile
                {
                    StatusIsVisible = false,
                    CanSetEmptyStatus = false,
                    CanRemoveThumbnail = true,
                };

				_userInformationEditor = new UserInformationEditor(service, DisplayAlert);

                _privacyList = new Button
                {
                    Text = Localize.GetString("TelegramPrivacyList"),
                };
                _privacyList.Clicked +=
                    (sender, e) => { DependencyService.Get<IPluginPageControls>().LaunchPrivacyList(); };

                var page = new StackLayout();
                page.Spacing = 20;
                page.Padding = 30;
                page.Children.Add(_userProfile);
				page.Children.Add(_userInformationEditor);
                page.Children.Add(_lastSeen);
                page.Children.Add(_groups);
                page.Children.Add(_error);
                page.Children.Add(_progressBar);
                page.Children.Add(_privacyList);

                Populate(service);

                Content = new ScrollView
                {
                    Content = page
                };
                Title = Localize.GetString("TelegramSettingsTitle");
            }

			private class UserInformationEditor : StackLayout
			{
				private static string FirstNameTitle
				{
					get
					{
						return Localize.GetString("TelegramFirstName");
					}
				}
				private static string LastNameTitle
				{
					get
					{
						return Localize.GetString("TelegramLastName");
					}
				}
				private static string UsernameTitle
				{
					get
					{
						return Localize.GetString("TelegramUsername");
					}
				}
				private static string EditTitle
				{
					get
					{
						return Localize.GetString("TelegramEdit");
					}
				}
				private static string EditDoneButton
				{
					get
					{
						return Localize.GetString("TelegramDone");
					}
				}
				private static string NoUsernameSet
				{
					get
					{
						return Localize.GetString("TelegramNoUsernameSet");
					}
				}
				private static string EnterUsername
				{
					get
					{
						return Localize.GetString("TelegramEnterUsername");
					}
				}
				private static string EnterLastName
				{
					get
					{
						return Localize.GetString("TelegramEnterLastName");
					}
				}
				private static string UsernameInfo
				{
					get
					{
						return Localize.GetString("TelegramUserInformation");
					}
				}
				private static string UsernameNotLongEnough
				{
					get
					{
						return Localize.GetString("TelegramUsernameNotLongEnough");
					}
				}
				private static string UsernameInvalid
				{
					get
					{
						return Localize.GetString("TelegramUsernameInvalid");
					}
				}
				private static string UsernameTaken
				{
					get
					{
						return Localize.GetString("TelegramUsernameTaken");
					}
				}
				private static string UsernameAvailable
				{
					get
					{
						return Localize.GetString("TelegramUsernameAvailable");
					}
				}
				private static string UsernameChecking
				{
					get
					{
						return Localize.GetString("TelegramUsernameChecking");
					}
				}
				private static string UsernameTryAgainLater
				{
					get
					{
						return Localize.GetString("TelegramUsernameTryAgainLater");
					}
				}
				private static string NameFirstNameEmpty
				{
					get
					{
						return Localize.GetString("TelegramNameFirstNameEmpty");
					}
				}
				private static string NameOk
				{
					get
					{
						return Localize.GetString("TelegramOkay");
					}
				}

				private StackLayout BuildNameLayoutViewer(string firstName, string lastName)
				{
					var first = new Label();
					first.FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label));
					first.Text = firstName;
					first.VerticalOptions = LayoutOptions.Center;
					var last = new Label();
					if (!string.IsNullOrWhiteSpace(lastName))
					{
						last.Text = lastName;
					}
					last.FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label));
					last.VerticalOptions = LayoutOptions.Center;
					var edit = new Button();
					edit.VerticalOptions = LayoutOptions.Start;
					edit.Text = EditTitle;
					var layout = new StackLayout { Orientation = StackOrientation.Horizontal };
					layout.Children.Add(first);
					layout.Children.Add(last);
					layout.Children.Add(edit);
					layout.VerticalOptions = LayoutOptions.Start;
					if (string.IsNullOrWhiteSpace(lastName))
					{
						last.IsVisible = false;
					}
					return layout;
				}

				private StackLayout BuildNameLayoutEditor(string firstName, string lastName)
				{
					var first = new Entry();
					first.Text = firstName;
					var last = new Entry();
					if (!string.IsNullOrWhiteSpace(lastName))
					{
						last.Text = lastName;
					}
					else
					{
						last.Placeholder = EnterLastName;
					}
					var firstTitle = new Label();
					firstTitle.Text = FirstNameTitle;
					firstTitle.Font = Font.BoldSystemFontOfSize(16);
					var lastTitle = new Label();
					lastTitle.Text = LastNameTitle;
					lastTitle.Font = Font.BoldSystemFontOfSize(16);
					var done = new Button();
					done.Text = EditDoneButton;
					var layout = new StackLayout { Orientation = StackOrientation.Vertical };
					layout.Children.Add(firstTitle);
					layout.Children.Add(first);
					layout.Children.Add(lastTitle);
					layout.Children.Add(last);
					layout.Children.Add(done);
					return layout;
				}

				private Layout BuildNameLayout(string firstName, string lastName)
				{
					var masterLayout = new StackLayout();
					var layoutViewer = BuildNameLayoutViewer(firstName, lastName);
					masterLayout.Children.Add(layoutViewer);
					var edit = layoutViewer.Children.OfType<Button>().First();
					Action switchToEditor = null;
					switchToEditor = () =>
					{
						var layoutEditor = BuildNameLayoutEditor(firstName, lastName);
						masterLayout.Children.Remove(layoutViewer);
						masterLayout.Children.Add(layoutEditor);
						var done = layoutEditor.Children.OfType<Button>().First();
						done.Clicked += async (sender2, e2) =>
						{
							var firstNameLayout = layoutEditor.Children.OfType<Entry>().First();
							var lastNameLayout = layoutEditor.Children.OfType<Entry>().Last();
							var firstNameTemp = firstNameLayout.Text;
							var lastNameTemp = lastNameLayout.Text;
							if (string.IsNullOrWhiteSpace(firstNameTemp))
							{
								await _displayAlert(string.Empty, NameFirstNameEmpty, NameOk);
							}
							else
							{
								if (string.IsNullOrWhiteSpace(lastNameTemp))
								{
									lastName = string.Empty;
								}
								try
								{
									await UpdateUserAccount(firstNameTemp, lastNameTemp);
									firstName = firstNameTemp;
									lastName = lastNameTemp;
								}
								catch
								{
									//fall-through
								}
							}
							layoutViewer = BuildNameLayoutViewer(firstName, lastName);
							masterLayout.Children.Remove(layoutEditor);
							masterLayout.Children.Add(layoutViewer);
							edit = layoutViewer.Children.OfType<Button>().First();
							edit.Clicked += (sender, e) =>
							{
								switchToEditor();
							};
						};
					};
					edit.Clicked += (sender, e) =>
					{
						switchToEditor();
					};
					return masterLayout;
				}

				private StackLayout BuildUsernameLayoutViewer(string currentUsername)
				{
					var username = new Label();
					if (!string.IsNullOrWhiteSpace(currentUsername))
					{
						username.FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label));
						username.Text = currentUsername;
					}
					else
					{
						username.FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label));
						username.FontAttributes = FontAttributes.Italic;
						username.Text = NoUsernameSet;
					}
					username.VerticalOptions = LayoutOptions.Center;
					var edit = new Button();
					edit.VerticalOptions = LayoutOptions.Start;
					edit.Text = EditTitle;
					var layout = new StackLayout { Orientation = StackOrientation.Horizontal };
					layout.Children.Add(username);
					layout.Children.Add(edit);
					layout.VerticalOptions = LayoutOptions.Start;
					return layout;
				}

				private StackLayout BuildUsernameLayoutEditor(string currentUsername)
				{
					var username = new Entry();
					var usernameErrorStatus = new Label();
					var defaultUsernameErrorStatusColor = usernameErrorStatus.TextColor;
					if (!string.IsNullOrWhiteSpace(currentUsername))
					{
						if (currentUsername.StartsWith("@") && currentUsername.Length >= 2)
						{
							currentUsername = currentUsername.Remove(0, 1);
						}
						username.Text = currentUsername;
					}
					else
					{
						username.Placeholder = EnterUsername;
					}
					username.TextChanged += async (sender, e) =>
					{
						usernameErrorStatus.TextColor = defaultUsernameErrorStatusColor;
						usernameErrorStatus.Text = UsernameChecking;
						var error = false;
						var green = false;
						var newUsername = e.NewTextValue;
						if (string.IsNullOrWhiteSpace(newUsername))
						{
							goto End;
						}
						if (newUsername.Length >= 1 && newUsername.Length < 5)
						{
							usernameErrorStatus.Text = UsernameNotLongEnough;
							error = true;
							goto End;
						}
						const string validCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";
						foreach (var chr in newUsername)
						{
							var found = false;
							foreach (var validChar in validCharacters)
							{
								if (validChar == chr)
								{
									found = true;
									break;
								}
							}
							if (!found)
							{
								usernameErrorStatus.Text = UsernameInvalid;
								error = true;
								goto End;
							}
						}
						bool checkUsername = false;
						try
						{
							checkUsername = await CheckUsername(newUsername);
						}
						catch (Exception ex)
						{
							// fall-through
						}
						if (!checkUsername)
						{
							usernameErrorStatus.Text = UsernameTaken;
							error = true;
						}
						else
						{
							usernameErrorStatus.Text = UsernameAvailable.Replace("[username]", newUsername);
							error = true;
							green = true;
						}
					End:
						usernameErrorStatus.TextColor = green ? Color.Green : Color.Red;
						usernameErrorStatus.IsVisible = error;
					};
					var usernameTitle = new Label();
					usernameTitle.Font = Font.BoldSystemFontOfSize(16);
					usernameTitle.Text = UsernameTitle;
					var info = new Label();
					info.Text = UsernameInfo;
					usernameErrorStatus.IsVisible = false;
					var done = new Button();
					done.Text = EditDoneButton;
					var layout = new StackLayout { Orientation = StackOrientation.Vertical };
					layout.Children.Add(usernameTitle);
					layout.Children.Add(username);
					layout.Children.Add(usernameErrorStatus);
					layout.Children.Add(info);
					layout.Children.Add(done);
					return layout;
				}

				private StackLayout BuildUsernameLayout(string username)
				{
					var masterLayout = new StackLayout();
					var layoutViewer = BuildUsernameLayoutViewer(username);
					masterLayout.Children.Add(layoutViewer);
					var edit = layoutViewer.Children.OfType<Button>().First();
					Action switchToEditor = null;
					switchToEditor = () =>
					{
						var layoutEditor = BuildUsernameLayoutEditor(username);
						masterLayout.Children.Remove(layoutViewer);
						masterLayout.Children.Add(layoutEditor);
						var done = layoutEditor.Children.OfType<Button>().First();
						done.Clicked += async (sender2, e2) =>
						{
							var usernameLayout = layoutEditor.Children.OfType<Entry>().First();
							var newUsername = usernameLayout.Text;
							if (string.IsNullOrWhiteSpace(newUsername))
							{
								newUsername = string.Empty;
							}
							var success = true;
							if (!string.IsNullOrWhiteSpace(newUsername))
							{
								try
								{
									if (!await CheckUsername(newUsername))
									{
										return;
									}
								}
								catch
								{
									success = false;
								}
							}
							try
							{
								await UpdateUsername(newUsername);
								success = true;
							}
							catch
							{
								success = false;
							}
							if (!string.IsNullOrWhiteSpace(newUsername) && !newUsername.StartsWith("@"))
							{
								newUsername = "@" + newUsername;
							}
							if (success)
							{
								username = newUsername;
								Username = username;
							}
							layoutViewer = BuildUsernameLayoutViewer(username);
							masterLayout.Children.Remove(layoutEditor);
							masterLayout.Children.Add(layoutViewer);
							edit = layoutViewer.Children.OfType<Button>().First();
							edit.Clicked += (sender, e) =>
							{
								switchToEditor();
							};
						};
					};
					edit.Clicked += (sender, e) =>
					{
						switchToEditor();
					};
					return masterLayout;
				}

				public string Username { get; set; }
				public string FirstName { get; set; }
				public string LastName { get; set; }
				public bool DisableNameInput { get; set; }

				private readonly Service _service;
				private readonly Func<string, string, string, Task> _displayAlert;

				public UserInformationEditor(Service service, Func<string, string, string, Task> displayAlert)
				{
					_service = service;
					_displayAlert = displayAlert;
				}

				private async Task<bool> CheckUsername(string username)
				{
					using (var client = new Telegram.FullClientDisposable(_service as Telegram))
					{
						return await client.Client.Methods.AccountCheckUsernameAsync(new AccountCheckUsernameArgs
						{
							Username = username
						});
					}
				}

				private async Task UpdateUserAccount(string firstName, string lastName)
				{
					if (string.IsNullOrWhiteSpace(lastName))
					{
						lastName = string.Empty;
					}
					using (var client = new Telegram.FullClientDisposable(_service as Telegram))
					{
						var user = await client.Client.Methods.AccountUpdateProfileAsync(new AccountUpdateProfileArgs
						{
							Flags = 3,
							FirstName = firstName,
							LastName = lastName,
						});
						(_service as Telegram).Dialogs.AddUser(user);
					}
				}

				private async Task UpdateUsername(string username)
				{
					using (var client = new Telegram.FullClientDisposable(_service as Telegram))
					{
						var user = await client.Client.Methods.AccountUpdateUsernameAsync(new AccountUpdateUsernameArgs
						{
							Username = username
						});
						(_service as Telegram).Dialogs.AddUser(user);
					}
				}

				public void Build()
				{
					if (!DisableNameInput && string.IsNullOrWhiteSpace(FirstName))
					{
						throw new ArgumentNullException("First name cannot be null.");
					}
					Orientation = StackOrientation.Vertical;
					if (!DisableNameInput)
					{
						Children.Add(BuildNameLayout(FirstName, LastName));
					}
					Children.Add(BuildUsernameLayout(Username));
				}
			}

            private async void Populate(Service service)
            {
                _progressBar.IsVisible = true;
                _lastSeen.IsVisible = false;
                _userProfile.IsVisible = false;
                _groups.IsVisible = false;
                _privacyList.IsVisible = false;

                var telegramService = service as Telegram;

                var user = telegramService.Dialogs.GetUser(telegramService.Settings.AccountId);

                DependencyService.Get<IPluginPageControls>().BackPressEnabled = false;

                //RPC call
                using (var client = new Telegram.FullClientDisposable(service as Telegram))
                {
                    var iStatusPrivacyRules =
                        await client.Client.Methods.AccountGetPrivacyAsync(new AccountGetPrivacyArgs
                        {
                            Key = new InputPrivacyKeyStatusTimestamp()
                        });

                    var statusPrivacyRules = iStatusPrivacyRules as AccountPrivacyRules;
                    if (statusPrivacyRules != null)
                    {
                        var statusPrivacyRule = GetPrivacyRule(statusPrivacyRules.Rules);
                        if (statusPrivacyRule is PrivacyValueAllowAll)
                        {
                            _lastSeenPicker.Title = _privacyOptionsLastSeenStrings[0];
                        }
                        else if (statusPrivacyRule is PrivacyValueAllowContacts)
                        {
                            _lastSeenPicker.Title = _privacyOptionsLastSeenStrings[1];
                        }
                        else
                        {
                            _lastSeenPicker.Title = _privacyOptionsLastSeenStrings[2];
                        }
                    }

                    var iChatPrivacyRules =
                        await client.Client.Methods.AccountGetPrivacyAsync(new AccountGetPrivacyArgs
                        {
                            Key = new InputPrivacyKeyChatInvite()
                        });

                    var chatPrivacyRules = iChatPrivacyRules as AccountPrivacyRules;
                    if (chatPrivacyRules != null)
                    {
                        var chatPrivacyRule = GetPrivacyRule(chatPrivacyRules.Rules);
                        if (chatPrivacyRule is PrivacyValueAllowAll)
                        {
                            _groupsPicker.Title = _privacyOptionsLastSeenStrings[0];
                        }
                        else if (chatPrivacyRule is PrivacyValueAllowContacts)
                        {
                            _groupsPicker.Title = _privacyOptionsLastSeenStrings[1];
                        }
                    }

                    var fileBytes = (UploadFile) await FetchUserThumbnail(user, telegramService, true);

                    if (fileBytes != null)
                    {
                        var thumbnail = new DisaThumbnail(telegramService, fileBytes.Bytes, "userprofile");

                        _userProfile.SetThumbnail(thumbnail);
                    }
                    else
                    {
                        _userProfile.SetThumbnail(null);
                    }

					_userInformationEditor.FirstName = TelegramUtils.GetUserName(user);

					var name = TelegramUtils.GetName(user);
					if (name != null)
					{
						_userInformationEditor.FirstName = name.Item1;
						_userInformationEditor.LastName = name.Item2;
					}
					else
					{
						_userInformationEditor.DisableNameInput = true;
					}


					var username = TelegramUtils.GetUserHandle(user);
					_userInformationEditor.Username = !string.IsNullOrWhiteSpace(username) ? username : null;

					_userInformationEditor.Build();
                }

                _progressBar.IsVisible = false;
                _lastSeen.IsVisible = true;
                _userProfile.IsVisible = true;
                _privacyList.IsVisible = true;
                _groups.IsVisible = true;


                _userProfile.FetchThumbnail = async () =>
                {
                    var fileBytes = (UploadFile) await FetchUserThumbnail(user, telegramService, false);

                    if (fileBytes == null)
                    {
                        return null;
                    }

                    return new DisaThumbnail(telegramService, fileBytes.Bytes, "userprofile");
                };

                _userProfile.ThumbnailChanged += async (sender, e) =>
                {
                    using (var client = new Telegram.FullClientDisposable(telegramService))
                    {
                        var resizedPhoto = Platform.GenerateJpegBytes(e, 640, 640);
                        var inputFile = await UploadProfilePhoto(telegramService, client.Client, resizedPhoto);
                        await SetProfilePhoto(telegramService, client.Client, inputFile);
                    }
                };

                _userProfile.ThumbnailRemoved += async (sender, e) =>
                {
                    using (var client = new Telegram.FullClientDisposable(telegramService))
                    {
                        await RemoveProfilePhoto(client.Client);
                    }
                };

                _groupsPicker.SelectedIndexChanged +=
                    async (sender, args) =>
                    {
                        await SendGroupPrivacyChangeUpdate(telegramService, _groupsPicker.SelectedIndex);
                    };

                _lastSeenPicker.SelectedIndexChanged +=
                    async (sender, args) =>
                    {
                        await SendLastSeenPrivacyChangeUpdate(telegramService, _lastSeenPicker.SelectedIndex);
                    };

                DependencyService.Get<IPluginPageControls>().BackPressEnabled = true;
            }

            private async Task SendLastSeenPrivacyChangeUpdate(Telegram telegramService, int selectedIndex)
            {
                IInputPrivacyKey key = new InputPrivacyKeyStatusTimestamp();
                switch (selectedIndex)
                {
                    case 0:
                        await SetPrivacyOptions(telegramService, key, new InputPrivacyValueAllowAll());
                        break;
                    case 1:
                        await SetPrivacyOptions(telegramService, key, new InputPrivacyValueAllowContacts());
                        break;
                    case 2:
                        await SetPrivacyOptions(telegramService, key, new InputPrivacyValueDisallowAll());
                        break;
                }
            }


            private async Task SendGroupPrivacyChangeUpdate(Telegram telegramService, int selectedIndex)
            {
                IInputPrivacyKey key = new InputPrivacyKeyChatInvite();
                switch (selectedIndex)
                {
                    case 0:
                        await SetPrivacyOptions(telegramService, key, new InputPrivacyValueAllowAll());
                        break;
                    case 1:
                        await SetPrivacyOptions(telegramService, key, new InputPrivacyValueAllowContacts());
                        break;
                }
            }

            private async Task SetPrivacyOptions(Telegram telegramService, IInputPrivacyKey key, IInputPrivacyRule rule)
            {
                using (var client = new Telegram.FullClientDisposable(telegramService))
                {
                    await client.Client.Methods.AccountSetPrivacyAsync(new AccountSetPrivacyArgs
                    {
                        Key = key,
                        Rules = new List<IInputPrivacyRule>
                        {
                            rule
                        }
                    });
                }
            }

            private async Task RemoveProfilePhoto(TelegramClient client)
            {
                await client.Methods.PhotosUpdateProfilePhotoAsync(new PhotosUpdateProfilePhotoArgs
                {
                    Id = new InputPhotoEmpty(),
                    Crop = new InputPhotoCropAuto()
                });
            }

            private async Task SetProfilePhoto(Telegram service, TelegramClient client, IInputFile inputFile)
            {
                var iPhoto = await client.Methods.PhotosUploadProfilePhotoAsync(new PhotosUploadProfilePhotoArgs
                {
                    Caption = "",
                    Crop = new InputPhotoCropAuto(),
                    File = inputFile,
                    GeoPoint = new InputGeoPointEmpty()
                });

                var photo = iPhoto as PhotosPhoto;
                if (photo != null)
                {
                    service.Dialogs.AddUsers(photo.Users);
                }

                var photoObj = photo.Photo as Photo;

                if (photoObj == null)
                {
                    return;
                }

                await client.Methods.PhotosUpdateProfilePhotoAsync(new PhotosUpdateProfilePhotoArgs
                {
                    Id = new InputPhoto
                    {
                        Id = photoObj.Id,
                    },
                    Crop = new InputPhotoCropAuto()
                });
            }

            private async Task<IInputFile> UploadProfilePhoto(Telegram service, TelegramClient client,
                byte[] resizedPhoto)
            {
                var fileId = service.GenerateRandomId();
                const int chunkSize = 65536;
                var chunk = new byte[chunkSize];
                uint chunkNumber = 0;
                var offset = 0;
                using (var memoryStream = new MemoryStream(resizedPhoto))
                {
                    int bytesRead;
                    while ((bytesRead = memoryStream.Read(chunk, 0, chunk.Length)) > 0)
                    {
                        //RPC call
                        var uploaded =
                            await client.Methods.UploadSaveFilePartAsync(new UploadSaveFilePartArgs
                            {
                                Bytes = chunk,
                                FileId = fileId,
                                FilePart = chunkNumber
                            });

                        if (!uploaded)
                        {
                            return null;
                        }
                        chunkNumber++;
                        offset += bytesRead;
                    }

                    return new InputFile
                    {
                        Id = fileId,
                        Md5Checksum = "",
                        Name = service.GenerateRandomId() + ".jpeg",
                        Parts = chunkNumber
                    };
                }
            }

            private async Task<IUploadFile> FetchUserThumbnail(IUser user, Telegram telegramService, bool small)
            {
                var thumbnailLocation = TelegramUtils.GetUserPhotoLocation(user, small);

                if (thumbnailLocation == null)
                {
                    return null;
                }

                if (thumbnailLocation.DcId == telegramService.Settings.NearestDcId)
                {
                    using (var clientDisposable = new Telegram.FullClientDisposable(telegramService))
                    {
                        return await FetchFileBytes(clientDisposable.Client, thumbnailLocation);
                    }
                }
                else
                {
                    try
                    {
                        var telegramClient = telegramService.GetClient((int) thumbnailLocation.DcId);
                        return await FetchFileBytes(telegramClient, thumbnailLocation);
                    }
                    catch (Exception ex)
                    {
                        Utils.DebugPrint("Failed to obtain client from DC manager: " + ex);
                        return null;
                    }
                }
            }

            private async Task<IUploadFile> FetchFileBytes(TelegramClient client, FileLocation thumbnailLocation)
            {
                return await client.Methods.UploadGetFileAsync(new UploadGetFileArgs
                {
                    Location = new InputFileLocation
                    {
                        VolumeId = thumbnailLocation.VolumeId,
                        LocalId = thumbnailLocation.LocalId,
                        Secret = thumbnailLocation.Secret
                    },
                    Offset = 0,
                    Limit = uint.MaxValue,
                });
            }

            private IPrivacyRule GetPrivacyRule(List<IPrivacyRule> rules)
            {
                IPrivacyRule privacyRule = null;
                foreach (var rule in rules)
                {
                    if (rule is PrivacyValueAllowAll)
                    {
                        privacyRule = rule as PrivacyValueAllowAll;
                        break;
                    }
                    if (rule is PrivacyValueAllowContacts)
                    {
                        privacyRule = rule as PrivacyValueAllowContacts;
                        break;
                    }
                }
                if (privacyRule == null)
                {
                    privacyRule = new PrivacyValueDisallowAll();
                }
                return privacyRule;
            }
        }
    }
}