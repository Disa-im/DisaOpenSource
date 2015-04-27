using System;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public interface IUserInformation
    {
        Task GetUserInformationThumbnail(string address, bool preview, Action<DisaThumbnail> result);

        Task GetUserInformationSecondaryActionThumbnail(string address, Action<byte[]> result);

        Task ClickUserInformationSecondaryAction(string address);

        Task GetUserInformationPrimaryActionThumbnail(string address, Action<byte[]> result);

        Task ClickUserInformationPrimaryAction(string address);

        Task GetUserInformation(string address, Action<UserInformation> result);
    }
}

