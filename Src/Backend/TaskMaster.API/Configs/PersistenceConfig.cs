using TaskMaster.API.Enums;

namespace TaskMaster.API.Configs
{
    public class PersistenceConfig
    {
        public const string SectionName = "Database";

        public string Provider { get; set; } = nameof(DatabaseProviderEnum.SqlServer);
        public bool AutoMigrate { get; set; } = true;
    }
}
