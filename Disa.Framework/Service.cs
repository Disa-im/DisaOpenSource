using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    public abstract class Service
    {
        internal Thread ReceivingBubblesThread = null;

        internal object HasSmartStart;

        internal readonly object SendBubbleLock = new object();

        public DisaServiceUserSettings UserSettings { get; internal set; }

		public abstract bool Initialize(DisaSettings settings);

        public abstract bool InitializeDefault();
        
        public abstract bool Authenticate(WakeLock wakeLock);

        public abstract void Deauthenticate();

        public abstract void Connect(WakeLock wakeLock);

        public abstract void Disconnect();

        public abstract string GetIcon(bool large);

        public abstract IEnumerable<Bubble> ProcessBubbles();

        public abstract void SendBubble(Bubble b);

        public abstract bool BubbleGroupComparer(string first, string second);

        public abstract Task GetBubbleGroupLegibleId(BubbleGroup group, Action<string> result);

        public abstract Task GetBubbleGroupName(BubbleGroup group, Action<string> result);

        public abstract Task GetBubbleGroupPhoto(BubbleGroup group, Action<DisaThumbnail> result);

        public abstract Task GetBubbleGroupPartyParticipants(BubbleGroup group, Action<DisaParticipant[]> result);

        public abstract Task GetBubbleGroupUnknownPartyParticipant(BubbleGroup group, string unknownPartyParticipant, Action<DisaParticipant> result);

        public abstract Task GetBubbleGroupPartyParticipantPhoto(DisaParticipant participant, Action<DisaThumbnail> result);

        public abstract Task GetBubbleGroupLastOnline(BubbleGroup group, Action<long> result);

        public virtual void RefreshPhoneBookContacts()
        {
            Utils.DebugPrint("Refresh contacts not implemented.... but the service has it enabled.");
        }

		public virtual Task NewBubbleGroupCreated(BubbleGroup group)
		{
            return Task.Factory.StartNew(() =>
            {
                Utils.DebugPrint("New bubble group created not overidden.");
            });
		}

        public virtual Task OpenedBubbleGroup(BubbleGroup group)
        {
            return Task.FromResult(0);
        }

        public virtual Task GetQuotedMessageTitle(VisualBubble bubble, Action<string> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(null);
            });
        }

        public void EventBubble(Bubble b)
        {
            if (b != null)
            {
                ServiceManager.OnBubbleReceived(b);
            }
            else 
            {
                DebugPrint(Information.ServiceName + " is trying to 'event' a null bubble. Doing nothing...");
            }
        }

        protected Service()
        {
            Guid = System.Guid.NewGuid().ToString();
            Information = GetParameter<ServiceInfo>();
            VideoParameters = GetParameter<VideoParameters>();
            AudioParameters = GetParameter<AudioParameters>();
            FileParameters = GetParameter<FileParameters>();
            QueuedBubblesParameters = GetParameter<QueuedBubblesParameters>();
        }

        private T GetParameter<T>()
        {
            foreach (var param in GetType().GetCustomAttributes(true).OfType<T>())
            {
                return param;
            }
            return default(T);
        }

        public string Guid { get; internal set; }

        public ServiceInfo Information { get; private set; }

        public VideoParameters VideoParameters { get; private set; }

        public AudioParameters AudioParameters { get; private set; }

        public FileParameters FileParameters { get; private set; }

        public QueuedBubblesParameters QueuedBubblesParameters { get; private set; }

        public void DebugPrint(string str)
        {
            Utils.DebugPrint(Information.ServiceName + ": " + str);
        }
    }

    [ServiceInfo(null, 
        true, false, false, false, false, typeof(DisaSettings), 
        ServiceInfo.ProcedureType.ConnectAuthenticate)]
    public class UnifiedService : Service
    {
        public static string Name = "Unified";

        public UnifiedService()
        {
            Information.SetServiceName(Name);
        }

        public void SetServiceName(string name)
        {
            Name = name;
            Information.SetServiceName(Name);
        }

        public override bool Initialize(DisaSettings settings)
        {
            return true;
        }

        public override bool InitializeDefault()
        {
            return true;
        }

        public override bool Authenticate(WakeLock wakeLock)
        {
            throw new NotImplementedException();
        }

        public override void Deauthenticate()
        {
            throw new NotImplementedException();
        }

        public override void Connect(WakeLock wakeLock)
        {
            throw new NotImplementedException();
        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        public override string GetIcon(bool large)
        {
            return large ? GreyIcon : Icon;
        }

        public override IEnumerable<Bubble> ProcessBubbles()
        {
            throw new NotImplementedException();
        }

        public override void SendBubble(Bubble b)
        {
            throw new NotImplementedException();
        }

        public override bool BubbleGroupComparer(string first, string second)
        {
            return first == second;
        }

        public override Task GetBubbleGroupLegibleId(BubbleGroup @group, Action<string> result)
        {
            return Task.Factory.StartNew(() => result(Name));
        }

        public override Task GetBubbleGroupName(BubbleGroup @group, Action<string> result)
        {
            return Task.Factory.StartNew(() => result(Name));
        }

        public override Task GetBubbleGroupPhoto(BubbleGroup @group, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() => result(null));
        }

        public override Task GetBubbleGroupPartyParticipants(BubbleGroup @group, Action<DisaParticipant[]> result)
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

        public override Task GetBubbleGroupLastOnline(BubbleGroup @group, Action<long> result)
        {
            return Task.Factory.StartNew(() => result(0));
        }

        private const string Icon =
            "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/GMzLdsAAAAgY0hSTQAAeiUAAICDAAD5/wAAgOkAAHUwAADqYAAAOpgAABdvkl/FRgAAFHRJREFUeAEAZBSb6wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wAA////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8AAP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AAD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wAA////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8AAP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AAD///8A////AP///wD///8AS8X0a0nE9ONHxvTfSMXz3EPL+lkE+/8AAP//AAD//wAA//8AAP//AAD//wAA//8AAP//AICAgAD/AAAA/wAAAP8AAAD/AAAA/wAAAP8AAAD/AAAA/wADAPAQRUvqFlXK6xVS6OoWVunvEEJu////AP///wD///8A////AP///wAA////AP///wD///8A////ADXW+AY70PaXSMX0/0fG9P9Gx/T8PM/1Xhjr/AAA//8AAP//AAD//wAA//8AAP//AAD//wCAgIAA/wAAAP8AAAD/AAAA/wAAAP8AAAD/AAAA/wACAPMNNEzrFVP46xVT/+sWVf/uEkqg/AMOB////wD///8A////AP///wD///8AAP///wD///8A////AP///wAA//8AGO3+AE/C9I1IxfT/R8b0/0zC8/0y1vheD/T+AAD//wAA//8AAP//AAD//wAA//8AgICAAP8AAAD/AAAA/wAAAP8AAAD/AAAA/gEDAPQMN0fqFlX16xVT/+sVVP/tE06e/AITAf8AAAD///8A////AP///wD///8A////AAD///8A////AP///wD///8AAP//AAr4/wAf5/sAPM/3jUvC8/9HxvT/TMLz/DbV+FsN9v4AAP//AAD//wAA//8AAP//AICAgAD/AAAA/wAAAP8AAAD/AAAA/wABAPEOOUPrFlTy6xVT/+sVU//sFFGi/AMUAv8AAAD/AAAA////AP///wD///8A////AP///wAA////AP///wD///8A////AAD//wAA//8AAP//ACbh+wBAzPWLR8b0/0fG9P9LwvP7N9T4Vwj4/gAA//8AAP//AAD//wCAgIAA/wAAAP8AAAD/AAAA/wAAAPINPEDqFVLw6xVT/+oWVP/sFFWi+wQZA/8AAQD/AAAA/wAAAP///wD///8A////AP///wD///8AAP///wD///8A////AP///wAA//8AAP//AAD//wAA//8AEfL+AEvD9IpHxvT/R8b0/0jF8/k9z/dYBP3/AAD//wAA//8AgX9+AP8AAAD/AAAA/wAAAPQLNT/rFVPv6xVT/+oWVv/uEkuk+QYcAv8AAgD/AAAA/wAAAP8AAAD///8A////AP///wD///8A////AAD///8A////AP///wD///8AAP//AAD//wAA//8AAP//AAT8/wAU7/wAQsr3kkvD8/9HxvT/Rsf0+D3O9lEZ6v4AAP//AHCPqAD/ADYA/wA7APQLOTbrFVLs6xVT/+oWVv/uEkms9wglBv8AAAD/AAAA/wAAAP8AAAD/AAAA////AP///wD///8A////AP///wAA////AP///wD///8A////AAD//wAA//8AAP//AAD//wAA//8AAP//ACfg+wA8z/aSScX0/0fG9P9NwfP4LNv6UhDz/wB1jJ0A/wAWAPYJQzDrFVno6hZT/+oWU//sE0qu9wgkB/8AAAD/AAAA/wAAAP8AAAD/AAAA/wAAAP///wD///8A////AP///wD///8AAP///wD///8A////AP///wAA//8AAP//AAD//wAA//8AAP//AAD//wAA//8AH+f8AEzD9JNHxvT/R8b0/0jF9Pk/0P9WlXSQAPwCFSzrFU/j6xVT/+oWUv/sFFay+AcpCP8ADgD/AA4A/wAOAP8ADgD/AA4A/wAOAP8ADgD///8A////AP///wD///8A////AAD///8A////AP///wD///8AAP//AAD//wAA//8AAP//AAD//wAA//8AAP//AAb6/gAT8v0AR833jUrC8/9HxvT/Pc/90Yt7sAD0DEqd6xVT/+oWU//sFFiz+QZZCv8AWQD/AFgA/wBZAP8AWQD/AFkA/wBZAP8AWQD/AFkA////AP///wD///8A////AP///wAA////AP///wD///8A////AAD//wAA//8AAP//AAD//wAA//8AAP//AAD//wAA//8AAP//ACrc/wAx0P+IPsb+2TfU/6yRgLcA+gJHgPAOUtj3CEes/wAxC/8AMQD/ADIA/wAyAP8AMgD/ADIA/wAyAP8AMgD/ADIA/wAyAP///wD///8A////AP///wD///8AAP///wD///8A////AP///wBC/70AQv+9AEL/vQBC/70AQv+9AEL/vQBC/70AQv+9AEL/vQBB/b4ATOnFAHXFvgB52MUAsqWZAM03VADNP18AtE4xAKVZAACpVgAAqVYAAKlWAACpVgAAqVYAAKlWAACpVgAAqVYAAKlWAAD///8A////AP///wD///8A////AAD///8A////AP///wD///8A//8AAP//AAD//wAA//8AAP//AAD//wAA//8AAP//AAD//wAA//oAAP/HGiv/xB1o/8QXUsLDPQBexHM/bMF0akXZTkYS9x0AAP8AAAD/AAAA/wAAAP8AAAD/AAAA/wAAAP8AAAD/AAAA/wAA////AP///wD///8A////AP///wAA////AP///wD///8A////AP//AAD//wAA//8AAP//AAD//wAA//8AAP//AAD//wAA/+kNAP/cEzT/xSLn/8Qj///DH9vCv0QAZb50q2y+bv9zunX+V8dYYQ33DgAB/gEAAP8AAAD/AAAA/wAAAP8AAAD/AAAA/wAAAP8AAP///wD///8A////AP///wD///8AAP///wD///8A////AP///wD//wAA//8AAP//AAD//wAA//8AAP//AAD//wAA//8AAP/PHDn/vyfp/8Qj///BJf//0BmNu9IvAFu2amBvvXH/bb5v/22+b/5kwGVkC/YLAAD/AAAA/wAAAP8AAAD/AAAA/wAAAP8AAAD/AAD///8A////AP///wD///8A////AAD///8A////AP///wD///8A//8AAP//AAD//wAA//8AAP//AAD//wAA//QCAP/OGjn/xiHs/8Qj///DI///yh+Y/+ERAZT7BAAO8RAATdBOdXK7dP9tvW//bb5v/lfIWWIM9gwAAP8AAAD/AAAA/wAAAP8AAAD/AAAA/wAA////AP///wD///8A////AP///wAA////AP///wD///8A////AP//AAD//wAA//8AAP//AAD//wAA//IHAP/cEjj/wCTr/8Qj///EI///xSKZ//MFAP//AAB8/wAAAP8AAAX9BABP0VCAb71x/2y+bv9wu3L9U8xUXhHyEQAA/wAAAP8AAAD/AAAA/wAAAP8AAP///wD///8A////AP///wD///8AAP///wD///8A////AP///wD//wAA//8AAP//AAD//wAA//wCAP/YFTP/vyfq/8Qj///CJP//xSCf//IHAf/9AQD//wAAku0SAB/ZJgAi3CIAN803AGy4bYttv3D/bL5u/3O5dfxK0UxXCPoIAAD/AAAA/wAAAP8AAAD/AAD///8A////AP///wD///8A////AAD///8A////AP///wD///8A//8AAP//AAD//wAA//8AAP/XFTD/wyTm/8Qj///BJP//yiCh/+sLA///AAD//wAA//8AAK7RLwBRnGMAXaJdAFqjWgBdqV0AbLZukW6+cP9svm7/c7p1+EfTSFEI+QgAAP8AAAD/AAAA/wAA////AP///wD///8A////AP///wAA////AP///wD///8A////AP//AAD//wAA//8AAP/WFzL/xCLn/8Mk///CJf//yCCm/+8KA///AAD//wAA//8AAP//AACO8g4AGOIdABvkGwAb5BsAGuMaAB/nHwBdx16Ycbtz/2y+bv9zuXX5RdRGUC3VLQAu0C4AL9AvAP///wD///8A////AP///wD///8AAP///wD///8A////AP///wD//wAA//8AAP/fFC//wyPl/8Mk///DJf//yCCq/+4KBf//AAD//wAA//8AAP//AAD//wAAfP8AAAD/AAAA/wAAAP8AAAD/AAAA/wAAEPURAVvIXKBvvXH/bL5u/3O6dfZks2VJKtIqACrVKgD///8A////AP///wD///8A////AAD///8A////AP///wD///8A//8AAP/cEzP/xCTq/8Mk///DJP//ySCw//EIB///AAD//wAA//8AAP//AAD//wAA//8AAI30DQAX4xwAGuUaABrlGgAa5RoAGuUaABjmGAAy1jIEcbZzqm2+cP9tvm//bb5v+DbfNkkA/wAA////AP///wD///8A////AP///wAA////AP///wD///8A////AP/eERn/wyO//8Mk5P/EJN7/wyKt/7UICf/BAAD/wAAA/8AAAP/AAAD/wAAA/8AAAP+9AACRzhIAH9olACPcIwAj3CMAI9wjACPcIwAj3CMAId0gACThJQdhxWOfbr1w5W29b+1uv3DNVr5WJP///wD///8A////AP///wD///8AAP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AAD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wAA////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8AAP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AAD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wAA////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8AAP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AAEAAP//BEBTGcIGcQwAAAAASUVORK5CYII=";

        public const string GreyIcon =
            "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";

    }
}

