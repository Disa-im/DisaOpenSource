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

    [DisaFramework]
    public interface IPartyOptionsSettings
    {
        //
        // Original interface - for these you can use the following test methodology:
        //
        // // Does the service implement the required interface?
        // var partyOptionsSettings = service as IPartyOptionsSettings
        // if (partyOptionsSettings != null)
        // {
        //     .
        //     .
        //     .
        // }
        //

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


        //
        // Begin interface extensions below. For these you must use the following test methodology
        // or something similar:
        // 
        // // Do we have the required method?
        // if(DisaFrameworkMethods.Missing(service, DisaFrameWorkMethods.IPartyOptionsSettingsXxx)
        // {
        //     return;
        // }
        //
        // // Ok to proceed
        //

        /// <summary>
        /// Set the result of the <see cref="Action"/> as true if the current user can set the sign messages value
        /// for this <see cref="BubbleGroup"/>, flase otherwise.
        /// </summary>
        /// <param name="group">The <see cref="BubbleGroup"/> in context.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/></returns>
        Task CanSignMessages(BubbleGroup group, Action<bool> result);

        /// <summary>
        /// Set the result of the <see cref="Action"/> as the current value for sign messages for this
        /// <see cref="BubbleGroup"/>.
        /// </summary>
        /// <param name="group">The <see cref="BubbleGroup"/> in context.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/></returns>
        Task GetSignMessages(BubbleGroup group, Action<bool> result);

        /// <summary>
        /// Set the result of the <see cref="Action"/> as true if the party's sign messages value
        /// was successfully set, false otherwise.
        /// </summary>
        /// <param name="group">The <see cref="BubbleGroup"/> in context.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/></returns>
        Task SignMessages(BubbleGroup group, bool sign, Action<bool> result);
    }

}

