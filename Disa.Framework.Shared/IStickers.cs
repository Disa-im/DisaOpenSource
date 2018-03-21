using Disa.Framework.Stickers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Disa.Framework
{
    /// <summary>
    /// An optional interface for plugin developers to implement to expose their sticker capabilities.
    /// 
    /// IMPORTANT IMPLEMENTATION NOTES:
    /// 1. Service providers must provide Sticker with Id and StickerPackId valid for a file name of:
    /// <see cref="Sticker.StickerPackId"/>-<see cref="Sticker.Id"/>
    /// Example: 435-897 
    /// 2. The combination of <see cref="Sticker.StickerPackId"/> and <see cref="Sticker.Id"/> must be unique for the service provider.
    /// 3. A retrieval of a still version of the sticker must always be available. An animated version is optional.
    /// 4. <see cref="Sticker.HasAnimated"/> will signal that retrieval of the sticker will produce both a still and animated version.
    /// 5. The fields <see cref="Sticker.LocationStill"/> and <see cref="Sticker.LocationAnimated"/> are not to be filled in
    /// by the service provider, but will be filled in by the framework.
    /// </summary>
    [DisaFramework]
    public interface IStickers
    {
        //
        // Original interface - for these you can use the following test methodology:
        //
        // // Does the service implement the required interface?
        // var stickersUi = service as IStickers
        // if (stickersUi != null)
        // {
        //     .
        //     .
        //     .
        // }
        //

        /// <summary>
        /// Set the result of the <see cref="Action{List{Sticker}}"/> as the collection of <see cref="Sticker"/>s
        /// that represent the most recent trending stickers for this <see cref="Service"/>.
        /// </summary>
        /// <param name="page">A 0-based designation of the page to retieve. 
        /// 
        /// Can be used for paginated results when retrieving trending stickers.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action{List{Sticker}}"/>.
        /// </returns>
        Task GetTrendingStickers(int page, Action<List<Sticker>> result);

        /// <summary>
        /// Property for this stickers provider to specify if they offer searching of stickers.
        /// 
        /// True if this stickers provider offers searching of stickers, false if not.
        /// </summary>
        bool HasSearchStickers { get; }

        /// <summary>
        /// Set the result of the <see cref="Action{List{Sticker}}"/> as the collection of <see cref="Sticker"/>s
        /// that represent the result of the query passed in.
        /// </summary>
        /// <param name="query">The search text to query on.
        /// <param name="page">A 0-based designation of the page to retieve. 
        /// 
        /// Can be used for paginated results when retrieving search results.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action{List{Sticker}}"/>.
        /// </returns>
        Task SearchStickers(string query, int page, Action<List<Sticker>> result);

        /// <summary>
        /// Set the result of the <see cref="Action{ServiceStickerPacks}"/> as the set of <see cref="StickerPack"/>s
        /// for the current user.
        /// </summary>
        /// <param name="hash">Contains a <see cref="ServiceStickerPacks.Hash"/> from
        /// a previous <see cref="GetUserStickerPacks(string, Action{ServiceStickerPacks})"/> call.
        /// 
        /// Can be set to null or empty string to indicate no hash is available.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action{ServiceStickerPacks}"/>.
        /// </returns>
        Task GetUserStickerPacks(string hash, Action<ServiceStickerPacks> result);

        /// <summary>
        /// Set the result of the <see cref="Action{ServiceStickerPacks}"/> as the set of available <see cref="StickerPack"/>s
        /// not installed for the user by this <see cref="Service"/>.
        /// </summary>
        /// <param name="hash">Contains a <see cref="ServiceStickerPacks.Hash"/> from
        /// a previous <see cref="GetAvailableStickerPacks(string hash, Action{ServiceStickerPacks})"/> call.
        /// 
        /// Can be set to null or empty string to indicate no hash is available.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action{ServiceStickerPacks}"/>.
        /// </returns>
        Task GetAvailableStickerPacks(string hash, Action<ServiceStickerPacks> result);

        /// <summary>
        /// Set the result of the <see cref="Action{ServiceStickerPacks}"/> as the set of trending <see cref="StickerPack"/>s
        /// for the <see cref="Service"/>.
        /// </summary>
        /// <param name="hash">Contains a <see cref="ServiceStickerPacks.Hash"/> from
        /// a previous <see cref="GetTrendingStickerPacks(string hash, Action{ServiceStickerPacks})"/> call.
        /// 
        /// Can be set to null or empty string to indicate no hash is available.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action{ServiceStickerPacks}"/>.
        /// </returns>
        Task GetTrendingStickerPacks(string hash, Action<ServiceStickerPacks> result);

        /// <summary>
        /// Sets the result of the <see cref="Action{FullStickerPack}"/> as the metadata for a <see cref="FullStickerPack"/> .
        /// </summary>
        /// <param name="stickerPack">A <see cref="StickerPack"/> containing the necessary <see cref="Service"/> specific identity
        /// information to get the <see cref="FullStickerPack"/>.</param>
        /// <param name="result"><see cref="Action"/>on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action{FullStickerPack}"/>.
        Task GetFullStickerPack(StickerPack stickerPack, Action<FullStickerPack> result);

        /// <summary>
        /// Returns a <see cref="StickerLocationInfo"/> as the location info necessary
        /// to store away an on-device representation for the passed in <see cref="Sticker"/>.
        /// 
        /// If the <see cref="Sticker"/> can be accessed via a simple remote http url, then the <see cref="Service"/>
        /// can set the <see cref="StickerLocationInfo.LocationStill"/> to the url and set the <see cref="StickerLocationInfo.IsUrl"/>
        /// to true.
        /// 
        /// If the <see cref="Sticker"/> cannot be accessed via a simple remote http url, then the <see cref="Service"/> should
        /// download the <see cref="Sticker"/> to a temporary file location on device and set the <see cref="StickerLocationInfo.LocationStill"/>
        /// to the on device location. The caller of <see cref="DownloadSticker(Sticker, Action{int})"/> will move
        /// the sticker in the temporary file location to a permanent cached location.
        /// </summary>
        /// <param name="sticker">The <see cref="Sticker"/> to get <see cref="StickerLocationInfo"/> for.</param>
        /// <param name="progress"><see cref="Action"/>on which progress can be reported.</param>
        /// <returns>A new <see cref="Task"/> that returns the <see cref="StickerLocationInfo"/>.</returns>
        Task<StickerLocationInfo> DownloadSticker(Sticker sticker, Action<int> progress);

        /// <summary>
        /// Set the result of the <see cref="Action{bool}"/> as the success or failure for 
        /// recording that the <see cref="Service"/> has installed a <see cref="StickerPack"/>.
        /// </summary>
        /// <param name="stickerPack">The <see cref="StickerPack"/> that has been installed.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action{bool}"/>.</returns>
        Task StickerPackInstalled(StickerPack stickerPack, Action<bool> result);

        /// <summary>
        /// Set the result of the <see cref="Action{bool}"/> as the success or failure for 
        /// recording that the <see cref="Service"/> has archived the passed in <see cref="StickerPack"/>.
        /// </summary>
        /// <param name="stickerPack">The <see cref="StickerPack"/> that has been archived.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action{bool}"/>.</returns>
        Task StickerPackArchived(StickerPack stickerPack, Action<bool> result);

        /// <summary>
        /// Set the result of the <see cref="Action{bool}"/> as the success or failure for 
        /// recording that the <see cref="Service"/> has unarchived the passed in <see cref="StickerPack"/>.
        /// </summary>
        /// <param name="stickerPack">The <see cref="StickerPack"/> that has been unarchived.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action{bool}"/>.</returns>
        Task StickerPackUnarchived(StickerPack stickerPack, Action<bool> result);

        /// <summary>
        /// Set the result of the <see cref="Action{bool}"/> as the success or failure for
        /// recording that you have uninstalled a <see cref="StickerPack"/>.
        /// </summary>
        /// <param name="stickerPack">The <see cref="StickerPack"/> that has been uninstalled.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action{bool}"/>.</returns>
        Task StickerPackUninstalled(StickerPack stickerPack, Action<bool> result);

        /// <summary>
        /// Set the result of the <see cref="Action{Byte[]}"/> as the the attribution logo
        /// for this stickers service provider.
        /// 
        /// The <see cref="Byte[]"/> shall represent a png image with transparent background meant
        /// for a dark background.
        /// 
        /// If the <see cref="Service"/> does not have an attribution logo, set the <see cref="Action{Byte[]"/>
        /// result as null.
        /// </summary>
        /// <param name="result"><see cref="Action{Byte[]}"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action{Byte[]}"/></returns>
        Task GetStickersAttributionLogo(Action<byte[]> result);
    }
}
