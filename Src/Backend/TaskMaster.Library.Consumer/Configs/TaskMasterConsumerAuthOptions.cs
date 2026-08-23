namespace TaskMaster.Library.Consumer.Configs
{
    public class TaskMasterConsumerAuthOptions
    {
        public TaskMasterConsumerOidcAuthOptions? Oidc { get; set; }

        public static TaskMasterConsumerAuthOptions Empty => new TaskMasterConsumerAuthOptions();
    }
}
