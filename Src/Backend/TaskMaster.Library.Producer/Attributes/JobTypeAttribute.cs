namespace TaskMaster.Library.Producer.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class JobTypeAttribute : Attribute
    {
        public JobTypeAttribute(string name, long version)
        {
            Name = name;
            Version = version;
        }

        public string Name { get; }
        public long Version { get; }
    }
}
