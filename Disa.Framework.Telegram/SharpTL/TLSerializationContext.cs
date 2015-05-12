// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TLSerializationContext.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace SharpTL
{
    /// <summary>
    ///     TL serialization context.
    /// </summary>
    public class TLSerializationContext
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TLSerializationContext" /> class.
        /// </summary>
        /// <param name="rig">TL rig.</param>
        /// <param name="streamer">TL streamer.</param>
        public TLSerializationContext(TLRig rig, TLStreamer streamer)
        {
            Rig = rig;
            Streamer = streamer;
        }

        /// <summary>
        ///     TL rig.
        /// </summary>
        public TLRig Rig { get; private set; }

        /// <summary>
        ///     TL streamer.
        /// </summary>
        public TLStreamer Streamer { get; private set; }
    }
}
