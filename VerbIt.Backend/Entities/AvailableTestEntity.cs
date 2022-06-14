using Azure;
using Azure.Data.Tables;
using VerbIt.DataModels;

namespace VerbIt.Backend.Entities
{
    public class AvailableTestEntity : ITableEntity
    {
        public const string DefaultPartitionKey = "AvailableTests";

        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        /// <summary>
        /// Convenience property for <see cref="RowKey"/>.
        /// </summary>
        public Guid TestId
        {
            get => Guid.Parse(PartitionKey);
            set => PartitionKey = value.ToString();
        }

        public string TestName { get; set; } = null!;
        public int TotalRows { get; set; }
        public bool IsRetakeable { get; set; }

        public UserTestInfo AsDTO()
        {
            return new UserTestInfo(TestId, TestName, TotalRows, IsRetakeable);
        }
    }
}
