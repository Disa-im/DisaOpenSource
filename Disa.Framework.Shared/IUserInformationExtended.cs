using System;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public interface IUserInformationExtended
    {
        Task IsUserBotStopped(Action<bool> result);

        Task EnableUserBot(bool enable, Action<bool> result);
    }
}

