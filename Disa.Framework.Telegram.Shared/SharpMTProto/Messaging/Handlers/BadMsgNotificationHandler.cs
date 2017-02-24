// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BadMsgNotificationHandler.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using SharpMTProto.Schema;
using SharpMTProto.Services;
using SharpMTProto.Utils;

namespace SharpMTProto.Messaging.Handlers
{
    public class BadMsgNotificationHandler : ResponseHandler<IBadMsgNotification>
    {
        private readonly IMTProtoClientConnection _connection;
        private readonly IRequestsManager _requestsManager;

        public BadMsgNotificationHandler(IMTProtoClientConnection connection, IRequestsManager requestsManager)
        {
            _connection = connection;
            _requestsManager = requestsManager;
        }

        protected override async Task HandleInternalAsync(IMessage responseMessage)
        {
            #region Notice of Ignored Error Message
            /* In certain cases, a server may notify a client that its incoming message was ignored for whatever reason.
             * Note that such a notification cannot be generated unless a message is correctly decoded by the server.
             * 
             * bad_msg_notification#a7eff811 bad_msg_id:long bad_msg_seqno:int error_code:int = BadMsgNotification;
             * bad_server_salt#edab447b bad_msg_id:long bad_msg_seqno:int error_code:int new_server_salt:long = BadMsgNotification;
             * 
             * Here, error_code can also take on the following values:
             * 
             * 16: msg_id too low (most likely, client time is wrong; it would be worthwhile to synchronize it using msg_id notifications
             *     and re-send the original message with the “correct” msg_id or wrap it in a container with a new msg_id if the original
             *     message had waited too long on the client to be transmitted)
             * 17: msg_id too high (similar to the previous case, the client time has to be synchronized, and the message re-sent with the correct msg_id)
             * 18: incorrect two lower order msg_id bits (the server expects client message msg_id to be divisible by 4)
             * 19: container msg_id is the same as msg_id of a previously received message (this must never happen)
             * 20: message too old, and it cannot be verified whether the server has received a message with this msg_id or not
             * 32: msg_seqno too low (the server has already received a message with a lower msg_id but with either a higher or an equal and odd seqno)
             * 33: msg_seqno too high (similarly, there is a message with a higher msg_id but with either a lower or an equal and odd seqno)
             * 34: an even msg_seqno expected (irrelevant message), but odd received
             * 35: odd msg_seqno expected (relevant message), but even received
             * 48: incorrect server salt (in this case, the bad_server_salt response is received with the correct salt, and the message is to be re-sent with it)
             * 64: invalid container.
             * The intention is that error_code values are grouped (error_code >> 4): for example, the codes 0x40 - 0x4f correspond to errors in container decomposition.
             * 
             * Notifications of an ignored message do not require acknowledgment (i.e., are irrelevant).
             * 
             * Important: if server_salt has changed on the server or if client time is incorrect, any query will result in a notification in the above format.
             * The client must check that it has, in fact, recently sent a message with the specified msg_id, and if that is the case,
             * update its time correction value (the difference between the client’s and the server’s clocks) and the server salt based on msg_id
             * and the server_salt notification, so as to use these to (re)send future messages.
             * In the meantime, the original message (the one that caused the error message to be returned) must also be re-sent with a better msg_id and/or server_salt.
             * 
             * In addition, the client can update the server_salt value used to send messages to the server,
             * based on the values of RPC responses or containers carrying an RPC response,
             * provided that this RPC response is actually a match for the query sent recently.
             * (If there is doubt, it is best not to update since there is risk of a replay attack).
             * 
             * https://core.telegram.org/mtproto/service_messages_about_messages#notice-of-ignored-error-message
             */
            #endregion

            var response = (IBadMsgNotification) responseMessage.Body;

            var errorCode = (ErrorCode) response.ErrorCode;
            Console.WriteLine(string.Format("Bad message notification received with error code: {0} ({1}).", response.ErrorCode, errorCode));

            Console.WriteLine("Searching for bad message in the request manager...");

            IRequest request = _requestsManager.Get(response.BadMsgId);
            if (request == null)
            {
                Console.WriteLine(string.Format("Bad message (MsgId: 0x{0:X}) was NOT found. Ignored.", response.BadMsgId));
                return;
            }
            if (request.Message.Seqno != response.BadMsgSeqno)
            {
                Console.WriteLine(
                    string.Format(
                        "Bad message (MsgId: 0x{0:X}) was found, but message sequence number is not the same ({1}) as server expected ({2}). Ignored.",
                        response.BadMsgId,
                        request.Message.Seqno,
                        response.BadMsgSeqno));
                return;
            }

            var badServerSalt = response as BadServerSalt;
            if (badServerSalt != null)
            {
                if (errorCode != ErrorCode.IncorrectServerSalt)
                {
                    Console.WriteLine(string.Format("Error code must be '{0}' for a BadServerSalt notification, but found '{1}'.", ErrorCode.IncorrectServerSalt, errorCode));
                }

                Console.WriteLine(
                    string.Format(
                        "Bad server salt was in outgoing message (MsgId = 0x{0:X}, Seqno = {1}). Error code = {2}.",
                        badServerSalt.BadMsgId,
                        badServerSalt.BadMsgSeqno,
                        errorCode));

                Console.WriteLine("Setting new salt.");

                _connection.UpdateSalt(badServerSalt.NewServerSalt);

                Console.WriteLine("Resending bad message with the new salt.");

                await request.SendAsync();
                return;
            }

            var badMsgNotification = response as BadMsgNotification;
            if (badMsgNotification != null)
            {
                // TODO: implement.
                switch (errorCode)
                {
                    case ErrorCode.MsgIdIsTooSmall:
                    case ErrorCode.MsgIdIsTooBig:
					case ErrorCode.MsgIdDuplicate:
					case ErrorCode.MsgTooOld:	
					case ErrorCode.MsgIdBadTwoLowBytes:
                        Disa.Framework.Utils.DebugPrint("BAD NOTIFICATOIN!");
						ulong time = (ulong)(responseMessage.MsgId / 4294967296.0 * 1000);
						ulong currentTime = UnixTimeUtils.GetCurrentUnixTimestampMilliseconds();
						_connection.MessageIdGenerator.TimeDifference = (ulong)((time - currentTime) / 1000f);
                        Disa.Framework.Utils.DebugPrint("Time drift set to: " + _connection.MessageIdGenerator.TimeDifference);
                        var message = new Message(_connection.MessageIdGenerator.GetNextMessageId(), request.Message.Seqno, request.Message.Body);
						request.UpdateMessage(message);
						await request.SendAsync();
						break;
                    case ErrorCode.MsgSeqnoIsTooLow:
                    case ErrorCode.MsgSeqnoIsTooBig:
                    case ErrorCode.MsgSeqnoNotEven:
                    case ErrorCode.MsgSeqnoNotOdd:
                    case ErrorCode.IncorrectServerSalt:
                    case ErrorCode.InvalidContainer:
						throw new NotImplementedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
