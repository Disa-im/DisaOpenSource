using System;
using System.Threading.Tasks;
using SharpTelegram.Schema;


namespace Disa.Framework.Telegram
{
    public partial class Telegram : INewMessageExtended
    {
        private enum LinkType
        {
            PrivateGroup,
            PublicGroup,
            PublicUser,
            Invalid
        }

        public bool SupportsShareLinks
        {
            get
            {
                return true;
            }
        }

        public Task FetchBubbleGroupAddressFromLink(string link, Action<Tuple<Contact, Contact.ID>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                string linkUsefulPart;
                var linkType = GetLinkType(link, out linkUsefulPart);
                DebugPrint("Link useful part " + ObjectDumper.Dump(linkType));
                switch (linkType)
                {
                    case LinkType.Invalid:
                        result(new Tuple<Contact, Contact.ID>(null, null));
                        break;
                    case LinkType.PrivateGroup:
                        IChat alreadyJoinedChat;
                        bool linkCheck = CheckLink(linkUsefulPart, out alreadyJoinedChat);
                        if (linkCheck)
                        {
                            if (alreadyJoinedChat != null)
                            {
                                SetResultAsChat(alreadyJoinedChat, TelegramUtils.GetChatId(alreadyJoinedChat), result);
                                return;
                            }
                            var updates = JoinChat(linkUsefulPart);
                            DebugPrint("Updates " + ObjectDumper.Dump(updates));
                            var updatesObj = updates as Updates;
                            if (updatesObj != null)
                            {
                                var chatObj = updatesObj.Chats[0];
                                SetResultAsChat(chatObj, TelegramUtils.GetChatId(chatObj), result);
                            }
                            
                        }
                        else
                        {
                            result(new Tuple<Contact, Contact.ID>(null,null));  
                        }
                        break;
                    case LinkType.PublicGroup:
                        JoinChannelIfLeft(linkUsefulPart);
                        var chat = _dialogs.GetChat(uint.Parse(linkUsefulPart));
                        SetResultAsChat(chat, linkUsefulPart, result);
                        break;
                    case LinkType.PublicUser:
                        var user = _dialogs.GetUser(uint.Parse(linkUsefulPart));
                        var userObj = user as User;
                        var userContact = new TelegramContact
                        {
                            Available = TelegramUtils.GetAvailable(user),
                            LastSeen = TelegramUtils.GetLastSeenTime(user),
                            FirstName = userObj?.FirstName,
                            LastName = userObj?.LastName,
                            User = userObj,
                        };
                        var userContactId = new Contact.ID
                        {
                            Id = linkUsefulPart,
                            Service = this
                        };
                        result(new Tuple<Contact, Contact.ID>(userContact, userContactId));
                        break;
                    default:
                        result(new Tuple<Contact, Contact.ID>(null, null));
                        break;
                }
            });
        }

        private void SetResultAsChat(IChat chat, string id, Action<Tuple<Contact, Contact.ID>> result)
        {
            var contact = new TelegramContact
            {
                FirstName = TelegramUtils.GetChatTitle(chat)
            };
            var contactId = new Contact.PartyID
            {
                ExtendedParty = chat is Channel,
                Id = id,
                Service = this
            };
            result(new Tuple<Contact, Contact.ID>(contact, contactId));
        }

        private IUpdates JoinChat(string linkUsefulPart)
        {
            using (var client = new FullClientDisposable(this))
            {
                try
                {
                    var result = TelegramUtils.RunSynchronously(client.Client.Methods.MessagesImportChatInviteAsync(new MessagesImportChatInviteArgs
                    {
                        Hash = linkUsefulPart
                    }));
                    SendToResponseDispatcher(result, client.Client);
                    return result;
                }
                catch (Exception e)
                {
                    Utils.DebugPrint("Exception e " + e);
                    return null;
                }
            }
        }

        private bool CheckLink(string linkUsefulPart, out IChat alreadyJoinedChat)
        {
            using (var client = new FullClientDisposable(this))
            {
                try
                {
                    var result = TelegramUtils.RunSynchronously(client.Client.Methods.MessagesCheckChatInviteAsync(new MessagesCheckChatInviteArgs
                    {
                        Hash = linkUsefulPart
                    }));
                    var chatInvite = result as ChatInvite;
                    var chatInviteAlready = result as ChatInviteAlready;
                    if (chatInvite != null)
                    {
                        alreadyJoinedChat = null;
                        return true;
                    }
                    if (chatInviteAlready != null)
                    {
                        alreadyJoinedChat = chatInviteAlready.Chat;
                        return true;
                    }
                    alreadyJoinedChat = null;
                    return false;
                }
                catch (Exception ex)
                {
                    DebugPrint("Exception while checking invite" + ex);
                    alreadyJoinedChat = null;
                    return false;
                }
            }
        }

        private LinkType GetLinkType(string link, out string linkUsefulPart)
        {
            Uri uri = null;
            try
            {
                uri = new Uri(link);
            }
            catch (Exception e)
            {
                DebugPrint("Invalid uri" + e);
                linkUsefulPart = null;
                return LinkType.Invalid;
            }
            if (uri != null)
            {
                if (uri.Host != "telegram.me" && 
                    uri.Host != "telegram.dog" &&  //yo dog i dunno why this shit aint a domain til now
                    uri.Host != "t.me") 
                {
                    linkUsefulPart = null;
                    return LinkType.Invalid;
                }
                var path = uri.LocalPath;
                if (path.StartsWith("/joinchat"))
                {
                    path = path.Replace("/joinchat/", "");
                    linkUsefulPart = path;
                    return LinkType.PrivateGroup;
                }
                else
                {
                    path = path.Replace("/", "");
                    var resolvedPeer = ResolvePeer(path);
                    if (resolvedPeer?.Peer != null)
                    {
                        //this can only be a channel or a user, cannot be a chat
                        _dialogs.AddUsers(resolvedPeer.Users);
                        _dialogs.AddChats(resolvedPeer.Chats);
                        var peerUser = resolvedPeer.Peer as PeerUser;
                        if (peerUser != null)
                        {
                            linkUsefulPart = peerUser.UserId.ToString();                 
                            return LinkType.PublicUser;    
                        }
                        var peerChannel = resolvedPeer.Peer as PeerChannel;
                        if (peerChannel != null)
                        {
                            linkUsefulPart = peerChannel.ChannelId.ToString();
                            return LinkType.PublicGroup;
                        }
                    }
                }
            }
            linkUsefulPart = null;
            return LinkType.Invalid;
        }

        private ContactsResolvedPeer ResolvePeer(string path)
        {
            using (var client = new FullClientDisposable(this))
            {
                try
                {
                    var result = (ContactsResolvedPeer)TelegramUtils.RunSynchronously(client.Client.Methods.ContactsResolveUsernameAsync(new ContactsResolveUsernameArgs
                    {
                        Username = path
                    }));
                    return result;
                }
                catch (Exception e)
                {
                    DebugPrint("Exception while resolving peer" + e);
                    return null;
                }
            }
        }
    }
}

