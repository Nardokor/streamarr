using System;
using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Jobs
{
    public class ScheduledTask : ModelBase
    {
        public string TypeName { get; set; }
        public int Interval { get; set; }
        public DateTime LastExecution { get; set; }
        public CommandPriority Priority { get; set; }
        public DateTime LastStartTime { get; set; }

        public ScheduledTask()
        {
            Priority = CommandPriority.Low;
        }
    }
}
