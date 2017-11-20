using System;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    /// <summary>
    /// The <see cref="VisualBubble.IdService"/> and <see cref="VisualBubble.IdService2"/> provide <see cref="Service"/> specific
    /// identity information for a <see cref="VisualBubble"/>. This interface allows plugin developers to
    /// 1. Hook into life-cycle events for maintenance of these fields.
    /// 2. Fine-tune the characteristics for identity as represented by these field.
    /// 
    /// Example: Telegram
    /// 1. Implements <see cref="AddVisualBubbleIdServices(VisualBubble)"/> during a message send to store the next message id in <see cref="VisualBubble.IdService2"/>.
    /// 2. Returns true from <see cref="DisctinctIncomingVisualBubbleIdServices"/> to specify that <see cref="VisualBubble.IdService"/> 
    ///    and <see cref="VisualBubble.IdService2"/> must be distinct on device.
    /// 3. Returns false from <see cref="CheckType"/> to specify that only <see cref="VisualBubble.IdService"/> and <see cref="VisualBubble.IdService2"/> are used
    ///    to determine if a <see cref="VisualBubble"/> is distinct.
    /// 4. Implements <see cref="VisualBubbleIdComparer(VisualBubble, VisualBubble)"/> to override the determination of a <see cref="VisualBubble"/> being distinct
    ///    for the case where we have an <see cref="ImageBubble"/> followed by a <see cref="TextBubble"/> with the same <see cref="VisualBubble.IdService"/> or 
    /// <see cref="VisualBubble.IdService2"/> which represents Telegram's implementation for an image with a caption.
    /// </summary>
    [DisaFramework]
    public interface IVisualBubbleServiceId
    {
        //
        // Original interface - for these you can use the following test methodology:
        //
        // // Does the service implement the required interface?
        // var visualBubbleServiceIdUi = service as IVisualBubbleServiceId
        // if (visualBubbleServiceIdUi != null)
        // {
        //     .
        //     .
        //     .
        // }
        //

        /// <summary>
        /// Allow a <see cref="Service"/> to update a <see cref="VisualBubble"/>'s state for it's
        /// own needs while a message is being sent in the Disa Framework and before 
        /// the <see cref="Service.SendBubble(Bubble)"/> is called.
        /// 
        /// Notes: This is currently called in the <see cref="BubbleManager"/> sending of a message flow.
        /// 
        /// Example: Telegram uses this to update and record the next message id to use.
        /// </summary>
        /// <param name="bubble">The <see cref="VisualBubble"/> for updating.</param>
        void AddVisualBubbleIdServices(VisualBubble bubble);

        /// <summary>
        /// Allows for a <see cref="Service"/> to specify that the <see cref="VisualBubble.IdService"/> and
        /// <see cref="VisualBubble.IdService2"/> should be distinct.
        /// 
        /// See the other methods in this interface for additional details how a <see cref="Service"/> can
        /// fine-tune the defintiion for what it means to be distinct.
        /// </summary>
        /// <returns>True if the <see cref="Service"/> requires that the <see cref="VisualBubble.IdService"/>
        /// and <see cref="VisualBubble.IdService2"/> should be distinct. False if the <see cref="Service"/>
        /// does not have such a requirement.</returns>
        bool DisctinctIncomingVisualBubbleIdServices();


        //
        // Begin interface extensions below. For these you must use the following test methodology
        // or something similar:
        // 
        // // Do we have the required method?
        // if(DisaFrameworkMethods.Missing(service, DisaFrameworkMethods.IVisualBubbleServiceIdXxx)
        // {
        //     return;
        // }
        //
        // // Ok to proceed
        //


        /// <summary>
        /// Should we use the result of <see cref="Object.GetType"/> in determining if the <see cref="VisualBubble.IdService"/>
        /// or <see cref="VisualBubble.IdService2"/> is a duplicate.
        /// </summary>
        /// <returns>False if you should base the existence of a duplicate <see cref="VisualBubble"/> only on if
        /// a <see cref="VisualBubble.IdService"/> or a <see cref="VisualBubble.IdService2"/> already exists on the device
        /// for a <see cref="Service"/>.
        /// 
        /// True if you should add in a comparison of the return from <see cref="object.GetType"/> to the determination.
        /// 
        /// That is, if you specify True, then you can have two distinct <see cref="VisualBubble"/>s with the same
        /// <see cref="VisualBubble.IdService"/> or <see cref="VisualBubble.IdService2"/> as long as their <see cref="Type"/>s
        /// differ.</returns>
        bool CheckType();

        /// <summary>
        /// Allows a <see cref="Service"/> to specify additional comparison logic for determining distinction.
        /// 
        /// IMPORTANT: This is called after the logic for <see cref="DisctinctIncomingVisualBubbleIdServices"/> and <see cref="CheckType"/>
        ///            has run AND determined that we have a duplicate based on that criteria.
        ///            
        /// For example, in Telegram, we allow an ImageBubble immediately followed by a TextBubble to have the 
        /// same VisualBubble.IdService - as this represents an image with a caption in Telegram.
        /// </summary>
        /// <param name="left">An existing <see cref="VisualBubble"/> to check for distinction.</param>
        /// <param name="right">A new <see cref="VisualBubble"/> to check for distinction against the existing <see cref="VisualBubble"/>.</param>
        /// <returns>True if the <see cref="Service"/> determines that the existing <see cref="VisualBubble"/> is a duplicate of the
        /// new <see cref="VisualBubble"/>, False if not.</returns>
        bool VisualBubbleIdComparer(VisualBubble left, VisualBubble right);
    }
}