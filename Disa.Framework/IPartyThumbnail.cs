using System;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public interface IPartyThumbnail
    {
        Task GetParticipantThumbnail(string address, Action<DisaThumbnail> result);
    }
}

