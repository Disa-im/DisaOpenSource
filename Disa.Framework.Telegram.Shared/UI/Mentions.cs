using SharpMTProto;
using SharpTelegram.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disa.Framework.Bot;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IMentions
    {
        public Task GetUsernameMentionsToken(Action<string> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result("@");
            });
        }

        public Task GetBotCommandMentionsToken(Action<string> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(@"/");
            });
        }

        public Task GetHashtagMentionsToken(Action<string> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result("#");
            });
        }

        // TODO
        public Task GetRecentHashtags(Action<List<Hashtag>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(new List<Hashtag>());
            });
        }

        // TODO
        public Task SetRecentHashtags(List<Hashtag> hashtags,  Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(true);
            });
        }

        // TODO
        public Task ClearRecentHashtags(Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(true);
            });
        }

        // TODO
        public Task GetContactsByUsername(string username, Action<List<Contact>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var peer = ResolvePeer(username);

                var contacts = new List<Contact>();
                foreach (var peerUser in peer.Users)
                {
                    var user = peerUser as User;
                    if (user != null)
                    {
                        var contact = CreateTelegramContact(user);
                        contacts.Add(contact);
                    }
                }

                result(contacts);
            });
        }

        // TODO
        public Task GetInlineBotResults(BotContact bot, string query, string offset, Action<BotResults> botResults)
        {
            throw new NotImplementedException();
        }
    }
}

