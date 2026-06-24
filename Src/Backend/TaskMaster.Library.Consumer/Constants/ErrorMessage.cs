using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Library.Consumer.Constants
{
    internal class ErrorMessage
    {
        internal static string ConsumerServiceAlreadyInitialised() => $"Consumer service already initialised";
        internal static string ConsumerServiceNotInitialised() => $"Consumer services not initilised";
        internal static string HandlerNotRegistered(string name, long version) => $"No handler registered for job type '{name}' version '{version}'.";
        internal static string JsonParsingError(Guid jobId, string jobTypeName, long jobTypeVersion) => $"Error while parsing payload for job id: '{jobId}' job type '{jobTypeName}' version '{jobTypeVersion}'";
        internal static string HeartbeatFailed() => "Heartbeat failed for worker.";
        internal static string PayloadTypeMissingAttribute(string fullName) => $"Payload type '{fullName}' must be decorated with JobTypeAttribute.";

    }
}
