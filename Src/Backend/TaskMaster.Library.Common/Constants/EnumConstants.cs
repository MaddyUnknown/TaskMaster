using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Library.Common.Constants
{
    public static class EnumConstants
    {
        public static class ActionStatusEnum
        {
            public static readonly string Ok = "ok";
            public static readonly string Failed = "failed";
        }

        public static class JobStatusEnum
        {
            public static readonly string Queued = "queued";
            public static readonly string InProgress = "in-progress";
            public static readonly string Completed = "completed";
            public static readonly string Failed = "failed";
        }
    }
}
