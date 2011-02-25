#region

using System;
using System.Data;
using Game.Database;

#endregion

namespace Game.Logic
{
    public abstract class ScheduledPassiveAction : PassiveAction, ISchedule, IActionTime
    {
        protected DateTime beginTime = DateTime.MinValue;

        protected DateTime endTime = DateTime.MinValue;

        protected DateTime nextTime = DateTime.MinValue;

        protected ScheduledPassiveAction()
        {
        }

        protected ScheduledPassiveAction(uint id, DateTime beginTime, DateTime nextTime, DateTime endTime, bool isVisible)
        {
            ActionId = id;
            IsVisible = isVisible;
            this.beginTime = beginTime;
            this.nextTime = nextTime;
            this.endTime = endTime;
        }

        public override DbColumn[] DbColumns
        {
            get
            {
                return new[]
                       {
                               new DbColumn("object_id", WorkerObject.WorkerId, DbType.UInt32), new DbColumn("is_chain", IsChain, DbType.Boolean),
                               new DbColumn("is_scheduled", true, DbType.Boolean), new DbColumn("is_visible", IsVisible, DbType.Boolean),
                               new DbColumn("type", Type, DbType.UInt32), new DbColumn("begin_time", BeginTime, DbType.DateTime),
                               new DbColumn("end_time", EndTime, DbType.DateTime), new DbColumn("next_time", nextTime, DbType.DateTime),
                               new DbColumn("properties", Properties, DbType.String)
                       };
            }
        }

        #region IActionTime Members

        public DateTime BeginTime
        {
            get
            {
                return beginTime;
            }
            set
            {
                beginTime = value;
            }
        }

        public DateTime EndTime
        {
            get
            {
                return endTime;
            }
            set
            {
                if (IsScheduled)
                    throw new Exception("Trying to change scheduled time while action is in scheduler");
                endTime = value;
            }
        }

        public DateTime NextTime
        {
            get
            {
                return nextTime == DateTime.MinValue ? endTime : nextTime;
            }
            set
            {
                if (IsScheduled)
                    throw new Exception("Trying to change scheduled time while action is in scheduler");
                nextTime = value;
                // Cap the end time so it can never be less than the next time
                if (endTime < nextTime)
                    endTime = nextTime;
            }
        }

        #endregion

        #region ISchedule Members

        public bool IsScheduled { get; set; }

        public DateTime Time
        {
            get
            {
                return NextTime;
            }
        }

        public abstract void Callback(object custom);

        #endregion
    }
}