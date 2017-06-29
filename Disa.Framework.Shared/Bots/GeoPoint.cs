using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    public abstract class GeoPointBase
    {
    }

    /// <summary>
    /// This object represents a point on the map.
    /// </summary>
    public class GeoPoint : GeoPointBase
    {
        /// <summary>
        /// Longitude as defined by sender.
        /// </summary>
        public double Long { get; set; }

        /// <summary>
        /// Latitude as defined by sender.
        /// </summary>
        public double Lat { get; set; }
    }

    /// <summary>
    /// This object represents an empty point on the map.
    /// </summary>
    public class GeoPointEmpty : GeoPointBase
    {
    }
}
