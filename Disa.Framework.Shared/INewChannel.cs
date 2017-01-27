using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Disa.Framework
{
    [DisaFramework]
    public interface INewChannel
    {
        //
        // Original interface - for these you can use the following test methodology:
        //
        // // Does the service implement the required interface?
        // var newChannelUi = service as INewChannel
        // if (newChannelUi != null)
        // {
        //     .
        //     .
        //     .
        // }
        //

        /// <summary>
        /// Set the result of the <see cref="Action"/> as the BubbleGroup requested
        /// by the collection of <see cref="Contact.ID"/>s.
        /// 
        /// This method should return null if the cardinality of the <see cref="Contact.ID"/>
        /// array is greater than 1 or if the 1 <see cref="Contact.ID"/> does not result in finding
        /// a <see cref="BubbleGroup"/>.
        /// </summary>
        /// <param name="contactIds">An array of <see cref="Contact.ID"/>s representing
        /// a <see cref="BubbleGroup"/>.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/>.</returns>
        Task FetchChannelBubbleGroup(Contact.ID[] contactIds, Action<BubbleGroup> result);

        /// <summary>
        /// Set the result of the <see cref="Action"/> as true with the new <see cref="BubbleGroup.Address"/>
        /// if the creation of the Channel was successful, false othwerwise.
        /// </summary>
        /// <param name="name">The name of the channel.</param>
        /// <param name="description">The description of the channel.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/>.</returns>
        Task FetchChannelBubbleGroupAddress(string name, string description, Action<bool, string> result);

        /// <summary>
        /// Set the result of the <see cref="Action"/> as true if the <see cref="Contact"/> was
        /// successfully invited to the channel, false otherwise.
        /// </summary>
        /// <param name="group">The <see cref="BubbleGroup"/> in context.</param>
        /// <param name="contacts">A <see cref="Tuple"/> containing <see cref="Contact"/> and <see cref="Contact.ID"/>
        /// representation of the contact to invite.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/>.</returns>
        Task InviteToChannel(BubbleGroup group, Tuple<Contact, Contact.ID>[] contacts, Action<bool> result);

        /// <summary>
        /// Set the result of the <see cref="Action"/> as the collection of Channel <see cref="Contact"/>s
        /// based on the query passed in.
        /// </summary>
        /// <param name="query">A query used to filter the list of <see cref="Contects"/>. May be
        /// an empty string, in which case all Channels will be returned.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/>.</returns>
        Task GetChannelContacts(string query, Action<List<Contact>> result);

        //
        // Begin interface extensions below. For these you must use the following test methodology
        // or something similar:
        // 
        // // Do we have the required method?
        // if(DisaFrameworkMethods.Missing(service, DisaFrameWorkMethods.INewChannelXxx)
        // {
        //     return;
        // }
        //
        // // Ok to proceed
        //

    }
}

