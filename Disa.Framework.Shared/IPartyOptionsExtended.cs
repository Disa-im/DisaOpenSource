using System;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public enum SetPartyShareLinkResult
    {
        Success,
        LinkUnavailable,
        LinkInvalid,
        Failure
    }

    public interface IPartyOptionsExtended
    {
        //links

        /// <summary>
        /// Set the result of the action as true if the party has a share link, false otherwise
        /// </summary>
        /// <returns>A new task that sets the result action.</returns>
        /// <param name="group">The BubbleGroup in context</param>
        /// <param name="result">Action on which the result should be set</param>
        Task HasPartyShareLink(BubbleGroup group, Action<bool> result);

        /// <summary>
        /// Set the result of this function as a Tuple<string, string>
        /// The first string in the Tuple is the immutable part of the link (In cases where the domain is fixed but the user can chose the url)
        /// The second string in the Tuple the mutable part of the url
        /// In cases where the url doesnt have two parts, just set the fist string in the tuple to null, it will wook as if 
        /// the there was just a single url.
        /// Also as captain obvious says, this function will only be called if the HasPartyShareLink is set to true
        /// </summary>
        /// <returns>The party share link.</returns>
        /// <param name="group">The BubbleGroup in context</param>
        /// <param name="result">Action on which the result should be set</param>
        Task GetPartyShareLink(BubbleGroup group, Action<Tuple<string, string>> result);

        /// <summary>
        /// Set the result of the action as true if the current user can set the party share link
        /// </summary>
        /// <returns>A new task that sets the result action</returns>
        /// <param name="group">The BubbleGroup in context</param>
        /// <param name="result">Action on which the result should be set</param>
        Task CanSetPartyShareLink(BubbleGroup group, Action<bool> result);

        /// <summary>
        /// Set the result of the action to the appropriate enum value depending on if the link was too short or properly set
        /// </summary>
        /// <returns>A new task that sets the result action</returns>
        /// <param name="group">The BubbleGroup in context</param>
        /// <param name="shareLink">The tuple contains two strings, the first of which would be set to the immutable string or null, 
        /// whatever you did set in the GetPartyShareLink function, and the second contains the new url that the user just set </param>
        /// <param name="result">Action on which the result as an enum should be set</param>
        Task SetPartyShareLink(BubbleGroup group, Tuple<string, string> shareLink, Action<SetPartyShareLinkResult> result);

        /// <summary>
        /// Set the result of the action with party share link max characters
        /// </summary>
        /// <returns>A new task that sets the result action</returns>
        /// <param name="group">The BubbleGroup in context</param>
        /// <param name="result">Action on which the result should be set</param>
        Task GetPartyShareLinkMaxCharacters(BubbleGroup group, Action<int> result);

        /// <summary>
        /// Set the result of the action with party share link max characters
        /// </summary>
        /// <returns>A new task that sets the result action</returns>
        /// <param name="group">The BubbleGroup in context</param>
        /// <param name="result">Action on which the result should be set</param>
        Task GetPartyShareLinkMinCharacters(BubbleGroup group, Action<int> result);


        //description

        /// <summary>
        /// Set the result of the action as true if the party has a description, false otherwise
        /// </summary>
        /// <returns>A new task that sets the result action.</returns>
        /// <param name="group">The BubbleGroup in context</param>
        /// <param name="result">Action on which the result should be set</param>
        Task HasPartyDescription(BubbleGroup group, Action<bool> result);

        /// <summary>
        /// Set the result of the action as the party description
        /// </summary>
        /// <returns>A new task that sets the result action.</returns>
        /// <param name="group">The BubbleGroup in context</param>
        /// <param name="result">Action on which the result should be set</param>
        Task GetPartyDescription(BubbleGroup group, Action<string> result);

        /// <summary>
        /// Set the result of the action as true if the  the current user can set the party description, flase otherwise
        /// </summary>
        /// <returns>A new task that sets the result action.</returns>
        /// <param name="group">The BubbleGroup in context</param>
        /// <param name="result">Action on which the result should be set</param>
        Task CanSetPartyDescription(BubbleGroup group, Action<bool> result);

        /// <summary>
        ///Set the result of the action as true if the party description was successfully set, flase otherwise
        /// </summary>
        /// <returns>A new task that sets the result action</returns>
        /// <param name="group">The BubbleGroup in context</param>
        /// <param name="description">The party share description</param>
        /// <param name="result">Action on which the result as an boolean should be set</param>
        Task SetPartyDescription(BubbleGroup group, string description, Action<bool> result);

        /// <summary>
        /// Set the result of the action with the minimum number of characters expected in the party description
        /// </summary>
        /// <returns>A new task that sets the result action</returns>
        /// <param name="group">The BubbleGroup in context</param>
        /// <param name="result">Action on which the result should be set</param>
        Task GetPartyDescriptionMaxCharacters(BubbleGroup group, Action<int> result);

        /// <summary>
        /// Set the result of the action with the maximum number of characters expected in the party description
        /// </summary>
        /// <returns>A new task that sets the result action</returns>
        /// <param name="group">The BubbleGroup in context</param>
        /// <param name="result">Action on which the result should be set</param>
        Task GetPartyDescriptionMinCharacters(BubbleGroup group, Action<int> result);

        //blockedparticipants

        /// <summary>
        /// Set the result of the action with true if the curent user can view blocked participants, false otherwise
        /// </summary>
        /// <returns>A new task that sets the result action</returns>
        /// <param name="group">The BubbleGroup in context</param>
        /// <param name="result">Action on which the result should be set</param>
        Task CanViewPartyBlockedParticipants(BubbleGroup group, Action<bool> result);
    }
}

