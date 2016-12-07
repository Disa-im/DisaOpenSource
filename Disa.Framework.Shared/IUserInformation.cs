using System;
using System.Threading.Tasks;

namespace Disa.Framework
{
    [DisaFramework]
    public interface IUserInformation
    {
        Task GetUserInformationThumbnail(string address, bool preview, Action<DisaThumbnail> result);

        Task GetUserInformationSecondaryActionThumbnail(string address, Action<byte[]> result);

        Task ClickUserInformationSecondaryAction(string address);

        Task GetUserInformationPrimaryActionThumbnail(string address, Action<byte[]> result);

        Task ClickUserInformationPrimaryAction(string address);

        Task GetUserInformation(string address, Action<UserInformation> result);

        //
        // Begin IUserInformation new methods (previously IUserInformationExtended)
        //

        Task IsUserBotStopped(string address, Action<bool> result);

        Task EnableUserBot(string address, bool enable, Action<bool> result);

        Task IsUserBot(string address, Action<bool> result);

    }
}

