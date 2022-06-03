namespace VerbIt.Backend.Models
{
    public class TableStorageSettings
    {
        public const string ConfigKey = nameof(TableStorageSettings);

        public string ConnectionString { get; set; } = null!;
        public string TablePrefix { get; set; } = "";
    }
}
