using System;
using System.Threading.Tasks;

namespace Disa.Framework
{
    [DisaFrameworkDeprecated]
    public interface IUserInformationExtended
    {
        Task IsUserBotStopped(string address, Action<bool> result);

        Task EnableUserBot(string address, bool enable, Action<bool> result);

        Task IsUserBot(string address, Action<bool> result);
    }

}

