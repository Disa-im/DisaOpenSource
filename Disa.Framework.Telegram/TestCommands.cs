using System;
using SharpTelegram;
using SharpMTProto;
using SharpTelegram.Schema;
using System.Linq;
using SharpMTProto.Transport;

namespace Disa.Framework.Telegram
{
    public partial class Telegram
    {
        public async void DoCommand(string[] args)
        {
            var command = args[0].ToLower();

            switch (command)
            {
                case "setup":
                    {
                        DebugPrint("Fetching nearest DC...");
                        var telegramSettings = new TelegramSettings();
                        var authInfo = await FetchNewAuthentication(DefaultTransportConfig);
                        using (var client = new TelegramClient(DefaultTransportConfig, 
                            new ConnectionConfig(authInfo.AuthKey, authInfo.Salt), AppInfo))
                        {
                            await client.Connect();
                            var nearestDcId = (NearestDc)await(client.Methods.HelpGetNearestDcAsync(new HelpGetNearestDcArgs{}));
                            var config = (Config)await(client.Methods.HelpGetConfigAsync(new HelpGetConfigArgs{ }));
                            var dcOption = config.DcOptions.OfType<DcOption>().FirstOrDefault(x => x.Id == nearestDcId.NearestDcProperty);
                            telegramSettings.NearestDcId = nearestDcId.NearestDcProperty;
                            telegramSettings.NearestDcIp = dcOption.IpAddress;
                            telegramSettings.NearestDcPort = (int)dcOption.Port;
                        }
                        DebugPrint("Generating authentication on nearest DC...");
                        var authInfo2 = await FetchNewAuthentication(
                            new TcpClientTransportConfig(telegramSettings.NearestDcIp, telegramSettings.NearestDcPort));
                        telegramSettings.AuthKey = authInfo2.AuthKey;
                        telegramSettings.Salt = authInfo2.Salt;
                        SettingsManager.Save(this, telegramSettings);
                        DebugPrint("Great! Ready for the service to start.");
                    }
                    break;
                case "sendcode":
                    {
                        var number = args[1];
                        var transportConfig = 
                            new TcpClientTransportConfig(_settings.NearestDcIp, _settings.NearestDcPort);
                        using (var client = new TelegramClient(transportConfig, 
                            new ConnectionConfig(_settings.AuthKey, _settings.Salt), AppInfo))
                        {
                            await client.Connect();
                            var result = await client.Methods.AuthSendCodeAsync(new AuthSendCodeArgs
                            {
                                PhoneNumber = number,
                                SmsType = 0,
                                ApiId = AppInfo.ApiId,
                                ApiHash = "f8f2562579817ddcec76a8aae4cd86f6",
                                LangCode = PhoneBook.Language
                            });
                            DebugPrint(ObjectDumper.Dump(result));
                        }
                    }
                    break;
                case "signin":
                    {
                        var number = args[1];
                        var hash = args[2];
                        var code = args[3];
                        var transportConfig = 
                            new TcpClientTransportConfig(_settings.NearestDcIp, _settings.NearestDcPort);
                        using (var client = new TelegramClient(transportConfig, 
                            new ConnectionConfig(_settings.AuthKey, _settings.Salt), AppInfo))
                        {
                            await client.Connect();
                            var result = (AuthAuthorization)await client.Methods.AuthSignInAsync(new AuthSignInArgs
                            {
                                PhoneNumber = number,
                                PhoneCodeHash = hash,
                                PhoneCode = code,
                            });
                            DebugPrint(ObjectDumper.Dump(result));
                        }
                    }
                    break;
                case "signup":
                    {
                        var number = args[1];
                        var hash = args[2];
                        var code = args[3];
                        var firstName = args[4];
                        var lastName = args[5];
                        var transportConfig = 
                            new TcpClientTransportConfig(_settings.NearestDcIp, _settings.NearestDcPort);
                        using (var client = new TelegramClient(transportConfig, 
                            new ConnectionConfig(_settings.AuthKey, _settings.Salt), AppInfo))
                        {
                            await client.Connect();
                            var result = (AuthAuthorization)await client.Methods.AuthSignUpAsync(new AuthSignUpArgs
                            {
                                PhoneNumber = number,
                                PhoneCodeHash = hash,
                                PhoneCode = code,
                                FirstName = firstName,
                                LastName = lastName,
                            });
                            DebugPrint(ObjectDumper.Dump(result));
                        }
                    }
                    break;
                case "getcontacts":
                    {
                        var result = await _fullClient.Methods.ContactsGetContactsAsync(new ContactsGetContactsArgs
                        {
                            Hash = string.Empty
                        });
                        DebugPrint(ObjectDumper.Dump(result));
                    }
                    break;
                case "sendhello":
                    {
                        var contacts = (ContactsContacts)await _fullClient.Methods.ContactsGetContactsAsync(new ContactsGetContactsArgs
                        {
                            Hash = string.Empty
                        });
                        var counter = 0;
                        Console.WriteLine("Pick a contact:");
                        foreach (var icontact in contacts.Users)
                        {
                            var contact = icontact as UserContact;
                            if (contact == null)
                                continue;
                            Console.WriteLine(counter++ + ") " + contact.FirstName + " " + contact.LastName);
                        }
                        var choice = int.Parse(Console.ReadLine());
                        var chosenContact = (UserContact)contacts.Users[choice];
                        var result = await _fullClient.Methods.MessagesSendMessageAsync(new MessagesSendMessageArgs
                        {
                            Peer = new InputPeerContact
                            {
                                UserId = chosenContact.Id,
                            },
                            Message = "Hello from Disa!",
                            RandomId = (ulong)Time.GetNowUnixTimestamp(),
                        });
                        Console.WriteLine(ObjectDumper.Dump(result));
                    }
                    break;
            }

        }

    }
}

