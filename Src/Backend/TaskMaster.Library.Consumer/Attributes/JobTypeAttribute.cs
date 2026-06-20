using CommonAttributes = TaskMaster.Library.Common.Attributes;

namespace TaskMaster.Library.Consumer.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class JobTypeAttribute : CommonAttributes.JobTypeAttribute
    {
        public JobTypeAttribute(string name, long version) : base(name, version) { }
    }
}
