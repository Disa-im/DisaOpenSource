using Disa.Framework.Gifs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Disa.Framework
{
    /// <summary>
    /// An optional interface for plugin developers to implement to expose their gif capabilities.
    /// </summary>
    [DisaFramework]
    public interface IGifs
    {
        //
        // Original interface - for these you can use the following test methodology:
        //
        // // Does the service implement the required interface?
        // var gifUi = service as IGifs
        // if (gifUi != null)
        // {
        //     .
        //     .
        //     .
        // }
        //

        /// <summary>
        /// Set the result of the <see cref="Action{List{Gif}}"/> as the collection of <see cref="Gif"/>s
        /// that represent the most recent trending gifs for this <see cref="Service"/>.
        /// </summary>
        /// <param name="page">A 0-based designation of the page to retieve. 
        /// 
        /// Can be used for paginated results when retrieving trending gifs.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action{List{Gif}}"/>.
        /// </returns>
        Task GetTrendingGifs(int page, Action<List<Gif>> result);

        /// <summary>
        /// Set the result of the <see cref="Action{List{Gif}}"/> as the collection of <see cref="Gif"/>s
        /// that represent the result of the query passed in.
        /// </summary>
        /// <param name="query">The search text to query on.
        /// <param name="page">A 0-based designation of the page to retieve. 
        /// 
        /// Can be used for paginated results when retrieving search results.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action{List{Gif}}"/>.
        /// </returns>
        Task SearchGifs(string query, int page, Action<List<Gif>> result);

        /// <summary>
        /// Returns a <see cref="GifLocationInfo"/> as the location info necessary
        /// to store away an on-device representation for the passed in <see cref="Gif"/>.
        /// 
        /// If the <see cref="Gif"/> can be accessed via a simple remote http url, then the <see cref="Service"/>
        /// can set the <see cref="GifLocationInfo.LocationStill"/> to the url and set the <see cref="GifLocationInfo.IsUrl"/>
        /// to true.
        /// 
        /// If the <see cref="Gif"/> cannot be accessed via a simple remote http url, then the <see cref="Service"/> should
        /// download the <see cref="Gif"/> to a temporary file location on device and set the <see cref="GifLocationInfo.LocationStill"/>
        /// to the on device location. The caller of <see cref="DownloadGif(Gif, Action{int})"/> will move
        /// the gif in the temporary file location to a permanent cached location.
        /// </summary>
        /// <param name="gif">The <see cref="Gif"/> to get <see cref="GifLocationInfo"/> for.</param>
        /// <param name="progress"><see cref="Action"/>on which progress can be reported.</param>
        /// <returns>A new <see cref="Task"/> that returns the <see cref="GifLocationInfo"/>.</returns>
        Task<GifLocationInfo> DownloadGif(Gif gif, Action<int> progress);
    }
}
