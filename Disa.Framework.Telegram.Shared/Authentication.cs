using System;
using SharpTelegram;
using SharpMTProto;
using SharpTelegram.Schema;
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
                    var nearestDcId = (NearestDc)TelegramUtils.RunSynchronously(client.Methods.HelpGetNearestDcAsync(new HelpGetNearestDcArgs { }));
                    var config = (Config)TelegramUtils.RunSynchronously(client.Methods.HelpGetConfigAsync(new HelpGetConfigArgs { }));
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
            public enum AuthType { Telegram, Text, Phone, Unknown }

            public bool Registered { get; set; }
            public string CodeHash { get; set; }
            public Type Response { get; set; }
            public int MigrateId { get; set; }
            public AuthType CurrentType { get; set; }
            public AuthType NextType { get; set; }
        }

        public static CodeRequest RequestCode(Service service, string number, string codeHash, TelegramSettings settings, bool reVerify)
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
                    try
                    {
                        AuthSentCode result;
                        if (reVerify)
                        {
                            result = TelegramUtils.RunSynchronously(client.Methods.AuthResendCodeAsync(new AuthResendCodeArgs
                            {
                                PhoneNumber = number,
                                PhoneCodeHash = codeHash
                            })) as AuthSentCode;
                        }
                        else
                        {
                            result = TelegramUtils.RunSynchronously(client.Methods.AuthSendCodeAsync(new AuthSendCodeArgs
                            {
                                PhoneNumber = number,
                                ApiId = AppInfo.ApiId,
                                ApiHash = "f8f2562579817ddcec76a8aae4cd86f6",
                            })) as AuthSentCode;
                        }
                        return new CodeRequest
                        {
                            Registered = result.PhoneRegistered != null ? true : false,
                            CodeHash = result.PhoneCodeHash,
                            CurrentType = GetAuthSentType(result.Type),
                            NextType = GetAuthType(result.NextType)
                        };
                    }
                    catch (RpcErrorException ex)
                    {
                        Utils.DebugPrint(">>>> Send code failure " + ObjectDumper.Dump(ex));
                        var error = (RpcError)ex.Error;
                        var cr = new CodeRequest();
                        var response = CodeRequest.Type.Failure;
                        switch (error.ErrorCode)
                        {
                            case 400:
                                cr.Response = CodeRequest.Type.NumberInvalid;
                                break;
                            case 303:
                                var newDcId = GetDcId(error.ErrorMessage);
                                cr.Response = CodeRequest.Type.Migrate;
                                cr.MigrateId = newDcId;
                                break;
                            default:
                                cr.Response = CodeRequest.Type.Failure;
                                break;
                        }
                        return cr;
                    }

                    return new CodeRequest
                    {
                        Response = CodeRequest.Type.Success
                    };
                }
            }
            catch (Exception ex)
            {
                service.DebugPrint("Error in CodeRequest: " + ex);
            }
            return null;
        }

        static CodeRequest.AuthType GetAuthType(IAuthCodeType nextType)
        {
            var authCodeTypeSms = nextType as AuthCodeTypeSms;
            var authCodeTypeCall = nextType as AuthCodeTypeCall;

            if (authCodeTypeSms != null)
            {
                return CodeRequest.AuthType.Text;
            }
            if (authCodeTypeCall != null)
            {
                return CodeRequest.AuthType.Phone;
            }
            return CodeRequest.AuthType.Unknown;
        }

        private static CodeRequest.AuthType GetAuthSentType(IAuthSentCodeType type)
        {
            var authSentCodeTypeApp = type as AuthSentCodeTypeApp;
            var authSentCodeTypeSms = type as AuthSentCodeTypeSms;
            var authSentCodeTypeCall = type as AuthSentCodeTypeCall;

            if (authSentCodeTypeApp != null)
            {
                return CodeRequest.AuthType.Telegram;
            }
            if (authSentCodeTypeSms != null)
            {
                return CodeRequest.AuthType.Text;
            }
            if (authSentCodeTypeCall != null)
            {
                return CodeRequest.AuthType.Phone;
            }
            return CodeRequest.AuthType.Unknown;
        }

        public static TelegramClient GetNewClient(int migrateId, TelegramSettings settings, out TelegramSettings newSettings)
        {

            var dcConfig = GetDcConfig(migrateId, settings);

            var transportConfig = new TcpClientTransportConfig(dcConfig.IpAddress, (int)dcConfig.Port);

            var authInfo = TelegramUtils.RunSynchronously(FetchNewAuthentication(transportConfig));

            var newClient = new TelegramClient(transportConfig, new ConnectionConfig(authInfo.AuthKey, authInfo.Salt), AppInfo);

            newSettings = new TelegramSettings();
            newSettings.AccountId = settings.AccountId;
            newSettings.AuthKey = authInfo.AuthKey;
            newSettings.Salt = authInfo.Salt;
            newSettings.NearestDcId = (uint)migrateId;
            newSettings.NearestDcIp = dcConfig.IpAddress;
            newSettings.NearestDcPort = (int)dcConfig.Port;

            return newClient;
        }

        public static DcOption GetDcConfig(int migrateId, TelegramSettings settings)
        {
            var transportConfig =
                    new TcpClientTransportConfig(settings.NearestDcIp, settings.NearestDcPort);
            using (var client = new TelegramClient(transportConfig,
                                                   new ConnectionConfig(settings.AuthKey, settings.Salt), AppInfo)) 
            {
                TelegramUtils.RunSynchronously(client.Connect());
                var config = (Config)TelegramUtils.RunSynchronously(client.Methods.HelpGetConfigAsync(new HelpGetConfigArgs { }));
                var dcOption = config.DcOptions.OfType<DcOption>().FirstOrDefault(x => x.Id == migrateId);
                return dcOption;
            }
        }


        public static AuthExportedAuthorization GenerateExportedAuth(int migrateId, TelegramSettings settings)
        {
            var transportConfig =
                   new TcpClientTransportConfig(settings.NearestDcIp, settings.NearestDcPort);
            using (var client = new TelegramClient(transportConfig,
                                                   new ConnectionConfig(settings.AuthKey, settings.Salt), AppInfo))
            {
                TelegramUtils.RunSynchronously(client.Connect());
                var exportedAuth = (AuthExportedAuthorization)TelegramUtils.RunSynchronously(client.Methods.AuthExportAuthorizationAsync(
                            new SharpTelegram.Schema.AuthExportAuthorizationArgs
                            {
                                DcId = (uint)migrateId,
                            }));
                return exportedAuth;
            }


        }

        private static int GetDcId(string errorMessage)
        {
            var messageParts = errorMessage.Split('_');
            int dcId = -1;
            int.TryParse(messageParts[messageParts.Length - 1],out dcId);
            return dcId;
        }

        public class CodeRegister
        {
            public enum Type { Success, Failure, NumberInvalid, CodeEmpty, CodeExpired, CodeInvalid, FirstNameInvalid, LastNameInvalid, PasswordNeeded, InvalidPassword }

            public uint AccountId { get; set; }
            public long Expires { get; set; }
            public Type Response { get; set; }
            public byte[] CurrentSalt { get; set; }
            public byte[] NewSalt { get; set; }
            public string Hint { get; set; }
            public bool HasRecovery { get; set; }
        }


        public static CodeRegister VerifyPassword(Service service, TelegramSettings settings, byte[] passwordHash)
        {
            try
            {
                var transportConfig =
                      new TcpClientTransportConfig(settings.NearestDcIp, settings.NearestDcPort);
                using (var client = new TelegramClient(transportConfig,
                    new ConnectionConfig(settings.AuthKey, settings.Salt), AppInfo))
                {
                    TelegramUtils.RunSynchronously(client.Connect());
                    try
                    {
                        var iresult = TelegramUtils.RunSynchronously(client.Methods.AuthCheckPasswordAsync(new AuthCheckPasswordArgs
                        {
                            PasswordHash = passwordHash
                        }));
                        var result = (AuthAuthorization)iresult;
                        return new CodeRegister
                        {
                            AccountId = (result.User as User).Id,
                            Response = CodeRegister.Type.Success
                        };
                    }
                    catch (RpcErrorException ex)
                    {
                        var error = (RpcError)ex.Error;
                        var cr = new CodeRegister();
                        if (error.ErrorMessage == "PASSWORD_HASH_INVALID")
                        {
                            cr.Response = CodeRegister.Type.InvalidPassword;
                        }
                        return cr;
                    }
                }
            }
            catch (Exception ex)
            { 
                service.DebugPrint("Error in VerifyPassword: " + ex);
                return null;
            }
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
                            AccountId = (result.User as User).Id,
                            Response = CodeRegister.Type.Success,
                        };
                    }
                    catch (RpcErrorException ex)
                    {
                        Utils.DebugPrint(">>>>>> Failed to sign in " + ex);
                        var error = (RpcError)ex.Error;
                        var cr = new CodeRegister();
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
                            case "SESSION_PASSWORD_NEEDED":
                                cr.Response = CodeRegister.Type.PasswordNeeded;
                                GetPasswordDetails(client, cr);
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

        private static void GetPasswordDetails(TelegramClient client, CodeRegister cr)
        {
            var result = TelegramUtils.RunSynchronously(client.Methods.AccountGetPasswordAsync(new AccountGetPasswordArgs
            {
            }));
            var authPassword = result as AccountPassword;
            var authNoPassword = result as AccountNoPassword;

            if (authPassword != null)
            {
                cr.CurrentSalt = authPassword.CurrentSalt;
                cr.NewSalt = authPassword.NewSalt;
                cr.HasRecovery = authPassword.HasRecovery;
                cr.Hint = authPassword.Hint;
            }
        }

        public static async Task<AuthInfo> FetchNewAuthentication(TcpClientTransportConfig config)
        {
            var authKeyNegotiater = MTProtoClientBuilder.Default.BuildAuthKeyNegotiator(config);
            authKeyNegotiater.KeyChain.Add(RSAPublicKey.Get());

            return await authKeyNegotiater.CreateAuthKey();
        }
    }
}

