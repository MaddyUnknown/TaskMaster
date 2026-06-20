namespace TaskMaster.Library.Common.Attributes
{
    public class JobTypeAttribute : Attribute
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
