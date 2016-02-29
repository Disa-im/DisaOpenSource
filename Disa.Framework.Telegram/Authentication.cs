using System;
using SharpTelegram;
using SharpMTProto;
using SharpTelegram.Schema.Layer18;
using SharpMTProto.Transport;
using SharpMTProto.Schema;
using System.Threading.Tasks;
using SharpMTProto.Authentication;
using System.Linq;

namespace Disa.Framework.Telegram
{
    public partial class Telegram
    {
        public static TelegramSettings GenerateAuthentication(Service service)
        {
            try
            {
                service.DebugPrint("Fetching nearest DC...");
                var settings = new TelegramSettings();
                var authInfo = TelegramUtils.RunSynchronously(FetchNewAuthentication(DefaultTransportConfig));
                using (var client = new TelegramClient(DefaultTransportConfig, 
                    new ConnectionConfig(authInfo.AuthKey, authInfo.Salt), AppInfo))
                {
                    TelegramUtils.RunSynchronously(client.Connect());
                    var nearestDcId = (NearestDc)TelegramUtils.RunSynchronously(client.Methods.HelpGetNearestDcAsync(new HelpGetNearestDcArgs{}));
                    var config = (Config)TelegramUtils.RunSynchronously(client.Methods.HelpGetConfigAsync(new HelpGetConfigArgs{ }));
                    var dcOption = config.DcOptions.OfType<DcOption>().FirstOrDefault(x => x.Id == nearestDcId.NearestDcProperty);
                    settings.NearestDcId = nearestDcId.NearestDcProperty;
                    settings.NearestDcIp = dcOption.IpAddress;
                    settings.NearestDcPort = (int)dcOption.Port;
                }
                service.DebugPrint("Generating authentication on nearest DC...");
                var authInfo2 = TelegramUtils.RunSynchronously(FetchNewAuthentication(
                    new TcpClientTransportConfig(settings.NearestDcIp, settings.NearestDcPort)));
                settings.AuthKey = authInfo2.AuthKey;
                settings.Salt = authInfo2.Salt;
                service.DebugPrint("Great! Ready for the service to start.");
                return settings;
            }
            catch (Exception ex)
            {
                service.DebugPrint("Error in GenerateAuthentication: " + ex);
            }
            return null;
        }

        public class CodeRequest
        {
            public enum Type { Success, Failure, NumberInvalid, Migrate }

            public bool Registered { get; set; }
            public string CodeHash { get; set; }
            public Type Response { get; set; }
        }

        public static CodeRequest RequestCode(Service service, string number, string codeHash, TelegramSettings settings, bool call)
        {
            try
            {
                service.DebugPrint("Requesting code...");
                var transportConfig = 
                    new TcpClientTransportConfig(settings.NearestDcIp, settings.NearestDcPort);
                using (var client = new TelegramClient(transportConfig,
                    new ConnectionConfig(settings.AuthKey, settings.Salt), AppInfo))
                {
                    TelegramUtils.RunSynchronously(client.Connect());

                    if (!call)
                    {
                        try
                        {
                            var result = TelegramUtils.RunSynchronously(client.Methods.AuthSendCodeAsync(new AuthSendCodeArgs
                            {
                                PhoneNumber = number,
                                SmsType = 0,
                                ApiId = AppInfo.ApiId,
                                ApiHash = "f8f2562579817ddcec76a8aae4cd86f6",
                                LangCode = PhoneBook.Language
                            })) as AuthSentCode;
                            return new CodeRequest
                            {
                                Registered = result.PhoneRegistered,
                                CodeHash = result.PhoneCodeHash,
                            };
                        }
                        catch (RpcErrorException ex)
                        {
                            var error = (RpcError)ex.Error;
                            var cr = new CodeRequest();
                            var response = CodeRequest.Type.Failure;
                            switch (error.ErrorCode)
                            {
                                case 400:
                                    cr.Response = CodeRequest.Type.NumberInvalid;
                                    break;
                                default:
                                    cr.Response = CodeRequest.Type.Failure;
                                    break;
                            }
                            return cr;
                        }
                    }
                    var result2 = (bool)TelegramUtils.RunSynchronously(client.Methods.AuthSendCallAsync(new AuthSendCallArgs
                    {
                        PhoneNumber = number,
                        PhoneCodeHash = codeHash,
                    }));
                    return new CodeRequest
                    {
                        Response = result2 ? CodeRequest.Type.Success : CodeRequest.Type.Failure
                    };
                }
            }
            catch (Exception ex)
            {
                service.DebugPrint("Error in CodeRequest: " + ex);
            }
            return null;
        }

        public class CodeRegister
        {
            public enum Type { Success, Failure, NumberInvalid, CodeEmpty, CodeExpired, CodeInvalid, FirstNameInvalid, LastNameInvalid }

            public uint AccountId { get; set; }

            public long Expires { get; set; }
            public Type Response { get; set; }
        }

        public static CodeRegister RegisterCode(Service service, TelegramSettings settings, string number, string codeHash, string code, string firstName, string lastName, bool signIn)
        {
            try
            {
                service.DebugPrint("Registering code...");
                var transportConfig = 
                    new TcpClientTransportConfig(settings.NearestDcIp, settings.NearestDcPort);
                using (var client = new TelegramClient(transportConfig,
                    new ConnectionConfig(settings.AuthKey, settings.Salt), AppInfo))
                {
                    TelegramUtils.RunSynchronously(client.Connect());

                    try
                    {
                        IAuthAuthorization iresult = null;
                        if (signIn)
                        {
                            iresult = TelegramUtils.RunSynchronously(client.Methods.AuthSignInAsync(new AuthSignInArgs
                            {
                                PhoneNumber = number,
                                PhoneCodeHash = codeHash,
                                PhoneCode = code,
                            }));
                        }
                        else
                        {
                            iresult = TelegramUtils.RunSynchronously(client.Methods.AuthSignUpAsync(new AuthSignUpArgs
                            {
                                PhoneNumber = number,
                                PhoneCodeHash = codeHash,
                                PhoneCode = code,
                                FirstName = firstName,
                                LastName = lastName,
                            }));
                        }
                        var result = (AuthAuthorization)iresult;
                        return new CodeRegister
                        {
                            AccountId = (result.User as UserSelf).Id,
                            Expires = result.Expires,
                            Response = CodeRegister.Type.Success,
                        };
                    }
                    catch (RpcErrorException ex)
                    {
                        var error = (RpcError)ex.Error;
                        var cr = new CodeRegister();
                        var response = CodeRegister.Type.Failure;
                        switch (error.ErrorMessage)
                        {
                            case "PHONE_NUMBER_INVALID":
                                cr.Response = CodeRegister.Type.NumberInvalid;
                                break;
                            case "PHONE_CODE_EMPTY":
                                cr.Response = CodeRegister.Type.CodeEmpty;
                                break;
                            case "PHONE_CODE_EXPIRED":
                                cr.Response = CodeRegister.Type.CodeExpired;
                                break;
                            case "PHONE_CODE_INVALID":
                                cr.Response = CodeRegister.Type.CodeInvalid;
                                break;
                            case "FIRSTNAME_INVALID":
                                cr.Response = CodeRegister.Type.FirstNameInvalid;
                                break;
                            case "LASTNAME_INVALID":
                                cr.Response = CodeRegister.Type.LastNameInvalid;
                                break;
                            default:
                                cr.Response = CodeRegister.Type.Failure;
                                break;
                        }
                        return cr;
                    }
                }
            }
            catch (Exception ex)
            {
                service.DebugPrint("Error in CodeRequest: " + ex);
            }
            return null;
        }

        private static async Task<AuthInfo> FetchNewAuthentication(TcpClientTransportConfig config)
        {
            var authKeyNegotiater = MTProtoClientBuilder.Default.BuildAuthKeyNegotiator(config);
            authKeyNegotiater.KeyChain.Add(RSAPublicKey.Get());

            return await authKeyNegotiater.CreateAuthKey();
        }
    }
}

