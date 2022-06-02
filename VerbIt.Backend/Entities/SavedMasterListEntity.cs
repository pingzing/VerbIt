using Azure;
using Azure.Data.Tables;
using VerbIt.DataModels;

namespace VerbIt.Backend.Entities
{
    public class SavedMasterListEntity : ITableEntity
    {
        public const string DefaultPartitionKey = "SavedMasterList";

        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string ListName { get; set; } = null!;
        public DateTimeOffset ListCreationTimestamp { get; set; }

        public SavedMasterList AsDTO()
        {
            return new SavedMasterList(Guid.Parse(RowKey), ListName, ListCreationTimestamp);
        }
    }
}
