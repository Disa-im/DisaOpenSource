using SharpMTProto;
using SharpTelegram.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IMentions
    {
        public Task GetUsernameMentionsToken(Action<string> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result("@");
            });
        }
    }
}

