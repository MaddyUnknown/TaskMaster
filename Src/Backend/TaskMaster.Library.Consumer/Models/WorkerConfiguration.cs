using System.Reflection;
using TaskMaster.Library.Common.Models.JobType;
using TaskMaster.Library.Consumer.Attributes;
using TaskMaster.Library.Consumer.Constants;
using TaskMaster.Library.Consumer.Interfaces;

namespace TaskMaster.Library.Consumer.Models
{
    public class WorkerConfiguration
    {
        internal Dictionary<(string Name, long Version), HandlerEntry> HandlerMap { get; } = new();

        public void Handle<TPayload, THandler>()
            where TPayload : class
            where THandler : IJobHandler<TPayload>
        {
            var attr = typeof(TPayload).GetCustomAttribute<JobTypeAttribute>()
                ?? throw new InvalidOperationException(
                    ErrorMessage.PayloadTypeMissingAttribute(typeof(TPayload).FullName!));

            var key = (attr.Name, attr.Version);
            HandlerMap[key] = new HandlerEntry(typeof(TPayload), typeof(THandler));
        }

        internal IEnumerable<JobTypeRef> GetCapabilities() =>
            HandlerMap.Keys.Select(k => new JobTypeRef { Name = k.Name, Version = k.Version });
    }

    internal record HandlerEntry(Type PayloadType, Type HandlerType);
}
