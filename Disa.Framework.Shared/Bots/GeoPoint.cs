using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    /// <summary>
    /// This object represents a point on the map.
    /// </summary>
    public class GeoPoint
    {
        /// <summary>
        /// Is this an empty map point?
        /// </summary>
        public bool IsEmpty { get; set; }

        /// <summary>
        /// Longitude as defined by sender.
        /// </summary>
        public double Long { get; set; }

        /// <summary>
        /// Latitude as defined by sender.
        /// </summary>
        public double Lat { get; set; }
    }
}
