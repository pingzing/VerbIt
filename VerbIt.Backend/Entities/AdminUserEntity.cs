using Azure;
using Azure.Data.Tables;
using System.Runtime.Serialization;

namespace VerbIt.Backend.Entities
{
    public class AdminUserEntity : ITableEntity
    {
        public const string DefaultPartitionKey = "Admin";

        public string PartitionKey { get; set; } = DefaultPartitionKey;
        public string RowKey { get; set; } = null!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        /// <summary>
        /// Convenience helper for <see cref="RowKey"/>.
        /// </summary>
        [IgnoreDataMember]
        public string Name
        {
            get => RowKey;
            set => RowKey = value;
        }

        public string HashedPassword { get; set; } = null!;
    }
}
