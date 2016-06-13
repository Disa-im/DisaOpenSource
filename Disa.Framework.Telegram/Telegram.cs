using System;
using Disa.Framework.Bubbles;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharpTelegram;
using SharpMTProto;
using SharpMTProto.Transport;
using SharpMTProto.Authentication;
using SharpTelegram.Schema;
using System.Linq;
using SharpMTProto.Messaging.Handlers;
using SharpMTProto.Schema;
using System.Globalization;
using System.Timers;
using System.IO;
using ProtoBuf;
using Message = SharpTelegram.Schema.Message;

//TODO:
//1) After authorization, there's an expiry time. Ensure that the login expires by then (also, in DC manager)

namespace Disa.Framework.Telegram
{
    [ServiceInfo("Telegram", true, false, false, false, true, typeof(TelegramSettings), 
        ServiceInfo.ProcedureType.ConnectAuthenticate, typeof(TextBubble), typeof(ReadBubble), 
        typeof(TypingBubble), typeof(PresenceBubble))]
    public partial class Telegram : Service, IVisualBubbleServiceId, ITerminal
    {
        private Dictionary<string, DisaThumbnail> _cachedThumbnails = new Dictionary<string, DisaThumbnail>();

        private static TcpClientTransportConfig DefaultTransportConfig = 
            new TcpClientTransportConfig("149.154.167.50", 443);

        private readonly object _baseMessageIdCounterLock = new object();
        private string _baseMessageId = "0000000000";
        private int _baseMessageIdCounter;

        private List<User> contactsCache = new List<User>();

        public bool LoadConversations;

        private object _quickUserLock = new object();

        //msgid, address, date
        private Dictionary<uint,Tuple<uint,uint,bool>> messagesUnreadCache = new Dictionary<uint, Tuple<uint, uint,bool>>();

        public string CurrentMessageId
        {
            get
            {
                return _baseMessageId + Convert.ToString(_baseMessageIdCounter);
            }
        }

        public string NextMessageId
        {
            get
            {
                lock (_baseMessageIdCounterLock)
                {
                    _baseMessageIdCounter++;
                    return CurrentMessageId;
                }
            }
        }

        private bool _hasPresence;

        private bool _longPollerAborted;

        private Random _random = new Random(System.Guid.NewGuid().GetHashCode());

        private TelegramSettings _settings;
        private TelegramMutableSettings _mutableSettings;

        private TelegramClient _longPollClient;

        private readonly object _mutableSettingsLock = new object();

        private CachedDialogs _dialogs = new CachedDialogs();
        private bool _dialogsInitiallyRetrieved = false;
        private Config _config;

        private Dictionary<string, Timer> _typingTimers = new Dictionary<string, Timer>();

        private WakeLockBalancer.GracefulWakeLock _longPollHeartbeart;

        private void CancelTypingTimer(string id)
        {
            if (_typingTimers.ContainsKey(id))
            {
                var timer = _typingTimers[id];
                timer.Stop();
                timer.Dispose();
            }
        }

        private void SaveState(uint date, uint pts, uint qts, uint seq)
        {
            lock (_mutableSettingsLock)
            {
                DebugPrint("Saving new state");
                if (date != 0)
                {
                    _mutableSettings.Date = date;
                }
                if (pts != 0)
                {
                    _mutableSettings.Pts = pts;
                }
                if (qts != 0)
                {
                    _mutableSettings.Qts = qts;
                }
                if (seq != 0)
                {
                    _mutableSettings.Seq = seq;
                }
                MutableSettingsManager.Save(_mutableSettings);
            }
        }

        private object NormalizeUpdateIfNeeded(object obj)
        {
            // flatten UpdateNewMessage to Message
            var newMessage = obj as UpdateNewMessage;
            if (newMessage != null)
            {
                return newMessage.Message;
            }

            return obj;
        }
            
        private List<object> AdjustUpdates(List<object> updates)
        {
            if (updates == null)
            {
                return null;
            }
            var precedents = new List<object>();
            var successors = updates.ToList();
            foreach (var update in successors.ToList())
            {
                var user = update as IUser;
                var chat = update as IChat;
                if (user != null || chat != null)
                {
                    precedents.Add(update);
                    successors.Remove(update);
                }
            }
            return precedents.Concat(successors).ToList();
        }

        private void ProcessIncomingPayload(List<object> payloads, bool useCurrentTime, TelegramClient optionalClient = null)
        {
            uint maxMessageId = 0;

            foreach (var payload in AdjustUpdates(payloads))
            {
                var update = NormalizeUpdateIfNeeded(payload);

                var shortMessage = update as UpdateShortMessage;
                var shortChatMessage = update as UpdateShortChatMessage;
                var typing = update as UpdateUserTyping;
                var typingChat = update as UpdateChatUserTyping;
                var userStatus = update as UpdateUserStatus;
                var messageService = update as MessageService;
                var updateChatParticipants = update as UpdateChatParticipants;
                var updateContactRegistered = update as UpdateContactRegistered;
                var updateContactLink = update as UpdateContactLink;
                var updateUserPhoto = update as UpdateUserPhoto;
                var updateReadHistoryInbox = update as UpdateReadHistoryInbox;
                var updateReadHistoryOutbox = update as UpdateReadHistoryOutbox;
                var message = update as SharpTelegram.Schema.Message;
                var user = update as IUser;
                var chat = update as IChat;

                DebugPrint(">>>>>> The type of object in process incoming payload is " + ObjectDumper.Dump(update));

                if (shortMessage != null)
                {
                    if (!string.IsNullOrWhiteSpace(shortMessage.Message))
                    {
						var fromId = shortMessage.UserId.ToString(CultureInfo.InvariantCulture);
                        EventBubble(new TypingBubble(Time.GetNowUnixTimestamp(),
                            Bubble.BubbleDirection.Incoming,
                            fromId, false, this, false, false));
                        TextBubble textBubble = new TextBubble(
                                            useCurrentTime ? Time.GetNowUnixTimestamp() : (long)shortMessage.Date, 
                                            shortMessage.Out != null ? Bubble.BubbleDirection.Outgoing : Bubble.BubbleDirection.Incoming, 
                                            fromId, null, false, this, shortMessage.Message,
                                            shortMessage.Id.ToString(CultureInfo.InvariantCulture));
                        
                        if (shortMessage.Out != null)
                        {
                            textBubble.Status = Bubble.BubbleStatus.Sent;
                        }
                        EventBubble(textBubble);
                    }
                    if (shortMessage.Id > maxMessageId)
                    {
                        maxMessageId = shortMessage.Id;
                    }
                }
                else if (updateUserPhoto != null)
                {
                    var iUpdatedUser = _dialogs.GetUser(updateUserPhoto.UserId);
                    var updatedUser = iUpdatedUser as User;
                    if (updatedUser != null)
                    {
                        updatedUser.Photo = updateUserPhoto.Photo;
                    }
                    _dialogs.AddUser(updatedUser);
                }
                else if (updateReadHistoryOutbox != null)
                {
                    var iPeer = updateReadHistoryOutbox.Peer;
                    var peerChat = iPeer as PeerChat;
                    var peerUser = iPeer as PeerUser;


                    if (peerUser != null)
                    {
                        BubbleGroup bubbleGroup = BubbleGroupManager.FindWithAddress(this,
                            peerUser.UserId.ToString(CultureInfo.InvariantCulture));
                        string idString = bubbleGroup.LastBubbleSafe().IdService;
                        if (idString == updateReadHistoryOutbox.MaxId.ToString(CultureInfo.InvariantCulture))
                        {
                            EventBubble(
                                new ReadBubble(useCurrentTime ? Time.GetNowUnixTimestamp() : (long) shortMessage.Date,
                                    Bubble.BubbleDirection.Incoming, this,
                                    peerUser.UserId.ToString(CultureInfo.InvariantCulture), null,
                                    Time.GetNowUnixTimestamp(), false, false));
                        }

                    }
                    else if (peerChat != null)
                    {//TODO:Uncomment when group read bubbles implemented
//                        BubbleGroup bubbleGroup = BubbleGroupManager.FindWithAddress(this,
//                           peerChat.ChatId.ToString(CultureInfo.InvariantCulture));
//                        string idString = bubbleGroup.LastBubbleSafe().IdService;
//                        DebugPrint("Idservice for Chat " + idString);
//                        if (idString == updateReadHistoryOutbox.MaxId.ToString(CultureInfo.InvariantCulture))
//                        {
//                            DebugPrint("######## Group Message Read!!!!!");
//                            EventBubble(
//                                new ReadBubble(useCurrentTime ? Time.GetNowUnixTimestamp() : (long)shortMessage.Date,
//                                    Bubble.BubbleDirection.Incoming, this,
//                                    peerChat.ChatId.ToString(CultureInfo.InvariantCulture), null,
//                                    Time.GetNowUnixTimestamp(), true, false));
//                        }

                    }

                }
                else if (updateReadHistoryInbox != null)
                {

                    var iPeer = updateReadHistoryInbox.Peer;
                    var peerChat = iPeer as PeerChat;
                    var peerUser = iPeer as PeerUser;

                    if (peerUser != null)
                    {
                        BubbleGroup bubbleGroup = BubbleGroupManager.FindWithAddress(this,
                            peerUser.UserId.ToString(CultureInfo.InvariantCulture));
                        string idString = bubbleGroup.LastBubbleSafe().IdService;

                        if (uint.Parse(idString) <= updateReadHistoryInbox.MaxId)
                        {
                            BubbleGroupManager.SetUnread(this,false, peerUser.UserId.ToString(CultureInfo.InvariantCulture));
                            NotificationManager.Remove(this,peerUser.UserId.ToString(CultureInfo.InvariantCulture));
                        }

                    }
                    else if (peerChat != null)
                    {
                        BubbleGroup bubbleGroup = BubbleGroupManager.FindWithAddress(this,
                            peerChat.ChatId.ToString(CultureInfo.InvariantCulture));
                        string idString = bubbleGroup.LastBubbleSafe().IdService;

                        if (uint.Parse(idString) == updateReadHistoryInbox.MaxId)
                        {
                            BubbleGroupManager.SetUnread(this, false,
                                peerChat.ChatId.ToString(CultureInfo.InvariantCulture));
                            NotificationManager.Remove(this, peerChat.ChatId.ToString(CultureInfo.InvariantCulture));
                        }

                    }

                }


                else if (shortChatMessage != null)
                {
                    if (!string.IsNullOrWhiteSpace(shortChatMessage.Message))
                    {
                        var address = shortChatMessage.ChatId.ToString(CultureInfo.InvariantCulture);
                        var participantAddress = shortChatMessage.FromId.ToString(CultureInfo.InvariantCulture);
                        EventBubble(new TypingBubble(Time.GetNowUnixTimestamp(),
                            Bubble.BubbleDirection.Incoming,
                            address, participantAddress, true, this, false, false));
                        TextBubble textBubble = new TextBubble(
                            useCurrentTime ? Time.GetNowUnixTimestamp() : (long) shortChatMessage.Date,
                            shortChatMessage.Out != null
                                ? Bubble.BubbleDirection.Outgoing
                                : Bubble.BubbleDirection.Incoming,
                            address, participantAddress, true, this, shortChatMessage.Message,
                            shortChatMessage.Id.ToString(CultureInfo.InvariantCulture));
                        if (shortChatMessage.Out != null)
                        {
                            textBubble.Status = Bubble.BubbleStatus.Sent;
                        }
                        EventBubble(textBubble);
                    }
                    if (shortChatMessage.Id > maxMessageId)
                    {
                        maxMessageId = shortChatMessage.Id;
                    }
                }
                else if (message != null)
                {
                    if (!string.IsNullOrWhiteSpace(message.MessageProperty))
                    {
                        TextBubble tb = null;

                        var peerUser = message.ToId as PeerUser;
                        var peerChat = message.ToId as PeerChat;

                        var direction = message.FromId == _settings.AccountId
                            ? Bubble.BubbleDirection.Outgoing
                            : Bubble.BubbleDirection.Incoming;

                        if (peerUser != null)
                        {
                            var address = direction == Bubble.BubbleDirection.Incoming
                                ? message.FromId
                                : peerUser.UserId;
                            var addressStr = address.ToString(CultureInfo.InvariantCulture);
                            tb = new TextBubble(
                                useCurrentTime ? Time.GetNowUnixTimestamp() : (long) message.Date,
                                direction, addressStr, null, false, this, message.MessageProperty,
                                message.Id.ToString(CultureInfo.InvariantCulture));
                        }
                        else if (peerChat != null)
                        {
                            var address = peerChat.ChatId.ToString(CultureInfo.InvariantCulture);
                            var participantAddress = message.FromId.ToString(CultureInfo.InvariantCulture);
                            tb = new TextBubble(
                                useCurrentTime ? Time.GetNowUnixTimestamp() : (long) message.Date,
                                direction, address, participantAddress, true, this, message.MessageProperty,
                                message.Id.ToString(CultureInfo.InvariantCulture));
                        }

                        if (direction == Bubble.BubbleDirection.Outgoing)
                        {
                            tb.Status = Bubble.BubbleStatus.Sent;
                        }

                        EventBubble(tb);
                    }
                    if (message.Id > maxMessageId)
                    {
                        maxMessageId = message.Id;
                    }
                }
                else if (updateContactRegistered != null)
                {
                    contactsCache = new List<User>(); //invalidate cache
                }
                else if (updateContactLink != null)
                {
                    contactsCache = new List<User>();
                }
                else if (userStatus != null)
                {
                    var available = TelegramUtils.GetAvailable(userStatus.Status);
                    var userToUpdate = _dialogs.GetUser(userStatus.UserId);
                    if (userToUpdate != null)
                    {
                        var userToUpdateAsUser = userToUpdate as User;
                        if (userToUpdateAsUser != null)
                        {
                            userToUpdateAsUser.Status = userStatus.Status;
                            _dialogs.AddUser(userToUpdateAsUser);
                        }
                    }

                    EventBubble(new PresenceBubble(Time.GetNowUnixTimestamp(),
                        Bubble.BubbleDirection.Incoming,
                        userStatus.UserId.ToString(CultureInfo.InvariantCulture),
                        false, this, available));
                }
                else if (typing != null || typingChat != null)
                {
                    var isAudio = false;
                    var isTyping = false;
                    if (typing != null)
                    {
                        isAudio = typing.Action is SendMessageRecordAudioAction;
                        isTyping = typing.Action is SendMessageTypingAction;
                    }
                    if (typingChat != null)
                    {
                        isAudio = typingChat.Action is SendMessageRecordAudioAction;
                        isTyping = typingChat.Action is SendMessageTypingAction;
                    }
                    var userId = typing != null ? typing.UserId : typingChat.UserId;
                    var party = typingChat != null;
                    var participantAddress = party ? userId.ToString(CultureInfo.InvariantCulture) : null;
                    var address = party
                        ? typingChat.ChatId.ToString(CultureInfo.InvariantCulture)
                        : userId.ToString(CultureInfo.InvariantCulture);
                    var key = address + participantAddress;


                    if (isAudio || isTyping)
                    {
                        EventBubble(new TypingBubble(Time.GetNowUnixTimestamp(),
                            Bubble.BubbleDirection.Incoming,
                            address, participantAddress, party,
                            this, true, isAudio));
                        CancelTypingTimer(key);
                        var newTimer = new Timer(6000) {AutoReset = false};
                        newTimer.Elapsed += (sender2, e2) =>
                        {
                            EventBubble(new TypingBubble(Time.GetNowUnixTimestamp(),
                                Bubble.BubbleDirection.Incoming,
                                address, participantAddress, party,
                                this, false, isAudio));
                            newTimer.Dispose();
                            _typingTimers.Remove(key);
                        };
                        _typingTimers[key] = newTimer;
                        newTimer.Start();
                    }
                    else
                    {
                        Console.WriteLine("Unknown typing action: " + typing.Action.GetType().Name);
                    }
                }
                else if (user != null)
                {
                    _dialogs.AddUser(user);
                }
                else if (chat != null)
                {
                    _dialogs.AddChat(chat);
                }
                else if (updateChatParticipants != null)
                {
//                    var chatParicipants = updateChatParticipants.Participants as ChatParticipants;
//                    if (chatParicipants != null)
//                    {
//                        Task.Factory.StartNew(() =>
//                        {
//                            var address = chatParicipants.ChatId.ToString(CultureInfo.InvariantCulture);
//                            DebugPrint("Updating chat participants: " + address);
//                            RemoveFullChat(address);
//                            GetFullChat(address, optionalClient);
//                            BubbleGroupUpdater.Update(this, address);
//                        });
//                    }
                }
                else if (messageService != null)
                {
                    var editTitle = messageService.Action as MessageActionChatEditTitle;
                    var deleteUser = messageService.Action as MessageActionChatDeleteUser;
                    var addUser = messageService.Action as MessageActionChatAddUser;
                    var created = messageService.Action as MessageActionChatCreate;

                    var address = TelegramUtils.GetPeerId(messageService.ToId);
                    var fromId = messageService.FromId.ToString(CultureInfo.InvariantCulture);
                    if (editTitle != null)
                    {
                        var newTitle = editTitle.Title;
                        var chatToUpdate = _dialogs.GetChat(uint.Parse(address));
                        if (chatToUpdate != null)
                        {
                            TelegramUtils.SetChatTitle(chatToUpdate, newTitle);
                        }
                        EventBubble(PartyInformationBubble.CreateTitleChanged(
                            useCurrentTime ? Time.GetNowUnixTimestamp() : (long) messageService.Date, address,
                            this, messageService.Id.ToString(CultureInfo.InvariantCulture), fromId, newTitle));
                        BubbleGroupUpdater.Update(this, address);
                    }
                    else if (deleteUser != null)
                    {
                        var userDeleted = deleteUser.UserId.ToString(CultureInfo.InvariantCulture);
                        EventBubble(PartyInformationBubble.CreateParticipantRemoved(
                            useCurrentTime ? Time.GetNowUnixTimestamp() : (long) messageService.Date, address,
                            this, messageService.Id.ToString(CultureInfo.InvariantCulture), fromId, userDeleted));
                    }
                    else if (addUser != null)
                    {
                        foreach (var userId in addUser.Users)
                        {
                            var userAdded = userId.ToString(CultureInfo.InvariantCulture);
                            EventBubble(PartyInformationBubble.CreateParticipantAdded(
                                useCurrentTime ? Time.GetNowUnixTimestamp() : (long) messageService.Date, address,
                                this, messageService.Id.ToString(CultureInfo.InvariantCulture), fromId, userAdded));
                        }
                    }
                    else if (created != null)
                    {
                        EventBubble(PartyInformationBubble.CreateParticipantAdded(
                            useCurrentTime ? Time.GetNowUnixTimestamp() : (long) messageService.Date, address,
                            this, messageService.Id.ToString(CultureInfo.InvariantCulture), fromId,
                            _settings.AccountId.ToString(CultureInfo.InvariantCulture)));
                    }
                    else
                    {
                        Console.WriteLine("Unknown message service: " + ObjectDumper.Dump(update));
                    }
                }
                else
                {
                    Console.WriteLine("Unknown update: " + ObjectDumper.Dump(update));
                }
            }

            if (maxMessageId != 0)
            {
                SendReceivedMessages(optionalClient, maxMessageId);
            }
        }

        private class OptionalClientDisposable : IDisposable
        {
            private readonly TelegramClient _optionalClient;
            private readonly FullClientDisposable _fullClient;

            public OptionalClientDisposable(Telegram telegram, TelegramClient optionalClient = null)
            {
                _optionalClient = optionalClient;
                if (_optionalClient == null)
                {
                    _fullClient = new FullClientDisposable(telegram);
                }
            }

            public TelegramClient Client
            {
                get
                {
                    if (_optionalClient != null)
                    {
                        return _optionalClient;
                    }
                    else
                    {
                        return _fullClient.Client;
                    }
                }
            }

            public void Dispose()
            {
                if (_fullClient != null)
                {
                    _fullClient.Dispose();
                }
            }
        }

        private void SendReceivedMessages(TelegramClient optionalClient, uint maxId)
        {
            Task.Factory.StartNew(() =>
            {
                using (var disposable = new OptionalClientDisposable(this, optionalClient))
                {
                    var items = TelegramUtils.RunSynchronously(disposable.Client.Methods
                        .MessagesReceivedMessagesAsync(new MessagesReceivedMessagesArgs
                    {
                            MaxId = maxId,
                    }));
                }
            });
        }
            
        private void OnLongPollClientClosed(object sender, EventArgs e)
        {
            if (!_longPollerAborted)
            {
                Utils.DebugPrint("Looks like a long poll client closed itself internally. Restarting Telegram...");
                RestartTelegram(null);
            }
        }

        private void OnLongPollClientUpdateTooLong(object sender, EventArgs e)
        {
            if (IsFullClientConnected)
                return;
            Task.Factory.StartNew(() =>
            {
                var transportConfig = 
                    new TcpClientTransportConfig(_settings.NearestDcIp, _settings.NearestDcPort);
                using (var client = new TelegramClient(transportConfig, 
                    new ConnectionConfig(_settings.AuthKey, _settings.Salt), AppInfo))
                {
                    var result = TelegramUtils.RunSynchronously(client.Connect());
                    if (result != MTProtoConnectResult.Success)
                    {
                        throw new Exception("Failed to connect: " + result);
                    }  
                    FetchState(client);
                }
            });
        }

        private void SendPresence(TelegramClient client)
        {
            TelegramUtils.RunSynchronously(client.Methods.AccountUpdateStatusAsync(
                new AccountUpdateStatusArgs
            {
                Offline = !_hasPresence
            }));
        }

        private void Ping(TelegramClient client, Action<Exception> exception = null)
        {
            try
            {
                Utils.DebugPrint("Sending ping!");
                var pong = (Pong)TelegramUtils.RunSynchronously(client.ProtoMethods.PingAsync(new PingArgs
                {
                    PingId = GetRandomId(),
                }));
                Utils.DebugPrint("Got pong (from ping): " + pong.MsgId);
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Failed to ping client: " + ex);
                if (exception != null)
                {
                    exception(ex);
                }
            }
        }
            
        private async void PingDelay(TelegramClient client, uint disconnectDelay, Action<Exception> exception = null)
        {
            try
            {
                Utils.DebugPrint("Sending pingDelay!");
                var pong = (Pong)await client.ProtoMethods.PingDelayDisconnectAsync(new PingDelayDisconnectArgs
                {
                    PingId = GetRandomId(),
                    DisconnectDelay = disconnectDelay
                });
                Utils.DebugPrint("Got pong (from pingDelay): " + pong.MsgId);
            }
            catch (Exception ex)
            {
                if (exception != null)
                {
                    exception(ex);
                }
            }
        }

        private void RestartTelegram(Exception exception)
        {
            if (exception != null)
            {
                Utils.DebugPrint("Restarting Telegram: " + exception);
            }
            else
            {
                Utils.DebugPrint("Restarting Telegram");
            }
            // start a new task, freeing the possibility that there could be a wake lock being held
            Task.Factory.StartNew(() =>
            {
                ServiceManager.Restart(this);
            });
        }

        private void ScheduleLongPollPing()
        {
            RemoveLongPollPingIfPossible();
            _longPollHeartbeart = new WakeLockBalancer.GracefulWakeLock(new WakeLockBalancer.ActionObject(() =>
            {
                if (_longPollClient == null || !_longPollClient.IsConnected)
                {
                    RemoveLongPollPingIfPossible();
                    RestartTelegram(null);
                }
                else
                {
                    Ping(_longPollClient, RestartTelegram);
                }
            }, WakeLockBalancer.ActionObject.ExecuteType.TaskWithWakeLock), 240, 60, true);
            Platform.ScheduleAction(_longPollHeartbeart);
        }

        private void RemoveLongPollPingIfPossible()
        {
            if (_longPollHeartbeart != null)
            {
                Platform.RemoveAction(_longPollHeartbeart);
                _longPollHeartbeart = null;
            }
        }

        private void DisconnectLongPollerIfPossible()
        {
            if (_longPollClient != null && _longPollClient.IsConnected)
            {
                _longPollerAborted = true;
                try
                {
                    TelegramUtils.RunSynchronously(_longPollClient.Disconnect());
                }
                catch (Exception ex)
                {
                    DebugPrint("Failed to disconnect full client: " + ex);
                }
                RemoveLongPollPingIfPossible();
            }
        }

        private ulong GetRandomId()
        {
            var buffer = new byte[sizeof(ulong)];
            _random.NextBytes(buffer);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public Telegram()
        {
            _baseMessageId = Convert.ToString(Time.GetNowUnixTimestamp());
        }

        public override bool Initialize(DisaSettings settings)
        {
            _settings = settings as TelegramSettings;
            _mutableSettings = MutableSettingsManager.Load<TelegramMutableSettings>();

            if (_settings.AuthKey == null)
            {
                return false;
            }

            return true;
        }

        public override bool InitializeDefault()
        {
            return false;
        }

        public override bool Authenticate(WakeLock wakeLock)
        {
            return true;
        }

        public override void Deauthenticate()
        {
            // do nothing
        }

        private void FetchDifference(TelegramClient client)
        {
            var counter = 0;

            DebugPrint("Fetching difference");

            Again:

            DebugPrint("Difference Page: " + counter);

            var difference = TelegramUtils.RunSynchronously(
                client.Methods.UpdatesGetDifferenceAsync(new UpdatesGetDifferenceArgs
            {
                Date = _mutableSettings.Date,
                Pts = _mutableSettings.Pts,
                Qts = _mutableSettings.Qts
            }));

            var empty = difference as UpdatesDifferenceEmpty;
            var diff = difference as UpdatesDifference;
            var slice = difference as UpdatesDifferenceSlice;

            Action dispatchUpdates = () =>
            {
                var updates = new List<object>();
                //TODO: encrypyed messages
                if (diff != null)
                {
                    updates.AddRange(diff.NewMessages);
                    updates.AddRange(diff.OtherUpdates);
                }
                else if(slice!=null)
                {
                    updates.AddRange(slice.NewMessages);
                    updates.AddRange(slice.OtherUpdates);
                }
                DebugPrint(ObjectDumper.Dump(updates));
                ProcessIncomingPayload(updates, false, client);
            };

            if (diff != null)
            {
                dispatchUpdates();
                var state = (UpdatesState)diff.State;
                SaveState(state.Date, state.Pts, state.Qts, state.Seq);
            }
            else if (slice != null)
            {
                dispatchUpdates();
                var state = (UpdatesState)slice.IntermediateState;
                SaveState(state.Date, state.Pts, state.Qts, state.Seq);
                counter++;
                goto Again;
            }
            else if (empty != null)
            {
                SaveState(empty.Date, 0, 0, empty.Seq);
            }
        }

        private void FetchState(TelegramClient client)
        {
            if (_mutableSettings.Date == 0)
            {
                DebugPrint("We need to fetch the state!");
                var state = (UpdatesState)TelegramUtils.RunSynchronously(client.Methods.UpdatesGetStateAsync(new UpdatesGetStateArgs()));
                SaveState(state.Date, state.Pts, state.Qts, state.Seq);
            }
            else
            {
                FetchDifference(client);
            }
        }

        public override void Connect(WakeLock wakeLock)
        {
            var sessionId = GetRandomId();
            var transportConfig = 
                new TcpClientTransportConfig(_settings.NearestDcIp, _settings.NearestDcPort);
            using (var client = new TelegramClient(transportConfig, 
                                    new ConnectionConfig(_settings.AuthKey, _settings.Salt), AppInfo))
            {
                var result = TelegramUtils.RunSynchronously(client.Connect());
                if (result != MTProtoConnectResult.Success)
                {
                    throw new Exception("Failed to connect: " + result);
                }   
                DebugPrint("Registering long poller...");
                var registerDeviceResult = TelegramUtils.RunSynchronously(client.Methods.AccountRegisterDeviceAsync(
                    new AccountRegisterDeviceArgs
                {
                    TokenType = 7,
                    Token = sessionId.ToString(CultureInfo.InvariantCulture),
                    DeviceModel = AppInfo.DeviceModel,
                    SystemVersion = AppInfo.SystemVersion,
                    AppVersion = AppInfo.AppVersion,
                    AppSandbox = false,
                    LangCode = AppInfo.LangCode
                }));
                if (!registerDeviceResult)
                {
                    throw new Exception("Failed to register long poller...");
                }

                DebugPrint(">>>>>>>>>>>>>> Fetching state!");
                FetchState(client);
                DebugPrint (">>>>>>>>>>>>>> Fetching dialogs!");
                GetDialogs(client);
                _dialogsInitiallyRetrieved = true;
                
                GetConfig(client);
            }

            DebugPrint("Starting long poller...");
            if (_longPollClient != null)
            {
                _longPollClient.OnUpdateTooLong -= OnLongPollClientUpdateTooLong;
                _longPollClient.OnClosedInternally -= OnLongPollClientClosed;
            }
            _longPollerAborted = false;
            _longPollClient = new TelegramClient(transportConfig, 
                new ConnectionConfig(_settings.AuthKey, _settings.Salt) { SessionId = sessionId }, AppInfo);
            var result2 = TelegramUtils.RunSynchronously(_longPollClient.Connect());
            if (result2 != MTProtoConnectResult.Success)
            {
                throw new Exception("Failed to connect long poll client: " + result2);
            } 
            _longPollClient.OnUpdateTooLong += OnLongPollClientUpdateTooLong;
            _longPollClient.OnClosedInternally += OnLongPollClientClosed;
           	ScheduleLongPollPing();
            DebugPrint("Long poller started!");
        }

        public override void Disconnect()
        {
            DisconnectFullClientIfPossible();
            DisconnectLongPollerIfPossible();
        }

        public override string GetIcon(bool large)
        {
            if (large)
            {
                return Constants.LargeIcon;
            }

            return Constants.SmallIcon;
        }

        public override IEnumerable<Bubble> ProcessBubbles()
        {
            throw new NotImplementedException();
        }

        public override void SendBubble(Bubble b)
        {
            var presenceBubble = b as PresenceBubble;
            if (presenceBubble != null)
            {
                _hasPresence = presenceBubble.Available;
                SetFullClientPingDelayDisconnect();
                if (_hasPresence)
                {
                    var updatedUsers = GetUpdatedUsersOfAllDialogs();
                    if (updatedUsers != null)
                    {
                        foreach (var updatedUser in updatedUsers)
                        {
                            EventBubble(new PresenceBubble(Time.GetNowUnixTimestamp(), Bubble.BubbleDirection.Incoming, 
                                TelegramUtils.GetUserId(updatedUser), false, this, TelegramUtils.GetAvailable(updatedUser)));
                        }
                    }
                }
                using (var client = new FullClientDisposable(this))
                {
                    SendPresence(client.Client);
                }
            }

            var typingBubble = b as TypingBubble;
            if (typingBubble != null)
            {
                var peer = GetInputPeer(typingBubble.Address, typingBubble.Party);
                using (var client = new FullClientDisposable(this))
                {
                    TelegramUtils.RunSynchronously(client.Client.Methods.MessagesSetTypingAsync(
                        new MessagesSetTypingArgs
                        {
                            Peer = peer,
                            Action = typingBubble.IsAudio ? (ISendMessageAction)new SendMessageRecordAudioAction() : (ISendMessageAction)new SendMessageTypingAction()
                        }));
                }
            }

            var textBubble = b as TextBubble;
            if (textBubble != null)
            {
                var peer = GetInputPeer(textBubble.Address, textBubble.Party);
               
                using (var client = new FullClientDisposable(this))
                {
                    var iUpdate = TelegramUtils.RunSynchronously(
                        client.Client.Methods.MessagesSendMessageAsync(new MessagesSendMessageArgs
                    {
						Flags = 0,
                        Peer = peer,
                        Message = textBubble.Message,
                        RandomId = ulong.Parse(textBubble.IdService2)
                    }));
                    var updateShortSentMessage = iUpdate as UpdateShortSentMessage;
                    if (updateShortSentMessage != null)
                    {
                        textBubble.IdService = updateShortSentMessage.Id.ToString(CultureInfo.InvariantCulture);
                    }
                }
            }
            
            var readBubble = b as ReadBubble;
            if (readBubble != null)
            {
                var peer = GetInputPeer(readBubble.Address, readBubble.Party);
                using (var client = new FullClientDisposable(this))
                {
                    var messagesAffectedMessages =
                        TelegramUtils.RunSynchronously(client.Client.Methods.MessagesReadHistoryAsync(
                        new MessagesReadHistoryArgs
                    {
                        Peer = peer,
                        MaxId = 0,

                    })) as MessagesAffectedMessages;
                    if (messagesAffectedMessages != null)
                    {
                        SaveState(0, messagesAffectedMessages.Pts, 0, 0);
                    }
                }
            }
        }

        void SendToResponseDispatcher(IUpdates iUpdate,TelegramClient client)
        {
                var mtProtoClientConnection = client.Connection as MTProtoClientConnection;
                if (mtProtoClientConnection != null)
                {
                    var responseDispatcher = mtProtoClientConnection.ResponseDispatcher as ResponseDispatcher;
                    if (responseDispatcher != null)
                    {
                        SharpMTProto.Schema.IMessage tempMessage = new SharpMTProto.Schema.Message(0,0,iUpdate);
                        responseDispatcher.DispatchAsync(tempMessage).Wait();
                    }
                }
        }

        private IInputPeer GetInputPeer(string userId, bool groupChat)
        {
            if (groupChat)
            {
                return new InputPeerChat
                {
                    ChatId = uint.Parse(userId)
                };
            }
            else
            {
                var accessHash = GetUserAccessHashIfForeign(userId);

                return new InputPeerUser
                {
					UserId = uint.Parse(userId),
					AccessHash = accessHash
                };
                
            }
        }

        private ulong GetUserAccessHashIfForeign(string userId)
        { 
            var user = _dialogs.GetUser(uint.Parse(userId));
            if (user != null)
            {
                return TelegramUtils.GetAccessHash(user);
            }
            return 0;
        }

        public override bool BubbleGroupComparer(string first, string second)
        {
            return first == second;
        }

        public override Task GetBubbleGroupLegibleId(BubbleGroup group, Action<string> result)
        {
            throw new NotImplementedException();
        }

        private async Task<List<IUser>> GetUsers(List<IInputUser> users, TelegramClient client)
        {
            var response = await client.Methods.UsersGetUsersAsync(new UsersGetUsersArgs
            {
                Id = users
            });
            return response;
        }

        private async Task<IUser> GetUser(IInputUser user, TelegramClient client)
        {
            return (await GetUsers(new List<IInputUser> { user }, client)).First();
        }

        private async Task<List<User>> FetchContacts()
        {
            if (contactsCache.Count != 0)
            {
                return contactsCache;
            }
            using (var client = new FullClientDisposable(this))
            {
                var response = (ContactsContacts)await client.Client.Methods.ContactsGetContactsAsync(
                    new ContactsGetContactsArgs
                {
                    Hash = string.Empty
                });
                contactsCache.AddRange(response.Users.OfType<User>().ToList());
                return contactsCache;
            }
        }

        public override Task GetBubbleGroupName(BubbleGroup group, Action<string> result)
        {
            DebugPrint("GetBubbleGroupName");
            return Task.Factory.StartNew(() =>
            {
                result(GetTitle(group.Address, group.IsParty));
            });
        }

        public override Task GetBubbleGroupPhoto(BubbleGroup group, Action<DisaThumbnail> result)
        {
            DebugPrint("GetBubbleGroupPhoto");
            return Task.Factory.StartNew(() =>
            {
                result(GetThumbnail(group.Address, group.IsParty, true));
            });
        }

        public override Task GetBubbleGroupPartyParticipants(BubbleGroup group, Action<DisaParticipant[]> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(null);
            });
        }

        public override Task GetBubbleGroupUnknownPartyParticipant(BubbleGroup group, string unknownPartyParticipant, Action<DisaParticipant> result)
        {
            DebugPrint("###### get bubble group unkonwn participants");
            return Task.Factory.StartNew(() =>
            {
                //TODO: this may not actually get title always.
                var name = GetTitle(unknownPartyParticipant, false);
                DebugPrint("###### The name of the unknown participant is " + name);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    result(new DisaParticipant(name, unknownPartyParticipant));
                }
                else
                {
                    result(null);
                }
            });
        }

        public override Task GetBubbleGroupPartyParticipantPhoto(DisaParticipant participant, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(GetThumbnail(participant.Address, false, true));
            });
        }

        public override Task GetBubbleGroupLastOnline(BubbleGroup group, Action<long> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(GetUpdatedLastOnline(group.Address));
            });
        }

        public void AddVisualBubbleIdServices(VisualBubble bubble)
        {
            bubble.IdService2 = NextMessageId;
        }

        public bool DisctinctIncomingVisualBubbleIdServices()
        {
            return true;
        }

        public override void RefreshPhoneBookContacts()
        {
            Utils.DebugPrint("Phone book contacts have been updated! Sending information to Telegram servers...");
            var contacts = PhoneBook.PhoneBookContacts;
            var inputContacts = new List<IInputContact>();
            foreach (var contact in contacts)
            {
                foreach (var phoneNumber in contact.PhoneNumbers)
                {
                    var clientId = "0";
                    if (contact.ContactId != null)
                    {
                        clientId = contact.ContactId;
                    }
                    inputContacts.Add(new InputPhoneContact
                    {
                        ClientId = ulong.Parse(clientId),
                        Phone = phoneNumber.Number,
                        FirstName = contact.FirstName,
                        LastName = contact.LastName,
                    });
                }
            }
            if (!inputContacts.Any())
            {
                Utils.DebugPrint("There are no input contacts!");
                return;
            }
            try
            {
                using (var client = new FullClientDisposable(this))
                {
                    var importedContacts = TelegramUtils.RunSynchronously(client.Client.Methods.ContactsImportContactsAsync(
                        new ContactsImportContactsArgs
                        {
                            Contacts = inputContacts,
                            Replace = false,
                        }));
                    var contactsImportedcontacts = importedContacts as ContactsImportedContacts;
                    if (contactsImportedcontacts != null)
                    {
                        _dialogs.AddUsers(contactsImportedcontacts.Users);
                    }
                    //invalidate the cache
                    contactsCache = new List<User>();
                }
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Failed to update contacts: " + ex);
            }
        }

        private IUser GetUser(List<IUser> users, string userId)
        {
            foreach (var user in users)
            {
                var userIdInner = TelegramUtils.GetUserId(user);
                if (userId == userIdInner)
                {
                    return user;
                }
            }
            return null;
        }

        private List<IUser> GetUpdatedUsersOfAllDialogs()
        {
            var users = new List<IUser>();
            foreach (var userBubbleGroup in BubbleGroupManager.FindAll(this).Where(x => !x.IsParty))
            {
                var user = _dialogs.GetUser(uint.Parse(userBubbleGroup.Address));
                if (user != null)
                {
                    users.Add(user);
                }
            }

            return users;
        }

        private long GetUpdatedLastOnline(string id)
        {
            var user = _dialogs.GetUser(uint.Parse(id));
            if (user != null)
            {
                return TelegramUtils.GetLastSeenTime(user);
            }
            return 0;
        }

        private string GetTitle(string id, bool group)
        {
            if (id == null)
            {
                return null;
            }
            if (group)
            {
                var chat = _dialogs.GetChat(uint.Parse(id));
                if (chat == null)
                {
                    return null;
                }
                return TelegramUtils.GetChatTitle(chat);
            }
            else
            {
                var user = _dialogs.GetUser(uint.Parse(id));
                if (user == null)
                {
                    //we havent recieved a user object for this user, we should probably get it and cache it
                    IUser newUser = GetUser(id);
                    if (newUser == null)
                    {
                        return null;
                    }
                    return TelegramUtils.GetUserName(newUser);
                }
                return TelegramUtils.GetUserName(user);
            }
        }

        private IUser GetUser(string id)
        {
            lock (_quickUserLock)
            {
                using (var client = new FullClientDisposable(this))
                {
                    var inputUser = new InputUser();
                    inputUser.UserId = uint.Parse(id);
                    var inputList = new List<IInputUser>();
                    inputList.Add(inputUser);
                    var users = TelegramUtils.RunSynchronously(client.Client.Methods.UsersGetUsersAsync(new UsersGetUsersArgs
                    {
                        Id = inputList,
                    }));
                    var user = users.FirstOrDefault();
                    return user;
                }
            }
    }

        private DisaThumbnail GetThumbnail(string id, bool group, bool small)
        {
            if (id == null)
            {
                return null;
            }
            var key = id + group + small;

            Func<DisaThumbnail, DisaThumbnail> cache = thumbnail =>
            {
                lock (_cachedThumbnails)
                {
                    _cachedThumbnails[key] = thumbnail;
                }
                return thumbnail;
            };

            lock (_cachedThumbnails)
            {
                if (_cachedThumbnails.ContainsKey(key))
                {
                    return _cachedThumbnails[key];
                }
            }
            if (group)
            {
                var chat = _dialogs.GetChat(uint.Parse(id));
                if (chat == null)
                {
                    return null;
                }
                var fileLocation = TelegramUtils.GetChatThumbnailLocation(chat, small);
                if (fileLocation == null)
                {
                    return cache(null);
                }
                else
                {
                    var bytes = FetchFileBytes(fileLocation);
                    return cache(new DisaThumbnail(this, bytes, key));
                }
                    
                
            }
            else
            {
                Func<IUser, DisaThumbnail> getThumbnail = user =>
                {
                    var fileLocation = TelegramUtils.GetUserPhotoLocation(user, small);
                    if (fileLocation == null)
                    {
                        return cache(null);
                    }
                    else
                    {
                        var bytes = FetchFileBytes(fileLocation);
                        return cache(new DisaThumbnail(this, bytes, key));
                    }
                };

                var userImg = _dialogs.GetUser(uint.Parse(id));
                return userImg == null ? null : getThumbnail(userImg);
            }
        }

        //TODO: chunk the download
        private static byte[] FetchFileBytes(TelegramClient client, FileLocation fileLocation)
        {
            var response = (UploadFile)TelegramUtils.RunSynchronously(client.Methods.UploadGetFileAsync(
                new UploadGetFileArgs
            {
                Location = new InputFileLocation
                {
                    VolumeId = fileLocation.VolumeId,
                    LocalId = fileLocation.LocalId,
                    Secret = fileLocation.Secret
                },
                Offset = 0,
                Limit = uint.MaxValue,
            }));
            return response.Bytes;
        }
            
        private byte[] FetchFileBytes(FileLocation fileLocation)
        {
            if (fileLocation.DcId == _settings.NearestDcId)
            {
                using (var clientDisposable = new FullClientDisposable(this))
                {
                    return FetchFileBytes(clientDisposable.Client, fileLocation);
                }   
            }
            else
            {
                try
                {
                    var client = GetClient((int)fileLocation.DcId);
                    return FetchFileBytes(client, fileLocation);
                }
                catch (Exception ex)
                {
                    DebugPrint("Failed to obtain client from DC manager: " + ex);
                    return null;
                }
            }
        }

        private void GetConfig(TelegramClient client)
        {
			DebugPrint (">>>>>>>>>>>>>> Getting config!");
            var config = (Config)TelegramUtils.RunSynchronously(client.Methods.HelpGetConfigAsync(new HelpGetConfigArgs
                {
                }));
            _config = config;
			DebugPrint (ObjectDumper.Dump(_config));
        }
 
//        private MessagesChatFull GetFullChat(string address, TelegramClient optionalClient = null)
//        {
//            using (var disposable = new OptionalClientDisposable(this, optionalClient))
//            {
//                return GetFullChat(disposable.Client, address);
//            }
//        }

//        private MessagesChatFull GetFullChat(TelegramClient client, string address)
//        {
//            var id = uint.Parse(address);
//            foreach (var iChatFull in _dialogs.FullChats)
//            {
//                var chatFull = iChatFull as MessagesChatFull;
//                if (chatFull != null)
//                {
//                    var chatFull2 = chatFull.FullChat as ChatFull;
//                    if (id == chatFull2.Id)
//                    {
//                        return chatFull;
//                    }
//                }
//            }
//            if (_dialogs.FullChatFailures.FirstOrDefault(x => x == address) != null)
//            {
//                return null;
//            }
//            else
//            {
//                return FetchAndCacheFullChat(client, _dialogs, address);
//            }
//        }

//        private void RemoveFullChat(string address)
//        {
//            var id = uint.Parse(address);
//            foreach (var iChatFull in _dialogs.FullChats)
//            {
//                var chatFull = iChatFull as MessagesChatFull;
//                if (chatFull != null)
//                {
//                    var chatFull2 = chatFull.FullChat as ChatFull;
//                    if (id == chatFull2.Id)
//                    {
//                        _dialogs.FullChats.Remove(iChatFull);
//                        break;
//                    }
//                }
//            }
//            var chatFailure = _dialogs.FullChatFailures.FirstOrDefault(x => address == x);
//            if (chatFailure != null)
//            {
//                _dialogs.FullChatFailures.Remove(chatFailure);
//            }
//        }
//
//        // See FetchAndCacheFullChat
//        private void FetchFullChatsForParties(TelegramClient client, CachedDialogs dialogs)
//        {
//            foreach (var group in BubbleGroupManager.FindAll(this).Where(x => x.IsParty))
//            {
//                FetchAndCacheFullChat(client, dialogs, group.Address);
//            }
//        }

        //FIXME: fully implement and persist to cache.
        // This one is actually called because MessagesGetDialogs doesn't list participant information.
        // When doing initial cache, or we're given info from starting a new party chat,
        // or a new incoming party chat, or added/remove particpants, change name, thumbnails, etc.
        // we need to refetch the full chat. This also needs to be cached in the same
        // structure as GetDialogs (CachedDialogs). Some of this updating can be seen
        // in ProcessIncomingPayload... although not all is yet implemented.
//        private static MessagesChatFull FetchAndCacheFullChat(TelegramClient client, 
//            CachedDialogs dialogs, string address)
//        {
//            var fullChat = FetchFullChat(client, address);
//            Utils.DebugPrint(">>>> fullchat " + ObjectDumper.Dump(fullChat));
//            if (fullChat != null)
//            {
//                dialogs.FullChats.Add(fullChat);
//            }
//            else
//            {
//                dialogs.FullChatFailures.Add(address);   
//            }
//            return fullChat;
//        }

//        private static MessagesChatFull FetchFullChat(TelegramClient client, string address)
//        {
//            var fullChat = (MessagesChatFull)TelegramUtils.RunSynchronously(client.Methods.MessagesGetFullChatAsync(new MessagesGetFullChatArgs
//            {
//                ChatId = uint.Parse(address) 
//            }));
//            return fullChat;
//        }

        //FIXME: If you have more than 100 convos, client will break.
        // >>>>> Telegram's implementation:
        // Telegram has a weird system and naming conventions. Basically:
        // Dialogs are conversations. They can be both parties or solo conversations.
        // Users are single users. They relate to solo conversations.
        // Chats are parties. They relate to party conversations.
        // When Telegram loads, it will load last 100 dialogs from a cache. If there's no cache, then it will call
        // MessagesGetDialogs(). This is done by setting Limit = 100, and OffsetPeer = PeerEmpty.
        // When you scroll to the bottom of the list in Telegram, it will load in an additional 100.
        // This is done by setting OffsetDate, OffsetPeer and OffsetId. These values are obtained by picking the oldest message from CachedDialogs.Messege, 
        // and then using it's attributes. 
        // When the user/party sends you a brand new piece of information (message, etc.), 
        // the dialogs/users/messages/chats will appear in and Update. Likewise when making the action (e.g starting a new chat, or party)
        // you should also get information in the form of an update.
        // >>>>> Disa's implementation:
        // On first ever auth start, we load the last 100 dialogs, as like in official Telegram client.
        // We then cache these and save them in a SQL database. If the SQL database ever corrupts, then we just redo it.
        // When a new message comes in or we start a new party, etc, we must update this CachedDialogs cache with the provided incoming payload info. This can already
        // be seen at work in ProcessIncomingPayload (an Update as aformentioned in Telegram impl.). We must just persist it to cache as well.
        // In summary, we'll leave this for now, but we must move this to only ever being called once (on a new auth or re-auth, 
        // or db corruption), and then accordingly update this cache when new information is provided.
        // It should be noted that with out implementation, we don't need the offset code, since if a user has more than 100 convos,
        // he's expected to manually load them in by going to NewMessage, which will force the Updates to come in.
        // Please see FetchFullChatsForParties for further information.
        //TODO: do this 100 at a time
        private void GetDialogs(TelegramClient client)
        {
            DebugPrint("Fetching conversations");
            var masterDialogs = new CachedDialogs();

            if (!masterDialogs.DatabasesExist())
            {
                DebugPrint("Databases dont exist! creating new ones from data from server");
                var iDialogs =
                    TelegramUtils.RunSynchronously(client.Methods.MessagesGetDialogsAsync(new MessagesGetDialogsArgs
                    {
                        Limit = 100,
                        OffsetPeer = new InputPeerEmpty(),
                    }));
                var messagesDialogs = iDialogs as MessagesDialogs;

                var messagesDialogsSlice = iDialogs as MessagesDialogsSlice;

                if (messagesDialogs != null)
                {
                    masterDialogs.AddChats(messagesDialogs.Chats);
                    masterDialogs.AddUsers(messagesDialogs.Users);
                }
                else if (messagesDialogsSlice != null)
                {
                    //first add whatever we have got until now
                    masterDialogs.AddChats(messagesDialogsSlice.Chats);
                    masterDialogs.AddUsers(messagesDialogsSlice.Users);
                    var numDialogs = 0;
                    do
                    {
                        numDialogs = messagesDialogsSlice.Dialogs.Count;

                        DebugPrint("%%%%%%% Number of Dialogs " + numDialogs);

                        var lastDialog = messagesDialogsSlice.Dialogs.LastOrDefault() as Dialog;

                        DebugPrint("%%%%%%% Last Dialog " + ObjectDumper.Dump(lastDialog));

                        if (lastDialog != null)
                        {
                            var lastPeer = GetInputPeerFromIPeer(lastDialog.Peer);
                            DebugPrint("%%%%%%% Last Peer " + ObjectDumper.Dump(lastPeer));
                            var offsetId = Math.Max(lastDialog.ReadInboxMaxId, lastDialog.TopMessage);
                            DebugPrint("%%%%%%% message offset " + ObjectDumper.Dump(offsetId));
                            var offsetDate = FindDateForMessageId(offsetId,messagesDialogsSlice);
                            DebugPrint("%%%%%%% offset date " + ObjectDumper.Dump(offsetDate));
                            var nextDialogs = TelegramUtils.RunSynchronously(client.Methods.MessagesGetDialogsAsync(new MessagesGetDialogsArgs
                            {
                                Limit = 100,
                                OffsetPeer = lastPeer,
                                OffsetId = offsetId,
                                OffsetDate = offsetDate
                            }));

                            messagesDialogsSlice = nextDialogs as MessagesDialogsSlice;
                            if (messagesDialogsSlice == null)
                            {
                                DebugPrint("%%%%%%% Next messages dialogs null ");
                                break;
                            }

                            DebugPrint("%%%%%%% users " + ObjectDumper.Dump(messagesDialogsSlice.Users));

                            DebugPrint("%%%%%%% chats " + ObjectDumper.Dump(messagesDialogsSlice.Chats));

                            masterDialogs.AddUsers(messagesDialogsSlice.Users);
                            masterDialogs.AddChats(messagesDialogsSlice.Chats);

                        }
                        DebugPrint("%%%%%%% Number of Dialogs At end " + numDialogs);

                    } while (numDialogs >= 100);
                }
            }

//            //////////////////////////
//
//            //test code
//            try
//            {
//                var d =
//                    TelegramUtils.RunSynchronously(client.Methods.MessagesGetDialogsAsync(new MessagesGetDialogsArgs
//                    {
//                        Limit = 100,
//                        OffsetPeer = new InputPeerEmpty(),
//                    }));
//                var d1 = d as MessagesDialogs;
//                var d2 = d as MessagesDialogsSlice;
//
//                if (d2 != null)
//                {
//                    Utils.DebugPrint("######## Dialogs Slice Count" + ObjectDumper.Dump(d2.Dialogs.Count));
//                    Utils.DebugPrint("######## Dialogs Slice" + ObjectDumper.Dump(d2.Dialogs));
//                   // Utils.DebugPrint("####### Full Dialogs " + ObjectDumper.Dump(d2));
//                    var lastPeerDialog = d2.Dialogs.LastOrDefault() as Dialog;
//                    var peerUser = lastPeerDialog.Peer as PeerUser;
//                    Utils.DebugPrint("####### The peer user is " + ObjectDumper.Dump(peerUser));
//                    Utils.DebugPrint("####### The user is " + ObjectDumper.Dump(_dialogs.GetUser(peerUser.UserId)));
//                    if (peerUser!=null)
//                    {
//                        var inputPeer = new InputPeerUser();
//                        inputPeer.UserId = peerUser.UserId;
//                        var offset = Math.Max(lastPeerDialog.ReadInboxMaxId, lastPeerDialog.TopMessage);
//                        Utils.DebugPrint("####### The offset message id is " + offset);
//                        uint date = 0;
//                        foreach(var imessage  in d2.Messages)
//                        {
//                            var message = imessage as Message;
//                            if (message != null)
//                            {
//                                if (message.Id == offset)
//                                {
//                                    Utils.DebugPrint("####### message found!!! " + ObjectDumper.Dump(message));
//                                    date = message.Date;
//                                }
//                            }
//                            var messageService = imessage as MessageService;
//                            if (messageService != null)
//                            {
//                                if (messageService.Id == offset)
//                                {
//                                    Utils.DebugPrint("####### message service found!!! " + ObjectDumper.Dump(messageService));
//                                    date = messageService.Date;
//                                }
//                            }
//
//                        }
//
//                        var e =
//                            TelegramUtils.RunSynchronously(
//                                client.Methods.MessagesGetDialogsAsync(new MessagesGetDialogsArgs
//                                {
//                                    Limit = 100,
//                                    OffsetPeer = inputPeer,
//                                    OffsetId = offset,
//                                    OffsetDate = date,
//                                }));
//                        var e1 = e as MessagesDialogs;
//                        var e2 = e as MessagesDialogsSlice;
//                        if (e1 != null)
//                        {
//                            Utils.DebugPrint("######## Dialogs internal Count" + ObjectDumper.Dump(e1.Dialogs.Count));
//                            Utils.DebugPrint("######## Dialogs internal" + ObjectDumper.Dump(e1.Dialogs));
//                        }
//                        if (e2 != null)
//                        {
//                            Utils.DebugPrint("######## Dialogs slice internal Count" + ObjectDumper.Dump(e2.Dialogs.Count));
//                            Utils.DebugPrint("######## Dialogs slice internal" + ObjectDumper.Dump(e2.Dialogs));
//                        }
//
//                    }
//                }
//                else
//                {
//                    Utils.DebugPrint("###### Dialogs Slice null");
//                }
//
//                if (d1 != null)
//                {
//                    Utils.DebugPrint("######## Dialogs Count" + ObjectDumper.Dump(d1.Dialogs.Count));
//                    Utils.DebugPrint("######## Dialogs" + ObjectDumper.Dump(d1.Dialogs));
//                }
//                else
//                {
//                    Utils.DebugPrint("###### Dialogs null");
//                }
//
//            }
//            catch (Exception e)
//            {
//                DebugPrint("###### exception" + e);
//            }
//            ////////////////////////////


            _dialogs = masterDialogs;
            //FetchFullChatsForParties(client, masterDialogs);

            DebugPrint("Obtained conversations.");
        }

        private IInputPeer GetInputPeerFromIPeer(IPeer peer)
        {
            IInputPeer retInputUser = null;
            var peerUser = peer as PeerUser;
            var peerChat = peer as PeerChat;
            var peerChannel = peer as PeerChannel;

            if (peerUser != null)
            {
                var inputPeerUser = new InputPeerUser
                {
                    UserId = peerUser.UserId
                };
                retInputUser = inputPeerUser;
            }
            else if (peerChat != null)
            {
                var inputPeerChat = new InputPeerChat
                {
                    ChatId = peerChat.ChatId
                };
                retInputUser = inputPeerChat;
            }
            else if(peerChannel!= null)
            {
                var inputPeerChannel = new InputPeerChannel
                {
                    ChannelId = peerChannel.ChannelId,
                };
                retInputUser = inputPeerChannel;
            }
            return retInputUser;

        }

        private uint FindDateForMessageId(uint offsetId, MessagesDialogsSlice messagesDialogsSlice)
        {
            uint date = 0;
            foreach (var iMessage in messagesDialogsSlice.Messages)
            {
                var message = iMessage as Message;
                if (message != null)
                {
                    if (message.Id == offsetId)
                    {
                        Utils.DebugPrint("####### message found!!! " + ObjectDumper.Dump(message));
                        date = message.Date;
                    }
                }
                var messageService = iMessage as MessageService;
                if (messageService != null)
                {
                    if (messageService.Id == offsetId)
                    {
                        Utils.DebugPrint("####### message service found!!! " + ObjectDumper.Dump(messageService));
                        date = messageService.Date;
                    }
                }

            }
            return date;
        }
    }
}

