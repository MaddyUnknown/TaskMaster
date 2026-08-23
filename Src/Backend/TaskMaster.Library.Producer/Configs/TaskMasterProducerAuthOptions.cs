namespace TaskMaster.Library.Producer.Configs
{
    public class TaskMasterProducerAuthOptions
    {
        public TaskMasterProducerOidcAuthOptions? Oidc { get; set; }

        public static TaskMasterProducerAuthOptions Empty => new TaskMasterProducerAuthOptions();
    }
}
