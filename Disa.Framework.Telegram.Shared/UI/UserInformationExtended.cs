using System;
using System.Threading.Tasks;
using SharpTelegram.Schema;
using System.Linq;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IUserInformationExtended
    {
        public Task IsUserBot(string address, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var user = _dialogs.GetUser(uint.Parse(address)) as User;

                result(user.Bot != null);
            });
        }

        public Task IsUserBotStopped(string address, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                GetPrivacyList((addresses) =>
                {
                    var isUserBotStopped = addresses.Contains(address);
                    result(isUserBotStopped);
                });
            });
        }

        public Task EnableUserBot(string address, bool enable, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                GetPrivacyList((addresses) =>
                {
                    var newPrivacyList = addresses.ToList();
                    if (enable)
                    {
                        newPrivacyList.Remove(address);
                    }
                    else
                    {
                        newPrivacyList.Add(address);
                    }

                    SetPrivacyList(newPrivacyList.ToArray())
                        .ContinueWith((t) => { result(true); });
                });
            });
        }
    }
}

