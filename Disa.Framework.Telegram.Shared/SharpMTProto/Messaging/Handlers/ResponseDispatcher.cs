// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResponseDispatcher.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nito.AsyncEx;
using SharpMTProto.Schema;

namespace SharpMTProto.Messaging.Handlers
{
    /// <summary>
    ///     Response dispatcher.
    /// </summary>
    public interface IResponseDispatcher
    {
        /// <summary>
        ///     Fallback handler.
        ///     If it is set then all responses without handler are handled by this handler, otherwise message is ignored.
        /// </summary>
        IResponseHandler GenericHandler { get; set; }

        /// <summary>
        ///     Dispatch response message.
        /// </summary>
        /// <param name="responseMessage">Response message.</param>
        /// <returns>Task.</returns>
        Task DispatchAsync(IMessage responseMessage);

        /// <summary>
        ///     Add handler for a response of a type which is set in handler's <see cref="IResponseHandler.ResponseType" />
        ///     property.
        /// </summary>
        /// <param name="handler">Handler.</param>
        /// <param name="overwriteExisted">Overwrite existed.</param>
        void AddHandler(IResponseHandler handler, bool overwriteExisted = false);

        /// <summary>
        ///     Add handler for a response of specified type.
        ///     Handler's <see cref="IResponseHandler.ResponseType" /> property do not affect anything.
        /// </summary>
        /// <param name="handler">Handler.</param>
        /// <param name="overwriteExisted">Overwrite existed.</param>
        void AddHandler<TResponse>(IResponseHandler handler, bool overwriteExisted = false);

        /// <summary>
        ///     Add handler for a response.
        ///     Handler's <see cref="IResponseHandler.ResponseType" /> property do not affect anything.
        /// </summary>
        /// <param name="handler">Handler.</param>
        /// <param name="responseType">Response type.</param>
        /// <param name="overwriteExisted">Overwrite existed.</param>
        void AddHandler(IResponseHandler handler, Type responseType, bool overwriteExisted = false);
    }
    
    /// <summary>
    ///     Response dispatcher routes messages to proper response handler.
    /// </summary>
    public class ResponseDispatcher : IResponseDispatcher
    {
        private readonly Dictionary<Type, IResponseHandler> _handlers = new Dictionary<Type, IResponseHandler>();

        public IResponseHandler GenericHandler { get; set; }

        public async Task DispatchAsync(IMessage responseMessage)
        {
            Type responseType = responseMessage.Body.GetType();

            IResponseHandler handler = _handlers.Where(pair => pair.Key.IsAssignableFrom(responseType)).Select(pair => pair.Value).FirstOrDefault();
            if (handler == null)
            {
                if (GenericHandler != null)
                {
                    handler = GenericHandler;
                }
                else
                {
                    Console.WriteLine(
                        string.Format("No handler found for response of type '{0}' and there is no fallback handler. Message was ignored.", responseType.Name));
                    return;
                }
            }

            try
            {
                await handler.HandleAsync(responseMessage);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while handling a message: " + e);
            }
        }

        public void AddHandler(IResponseHandler handler, bool overwriteExisted = false)
        {
            AddHandler(handler, handler.ResponseType, overwriteExisted);
        }

        public void AddHandler<TResponse>(IResponseHandler handler, bool overwriteExisted = false)
        {
            Type responseType = typeof (TResponse);
            AddHandler(handler, responseType, overwriteExisted);
        }

        public void AddHandler(IResponseHandler handler, Type responseType, bool overwriteExisted = false)
        {
            if (_handlers.ContainsKey(responseType))
            {
                if (!overwriteExisted)
                {
                    Console.WriteLine(string.Format("Prevented addition of another handler '{0}' for response of type '{1}'.", handler.GetType(), handler.ResponseType.Name));
                    return;
                }
                _handlers.Remove(responseType);
            }

            _handlers.Add(responseType, handler);
        }
    }
}
