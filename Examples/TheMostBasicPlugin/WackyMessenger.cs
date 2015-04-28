using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Disa.Framework.Bubbles;

namespace Disa.Framework.WackyMessenger
{
    [ServiceInfo("WackyMessenger", true, false, false, false, false, typeof(WackyMessengerSettings), 
        ServiceInfo.ProcedureType.ConnectAuthenticate, typeof(TextBubble))]
    public class WackyMessenger : Service, IVisualBubbleServiceId
    {
        private string _deviceId;
        private int _bubbleSendCount;

        public override bool Initialize(DisaSettings settings)
        {
            throw new NotImplementedException();
        }

        public override bool InitializeDefault()
        {
            _deviceId = Time.GetNowUnixTimestamp().ToString();
            return true;
        }

        public override bool Authenticate(WakeLock wakeLock)
        {
            return true;
        }

        public override void Deauthenticate()
        {
            // do nothing
        }

        public override void Connect(WakeLock wakeLock)
        {
            // do nothing
        }

        public override void Disconnect()
        {
            // do nothing
        }

        public override string GetIcon(bool large)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Bubble> ProcessBubbles()
        {
            throw new NotImplementedException();
        }

        private static string Reverse( string s )
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse( charArray );
            return new string( charArray );
        }

        public override void SendBubble(Bubble b)
        {
            var textBubble = b as TextBubble;
            if (textBubble != null)
            {
                Utils.Delay(2000).Wait();
                Platform.ScheduleAction(1, new WakeLockBalancer.ActionObject(() =>
                {
                    EventBubble(new TextBubble(Time.GetNowUnixTimestamp(), Bubble.BubbleDirection.Incoming,
                        textBubble.Address, null, false, this, Reverse(textBubble.Message)));
                }, WakeLockBalancer.ActionObject.ExecuteType.TaskWithWakeLock));
            }
        }

        public override bool BubbleGroupComparer(string first, string second)
        {
            return first == second;
        }

        public override Task GetBubbleGroupLegibleId(BubbleGroup group, Action<string> result)
        {
            throw new NotImplementedException();
        }

        public override Task GetBubbleGroupName(BubbleGroup group, Action<string> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(group.Address);
            });
        }

        public override Task GetBubbleGroupPhoto(BubbleGroup group, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(null);
            });
        }

        public override Task GetBubbleGroupPartyParticipants(BubbleGroup group, Action<DisaParticipant[]> result)
        {
            throw new NotImplementedException();
        }

        public override Task GetBubbleGroupUnknownPartyParticipant(BubbleGroup group, string unknownPartyParticipant, Action<DisaParticipant> result)
        {
            throw new NotImplementedException();
        }

        public override Task GetBubbleGroupPartyParticipantPhoto(DisaParticipant participant, Action<DisaThumbnail> result)
        {
            throw new NotImplementedException();
        }

        public override Task GetBubbleGroupLastOnline(BubbleGroup group, Action<long> result)
        {
            throw new NotImplementedException();
        }

        public void AddVisualBubbleIdServices(VisualBubble bubble)
        {
            bubble.IdService = _deviceId + ++_bubbleSendCount;
        }

        public bool DisctinctIncomingVisualBubbleIdServices()
        {
            return true;
        }
    }

    public class WackyMessengerSettings : DisaSettings
    {
        // store settings in here:
        // e.g: public string Username { get; set; }
    }
}

