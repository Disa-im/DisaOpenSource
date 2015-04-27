using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public class WakeLockBalancer
    {
        public class CruelWakeLock : WakeLock
        {
            public CruelWakeLock(ActionObject action, int interval, int tolerance, bool reoccurring)
                : base(action, interval, tolerance, reoccurring)
            {
            }
        }

        public class GracefulWakeLock : WakeLock
        {
            public GracefulWakeLock(ActionObject action, int interval, int tolerance, bool reoccurring)
                : base(action, interval, tolerance, reoccurring)
            {
            }
        }

        public abstract class WakeLock
        {
            public ActionObject Action { get; private set; }
            public long Earliest { get; private set; }
            public long Latest { get; private set; }
            public int Interval { get; private set; }
            public int Tolerance { get; private set; }
            public bool Reoccurring { get; private set; }

            protected WakeLock(ActionObject action, int interval, int tolerance, bool reoccurring)
            {
                Action = action;
                Interval = interval;
                Tolerance = tolerance;
                Reoccurring = reoccurring;
                Update();
            }

            public void Update()
            {
                var absolute = Time.GetNowUnixTimestamp() + Interval;
                Earliest = absolute - Tolerance;
                Latest = absolute + Tolerance;
            }
        }

        public class ActionParamObject<T>
        {
            public Action<T> Action { get; private set; }
            public ActionObject.ExecuteType Execute { get; private set; }

            public ActionParamObject(Action<T> action, ActionObject.ExecuteType execute)
            {
                Action = action;
                Execute = execute;
            }
        }

        public class ActionObject
        {
            public enum ExecuteType
            {
                UiThread,
                TaskWithWakeLock
            }

            public Action Action { get; private set; }
            public ExecuteType Execute { get; private set; }

            public ActionObject(Action action, ExecuteType execute)
            {
                Action = action;
                Execute = execute;
            }
        }
    }
}

