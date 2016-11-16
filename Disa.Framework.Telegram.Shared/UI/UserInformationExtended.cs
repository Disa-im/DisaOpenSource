using System;
using System.Threading.Tasks;
using SharpTelegram.Schema;
using System.Linq;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IUserInformationExtended
    {
        public Task IsUserBotStopped(Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // do stuff here
                result(false);
            });
        }

        public Task EnableUserBot(bool enable, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // do stuff here
                result(false);
            });
        }
    }
}

