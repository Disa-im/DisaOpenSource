

namespace Disa.Framework.Gifs
{
    /// <summary>
    /// A value class representing metadata for the location of a <see cref="Gif"/>.
    /// </summary>
    public class GifLocationInfo
    {
        /// <summary>
        /// A <see cref="string"/> representing the location for the still image for this gif.
        /// 
        /// If <see cref="IsUrl"/> is false, then this represents an on-device location. If <see cref="IsUrl"/>
        /// is true, then this represents a remote http location.
        /// </summary>
        public string LocationStill { get; set; }

        /// <summary>
        /// An optional <see cref="string"/> representing the location for the animated image for this gif.
        /// 
        /// If <see cref="IsUrl"/> is false, then this represents an on-device location. If <see cref="IsUrl"/>
        /// is true, then this represents a remote http location.
        /// </summary>
        public string LocationAnimated { get; set; }

        /// <summary>
        /// True if <see cref="LocationStill"/> and <see cref="LocationAnimated"/> represent remote http locations. 
        /// False if <see cref="LocationStill"/> and <see cref="LocationAnimated"/> represent an on-device locations.
        /// </summary>
        public bool IsUrl { get; set; }
    }
}
