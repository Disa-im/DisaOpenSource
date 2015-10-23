using System;
using Disa.Framework.Bubbles;
using SQLite;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Disa.Framework
{
    public static class BubbleQueueManager
    {
        private static readonly object _dbLock = new object();
        private static readonly object _sendLock = new object();
        private const string FileName = "QueuedBubbles.db";
        private const string NotQueuedFileName = "NotQueuedBubbles.db";
        private static int _resendInterval = 5;
        private static WakeLockBalancer.CruelWakeLock _resend;
        private static string[] _resendServices;

        private static string Location
        {
            get
            {
                var databasePath = Platform.GetDatabasePath();
                var queuedBubblesLocation = Path.Combine(databasePath, FileName);

                return queuedBubblesLocation;
            }
        }

        private static string NotQueuedLocation
        {
            get
            {
                var databasePath = Platform.GetDatabasePath();
                var queuedBubblesLocation = Path.Combine(databasePath, NotQueuedFileName);

                return queuedBubblesLocation;
            }
        }

        private static bool HasNotQueued(BubbleGroup group)
        {
            lock (_dbLock)
            {
                var groupId = group.ID;
                using (var db = new SqlDatabase<NotQueuedEntry>(NotQueuedLocation))
                {
                    // don't add a duplicate group into the queue
                    foreach (var possibleGroup in db.Store.Where(x => 
                        x.BubbleGroupGuid == groupId))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private static void NotQueuedSanityCheckup()
        {
            lock (_dbLock)
            {
                using (var db = new SqlDatabase<NotQueuedEntry>(NotQueuedLocation))
                {
                    const int secondsInWeek = 604800;
                    var nowTime = Time.GetNowUnixTimestamp();
                    var truncateTime = nowTime - secondsInWeek;
                    var removing = false;
                    var bubblesToTruncate = new List<NotQueuedEntry>();
                    foreach (var possibleBubble in db.Store.Where(x => x.Time < truncateTime))
                    {
                        if (!removing)
                        {
                            Utils.DebugPrint("Pruning not queued bubbles database. Some bubbles are too old!");
                        }
                        removing = true;
                        bubblesToTruncate.Add(possibleBubble);
                    }
                    foreach (var possibleBubble in bubblesToTruncate)
                    {
                        db.Remove(possibleBubble);
                    }
                }
            }
        }

        private static void SanityCheckup()
        {
            lock (_dbLock)
            {
                using (var db = new SqlDatabase<Entry>(Location))
                {
                    const int secondsInWeek = 604800;
                    var nowTime = Time.GetNowUnixTimestamp();
                    var truncateTime = nowTime - secondsInWeek;
                    var removing = false;
                    var bubblesToTruncate = new List<Entry>();
                    foreach (var possibleBubble in db.Store.Where(x => x.Time < truncateTime))
                    {
                        if (!removing)
                        {
                            Utils.DebugPrint("Pruning queued bubbles database. Some bubbles are too old!");
                        }
                        removing = true;
                        bubblesToTruncate.Add(possibleBubble);
                    }
                    foreach (var possibleBubble in bubblesToTruncate)
                    {
                        db.Remove(possibleBubble);
                    }
                }
            }
        }

        public static void SetNotQueuedToFailures(Service service)
        {
            if (service is UnifiedService)
                return;

            if (service.QueuedBubblesParameters == null 
                || service.QueuedBubblesParameters.BubblesNotToQueue == null
                || !service.QueuedBubblesParameters.BubblesNotToQueue.Any())
                return;

            foreach (var group in BubbleGroupManager.FindAll(service))
            {
                SetNotQueuedToFailures(@group);
            }
        }

        public static void SetNotQueuedToFailures(BubbleGroup group)
        {
            var nowUnixTimeStamp = Time.GetNowUnixTimestamp();

            var groups = BubbleGroupManager.GetInner(@group).Where(x => 
                    (x.Service.QueuedBubblesParameters != null && 
                    x.Service.QueuedBubblesParameters.BubblesNotToQueue != null && 
                    x.Service.QueuedBubblesParameters.BubblesNotToQueue.Any()));

            if (!groups.Any())
                return;

            foreach (var innerGroup in groups)
            {
                if (HasNotQueued(innerGroup))
                {
                    if (innerGroup.PartiallyLoaded)
                    {
                        BubbleGroupFactory.LoadFullyIfNeeded(innerGroup);
                    }

                    var bubblesSetToFailed = new List<VisualBubble>();

                    foreach (var bubble in innerGroup)
                    {                        
                        if (bubble.Direction == Bubble.BubbleDirection.Outgoing &&
                            bubble.Status == Bubble.BubbleStatus.Waiting)
                        {
                            if (innerGroup
                                .Service.QueuedBubblesParameters.BubblesNotToQueue
                                .FirstOrDefault(x => x == bubble.GetType()) != null)
                            {
                                var failBubble = innerGroup
                                    .Service.QueuedBubblesParameters.SendingBubblesToFailOnServiceStart
                                    .FirstOrDefault(x => x == bubble.GetType()) != null;

                                // Older than 10 minutes, fail it regardless.
                                if (!failBubble && bubble.Time < (nowUnixTimeStamp - 600))
                                {
                                    failBubble = true;
                                }

                                if (failBubble)
                                {
                                    RemoveNotQueued(bubble);
                                    bubble.Status = Bubble.BubbleStatus.Failed;
                                    bubblesSetToFailed.Add(bubble);
                                }
                            }
                        }
                    }

                    if (bubblesSetToFailed.Any())
                    {
                        BubbleGroupDatabase.UpdateBubble(innerGroup, bubblesSetToFailed.ToArray(), 100);
                        foreach (var bubble in bubblesSetToFailed)
                        {
                            BubbleGroupEvents.RaiseBubbleFailed(bubble, innerGroup);
                        }
                        BubbleGroupEvents.RaiseBubblesUpdated(innerGroup);
                        BubbleGroupEvents.RaiseRefreshed(innerGroup);
                    }
                }
            }

            NotQueuedSanityCheckup();
        }

        private static NotQueuedEntry AddNotQueued(VisualBubble bubble, BubbleGroup group)
        {
            lock (_dbLock)
            {
                var bubbleId = bubble.ID;
                using (var db = new SqlDatabase<NotQueuedEntry>(NotQueuedLocation))
                {
                    // don't add a duplicate group into the queue
                    foreach (var possibleGroup in db.Store.Where(x => x.Guid == bubbleId))
                    {
                        return possibleGroup;
                    }
                    var entry = new NotQueuedEntry
                    {
                        ServiceName = group.Service.Information.ServiceName,
                        BubbleGroupGuid = group.ID,
                        Guid = bubble.ID,
                        Time = bubble.Time,
                    };
                    db.Add(entry);
                    return entry;
                }
            }
        }

        private static void RemoveNotQueued(VisualBubble bubble)
        {
            lock (_dbLock)
            {
                var bubbleId = bubble.ID;
                using (var db = new SqlDatabase<NotQueuedEntry>(NotQueuedLocation))
                {
                    // don't add a duplicate group into the queue
                    foreach (var possibleGroup in db.Store.Where(x => x.Guid == bubbleId))
                    {
                        db.Remove(possibleGroup);
                    }
                }
            }
        }

        private static Entry Add(BubbleGroup group, VisualBubble bubble)
        {
            lock (_dbLock)
            {
                using (var db = new SqlDatabase<Entry>(Location))
                {
                    // don't add a duplicate bubble in the queue
                    foreach (var possibleBubble in db.Store)
                    {
                        if (possibleBubble.Guid == bubble.ID)
                            return possibleBubble;
                    }
                    var entry = new Entry
                    {
                        ServiceName = bubble.Service.Information.ServiceName,
                        Guid = bubble.ID,
                        Time = bubble.Time,
                        BubbleGroupGuid = group.ID,
                    };
                    db.Add(entry);
                    return entry;
                }
            }
        }

        private static void Remove(Entry entry)
        {
            lock (_dbLock)
            {
                using (var db = new SqlDatabase<Entry>(Location))
                {
                    db.Remove(entry);
                }
            }
        }

        private static void Remove(Entry[] entries)
        {
            lock (_dbLock)
            {
                using (var db = new SqlDatabase<Entry>(Location))
                {
                    foreach (var entry in entries)
                    {
                        db.Remove(entry);
                    }
                }
            }
        }

        public static Task PurgeBubble(string bubbleId)
        {
            return Task.Factory.StartNew(() =>
            {
                lock (_dbLock)
                {
                    using (var db = new SqlDatabase<Entry>(Location))
                    {
                        foreach (var entry in db.Store.Where(x => x.Guid == bubbleId))
                        {
                            db.Remove(entry);
                        }
                        var sending = InsertBubble.Sending.FirstOrDefault(x => x.ID == bubbleId);
                        if (sending != null)
                        {
                            InsertBubble.Sending.Remove(sending);
                        }
                    }
                }
            });
        }

        public static Task PurgeNotQueuedBubble(string bubbleId)
        {
            return Task.Factory.StartNew(() =>
            {
                lock (_dbLock)
                {
                    using (var db = new SqlDatabase<NotQueuedEntry>(NotQueuedLocation))
                    {
                        foreach (var entry in db.Store.Where(x => x.Guid == bubbleId))
                        {
                            db.Remove(entry);
                        }
                    }
                }
            });
        }

        public static bool HasQueuedBubbles(string serviceName, bool hourLimitation = false, 
            bool removeCurrentlySendingBubbles = false)
        {
            return QueuedBubblesIterator(serviceName, hourLimitation, removeCurrentlySendingBubbles).FirstOrDefault() != null;
        }

        private static IEnumerable<bool?> QueuedBubblesIterator(string serviceName, bool hourLimitation = false, 
            bool removeCurrentlySendingBubbles = false)
        {
            lock (_dbLock)
            {
                var currentTime = Time.GetNowUnixTimestamp();
                const int SecondsInHour = 3600;
                using (var db = new SqlDatabase<Entry>(Location))
                {
                    foreach (var possibleBubble in db.Store.Where(x => serviceName == x.ServiceName))
                    {
                        if (hourLimitation && possibleBubble.Time <= currentTime - SecondsInHour)
                        {
                            continue;
                        }

                        foreach (var bubbleGroup in BubbleGroupManager.BubbleGroupsNonUnified)
                        {
                            if (bubbleGroup.PartiallyLoaded)
                            {
                                continue;
                            }

                            foreach (var bubble in bubbleGroup)
                            {
                                if (bubble.Status != Bubble.BubbleStatus.Waiting)
                                    continue;

                                if (removeCurrentlySendingBubbles && InsertBubble.IsSending(bubble.ID))
                                {
                                    continue;
                                }

                                if (possibleBubble.Guid == bubble.ID)
                                {
                                    yield return true;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static object _scheduleLock = new object();

        private static void ScheduleReSend(string[] serviceNames, bool scheduled = false)
        {
            lock (_scheduleLock)
            {
                var hashSet = new HashSet<string>();
                foreach (var serviceName in serviceNames)
                {
                    hashSet.Add(serviceName);
                }
                if (_resendServices != null)
                {
                    foreach (var serviceName in _resendServices)
                    {
                        hashSet.Add(serviceName);
                    }
                }
                _resendServices = hashSet.ToArray();
                if (!scheduled)
                {
                    _resendInterval = 10;
                }
            }

            if (_resend != null)
            {
                Platform.RemoveAction(_resend);
            }

            Utils.DebugPrint("Scheduling resend action in " + _resendInterval + " seconds");
            _resend = new WakeLockBalancer.CruelWakeLock(new WakeLockBalancer.ActionObject(() =>
            {
                Utils.DebugPrint("Executing scheduled resend!");
                Task<List<Receipt>> sendTask;
                lock (_scheduleLock)
                {
                    sendTask = Send(_resendServices, true);
                }
                sendTask.Wait();
                lock (_scheduleLock)
                {
                    var receipts = sendTask.Result;
                    if (receipts.FirstOrDefault(x => !x.Success) != null)
                    {
                        _resendInterval *= 2;
                        if (_resendInterval > 600)
                        {
                            Utils.DebugPrint("Need to wait to long to resend. Killing resend scheduling!");
                            _resendInterval = 10;
                            _resendServices = null;
                            return;
                        }
                        Utils.DebugPrint("Some bubbles failed to send. Rescheduling.");
                        ScheduleReSend(_resendServices, true);
                    }
                    else
                    {
                        Utils.DebugPrint("Succeeded in sending all the queued bubbles!");
                        _resendInterval = 10;
                        _resendServices = null;
                    }
                }
            }, WakeLockBalancer.ActionObject.ExecuteType.TaskWithWakeLock), _resendInterval, 5, false);
            Platform.ScheduleAction(_resend);
        }
            
        public static Task<List<Receipt>> Send(string[] serviceNames, bool scheduled = false)
        {
            return Task<List<Receipt>>.Factory.StartNew(() =>
            {
                //PROCESS:  1) find all bubbles under serviceNames in db
                //          2) relate db entries to bubbles loaded into memory by Disa
                //          3) parallel send bubbles based respective to service
                //          4) delete all successful bubbles out of db

                lock (_sendLock)
                {
                    var nowUnixTimestamp = Time.GetNowUnixTimestamp();

                    var possibleBubblesFromDatabase = new List<Entry>();

                    lock (_dbLock)
                    {
                        using (var db = new SqlDatabase<Entry>(Location))
                        {
                            foreach (var possibleBubble in db.Store.Where(x => serviceNames.Contains(x.ServiceName)).Reverse())
                            {
                                if (!InsertBubble.IsSending(possibleBubble.Guid))
                                {
                                    possibleBubblesFromDatabase.Add(possibleBubble);
                                }
                            }
                        }
                    }

                    var possibleBubblesInDisa = new List<Tuple<Entry, VisualBubble>>();
                    foreach (var bubbleGroup in BubbleGroupManager.BubbleGroupsNonUnified)
                    {
                        var hasPossibleBubble = possibleBubblesFromDatabase.FirstOrDefault(x => 
                            x.BubbleGroupGuid != null && x.BubbleGroupGuid == bubbleGroup.ID) != null;
                        if (!hasPossibleBubble)
                        {
                            continue;
                        }

                        if (bubbleGroup.PartiallyLoaded)
                        {
                            BubbleGroupFactory.LoadFullyIfNeeded(bubbleGroup);
                        }

                        var bubblesToSetToFail = new List<Tuple<Entry, VisualBubble>>();
                        foreach (var bubble in bubbleGroup)
                        {
                            if (bubble.Status != Bubble.BubbleStatus.Waiting)
                                continue;

                            if (bubble.Direction != Bubble.BubbleDirection.Outgoing)
                                continue;

                            var possibleBubbleFromDatabase = possibleBubblesFromDatabase.FirstOrDefault(x => x.Guid == bubble.ID);
                            if (possibleBubbleFromDatabase != null)
                            {
                                // older than 10 minutes, fail
                                if (bubble.Time < (nowUnixTimestamp - 600))
                                {
                                    bubble.Status = Bubble.BubbleStatus.Failed;
                                    bubblesToSetToFail.Add(Tuple.Create(possibleBubbleFromDatabase, bubble));
                                    continue;
                                }

                                possibleBubblesInDisa.Add(new Tuple<Entry, VisualBubble>(possibleBubbleFromDatabase, bubble));
                            }
                        }
                        if (bubblesToSetToFail.Any())
                        {
                            BubbleGroupDatabase.UpdateBubble(bubbleGroup, 
                                bubblesToSetToFail.Select(x => x.Item2).ToArray(), 100);
                            Remove(bubblesToSetToFail.Select(x => x.Item1).ToArray());
                            foreach (var bubble in bubblesToSetToFail)
                            {
                                BubbleGroupEvents.RaiseBubbleFailed(bubble.Item2, bubbleGroup);
                            }
                            BubbleGroupEvents.RaiseBubblesUpdated(bubbleGroup);
                            BubbleGroupEvents.RaiseRefreshed(bubbleGroup);
                        }
                    }

                    var sent = new List<Tuple<Entry, bool>>();

                    var possibleBubblesInDisaByService = possibleBubblesInDisa.GroupBy(x => x.Item1.ServiceName);
                    Parallel.ForEach(possibleBubblesInDisaByService, possibleBubblesInService =>
                    {
                        var failed = false;
                        foreach (var possibleBubble in possibleBubblesInService)
                        {
                            if (failed)
                            {
                                sent.Add(new Tuple<Entry, bool>(possibleBubble.Item1, false));
                                continue;
                            }

                            Utils.DebugPrint(">>>>>>>>>>> Sending queued bubble on " 
                                + possibleBubble.Item2.Service.Information.ServiceName + "!");

                            var sendBubbleTask = BubbleManager.Send(possibleBubble.Item2, true);
                            sendBubbleTask.Wait();
                            if (sendBubbleTask.Result)
                            {
                                Utils.DebugPrint(">>>>>>>>> Successfully sent queued bubble on " +
                                    possibleBubble.Item2.Service.Information.ServiceName + "!");
                                sent.Add(new Tuple<Entry, bool>(possibleBubble.Item1, true));
                                lock (_dbLock)
                                {
                                    using (var db = new SqlDatabase<Entry>(Location))
                                    {
                                        db.Remove(possibleBubble.Item1);
                                    }
                                }
                                Utils.Delay(100).Wait();
                            }
                            else
                            {
                                Utils.DebugPrint(">>>>>>>>> Failed to send queued bubble on " +
                                    possibleBubble.Item2.Service.Information.ServiceName + "!");
                                sent.Add(new Tuple<Entry, bool>(possibleBubble.Item1, false));
                                failed = true; // fail the entire chain for this service from here on out so messages aren't sent out of order
                            }
                        }
                    });

                    var receipts = sent.Select(x => new Receipt(x.Item1.Guid, x.Item2)).ToList();

                    if (!scheduled)
                    {
                        if (receipts.FirstOrDefault(x => !x.Success) != null)
                        {
                            ScheduleReSend(serviceNames);
                        }
                        else
                        {
                            var needToSendAgain = serviceNames.Where(x => BubbleQueueManager.HasQueuedBubbles(x, true, true)).ToArray();
                            if (needToSendAgain.Any())
                            {
                                Send(needToSendAgain);
                            }
                        }
                    }

                    SanityCheckup();

                    return receipts;
                }
            });
        }

        public static Task JustQueue(BubbleGroup group, VisualBubble visualBubble)
        {
            return Task.Factory.StartNew(() =>
            {
                var entry = Add(group, visualBubble);
            });
        }

        public class Receipt
        {
            public string BubbleId { get; private set; }
            public bool Success { get; private set; }

            public Receipt(string bubbleId, bool success)
            {
                BubbleId = bubbleId;
                Success = success;
            }
        }

        private class Entry
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string ServiceName { get; set; }
            public string Guid { get; set; }
            public long Time { get; set; } 
            public string BubbleGroupGuid { get; set; }
        }

        private class NotQueuedEntry
        {
            [PrimaryKey, AutoIncrement] 
            public int Id { get; set; }

            public string ServiceName { get; set; }
            public string Guid { get; set; }
            public long Time { get; set; } 
            public string BubbleGroupGuid { get; set; }
        }
            
        public class InsertBubble : IDisposable
        {
            // This list is to prevent the possibility of the QueueManager sending the same bubble twice. Therefore, it is only applicable
            // to bubbles being queued, and not the ones not queued (a.k.a those inserted with NotQueuedEntry).
            public static readonly List<VisualBubble> Sending = new List<VisualBubble>();
            private readonly Entry _entry;
            private bool _cancelQueue;
            private readonly bool _shouldInsert;
            private readonly VisualBubble _visualBubble;

            public InsertBubble(BubbleGroup group, VisualBubble visualBubble, bool shouldInsert)
            {
                _shouldInsert = shouldInsert;
                _visualBubble = visualBubble;
                if (_shouldInsert)
                {
                    _entry = Add(group, _visualBubble);
                    lock (Sending)
                    {
                        Sending.Add(_visualBubble);
                    }
                }
                else
                {
                    AddNotQueued(visualBubble, group);
                    _entry = null;
                }
            }

            public static bool IsSending(string bubbleGuid)
            {
                lock (Sending)
                {
                    return Sending.FirstOrDefault(x => x.ID == bubbleGuid) != null;
                }
            }

            public void CancelQueue()
            {
                _cancelQueue = true;
            }

            public void Dispose()
            {
                if (_shouldInsert)
                {
                    if (_cancelQueue)
                    {
                        Remove(_entry);
                    }
                    else
                    {
                        ScheduleReSend(new [] { _entry.ServiceName });
                    }
                    lock (Sending)
                    {
                        Sending.Remove(_visualBubble);
                    }
                }
                else if (!_shouldInsert && _cancelQueue)
                {
                    RemoveNotQueued(_visualBubble);
                }
            }
        }
    }
}

