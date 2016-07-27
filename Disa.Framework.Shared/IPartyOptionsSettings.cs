using System;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public enum PartyOptionsSettingsPartyType
    {
        Public,
        Private,
        Unknown
    };

    public enum PartyOptionsSettingsAddNewMembersRestriction
    {
        Everyone,
        OnlyAdmins,
        Unknown
    };

    public interface IPartyOptionsSettings
    {
        Task CanSetPartyType(BubbleGroup group, Action<bool> result);
        Task GetPartyType(BubbleGroup group, Action<PartyOptionsSettingsPartyType> result);
        Task SetPartyType(BubbleGroup group, PartyOptionsSettingsPartyType type,Action<bool> result);
        Task CanSetPartyAddNewMembersRestriction(BubbleGroup group, Action<bool> result);
        Task GetPartyAddNewMembersRestriction(BubbleGroup group, Action<PartyOptionsSettingsAddNewMembersRestriction> result);
        Task SetPartyAddNewMembersRestriction(BubbleGroup group, PartyOptionsSettingsAddNewMembersRestriction restriction, Action<bool> result);
        Task CanSetPartyAllMembersAdministratorsRestriction(BubbleGroup group, Action<bool> result);
        Task GetPartyAllMembersAdmininistratorsRestriction(BubbleGroup group, Action<bool> result);
        Task SetPartyAllMembersAdmininistratorsRestriction(BubbleGroup group, bool restriction, Action<bool> result);
        Task CanConvertToExtendedParty(BubbleGroup group, Action<bool> result);
        Task ConvertToExtendedParty(BubbleGroup group, Action<bool> result);
    }
}

