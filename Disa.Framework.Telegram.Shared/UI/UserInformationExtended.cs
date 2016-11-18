using System;
using System.Threading.Tasks;
using SharpTelegram.Schema;
using System.Linq;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IUserInformationExtended
    {
        // TODO: Remove, testing
        private bool _isBotStopped;

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
                // TODO
                result(_isBotStopped);
            });
        }

        public Task EnableUserBot(string address, bool enable, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                _isBotStopped = !enable;

                // TODO
                result(true);
            });
        }
    }
}

