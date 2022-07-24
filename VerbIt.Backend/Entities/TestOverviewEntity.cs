using Azure;
using Azure.Data.Tables;
using System.Runtime.Serialization;
using VerbIt.DataModels;

namespace VerbIt.Backend.Entities
{
    public class TestOverviewEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        /// <summary>
        /// Convenience property for <see cref="PartitionKey"/>.
        /// </summary>
        [IgnoreDataMember]
        public DateOnly TestCreationDate
        {
            get => ConvertFromPartitionKey(PartitionKey);
            set => PartitionKey = ConvertToPartitionKey(value);
        }

        /// <summary>
        /// Convenience property for <see cref="RowKey"/>.
        /// </summary>
        [IgnoreDataMember]
        public Guid TestId
        {
            get => Guid.Parse(RowKey);
            set => RowKey = value.ToString();
        }

        public string TestName { get; set; } = null!;
        public int TotalRows { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsRetakeable { get; set; }
        public Guid SourceList { get; set; }
        public string SourceListName { get; set; } = null!;

        /// <summary>
        /// Essentially the same as TestCreationDate, but easier to serialize.
        /// </summary>
        public DateTimeOffset TestCreationTimestamp { get; set; }

        public TestOverviewEntry AsOverviewDTO()
        {
            return new TestOverviewEntry(
                TestId,
                TestName,
                TotalRows,
                TestCreationTimestamp,
                IsAvailable,
                IsRetakeable,
                SourceList,
                SourceListName
            );
        }

        public UserTestInfo AsUserDTO()
        {
            return new UserTestInfo(TestId, TestName, TotalRows, IsRetakeable);
        }

        public static string ConvertToPartitionKey(DateOnly date)
        {
            // Convert the date to Unix Seconds, but subtracted from max, so that
            // when Table Storage returns rows, we always get them in descending order.
            return (
                long.MaxValue - new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeSeconds()
            ).ToString();
        }

        public static DateOnly ConvertFromPartitionKey(string partitionKey)
        {
            // Convert the date to ticks, but subtracted from max, so that
            // when Table Storage returns rows, we always get them in descending order.
            return DateOnly.FromDateTime(
                DateTimeOffset.FromUnixTimeSeconds(long.MaxValue - long.Parse(partitionKey)).UtcDateTime
            );
        }
    }
}
