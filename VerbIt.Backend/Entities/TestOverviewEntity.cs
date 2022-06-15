using Azure;
using Azure.Data.Tables;
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
        public DateOnly TestCreationDate
        {
            // Convert the date to ticks, but subtracted from max, so that
            // when Table Storage returns rows, we always get them in descending order.
            get =>
                DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(long.MaxValue - long.Parse(PartitionKey)).UtcDateTime);
            set =>
                PartitionKey = (
                    long.MaxValue - new DateTimeOffset(value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeSeconds()
                ).ToString();
        }

        /// <summary>
        /// Convenience property for <see cref="RowKey"/>.
        /// </summary>
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
                SourceList
            );
        }

        public UserTestInfo AsUserDTO()
        {
            return new UserTestInfo(TestId, TestName, TotalRows, IsRetakeable);
        }
    }
}
