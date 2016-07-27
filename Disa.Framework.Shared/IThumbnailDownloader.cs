using System;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    public interface IThumbnailDownloader
    {
        /// <summary>
        /// Fetch the thumbnail bytes.
        /// </summary>
        /// <returns>A tuple the bool indicating if the thumbnail should not be retried, and byte array containing the thumbnail bytes</returns>
        /// <param name="bubble">Bubble.</param>
        Tuple<bool,byte[]> FetchThumbnailBytes(VisualBubble bubble);

        /// <summary>
        /// Fetchs the quoted thumbnail bytes.
        /// </summary>
        /// <returns>A tuple the bool indicating if the thumbnail should not be retried, and byte array containing the thumbnail bytes</returns>
        /// <param name="bubble">Bubble.</param>
        Tuple<bool,byte[]> FetchQuotedThumbnailBytes(VisualBubble bubble);
    }
}

