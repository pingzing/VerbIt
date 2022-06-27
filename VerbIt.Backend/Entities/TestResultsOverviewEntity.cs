using Azure;
using Azure.Data.Tables;
using VerbIt.DataModels;

namespace VerbIt.Backend.Entities
{
    public class TestResultsOverviewEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Both TestId and UserId can be used interchangeably as RowKeys or PartitionKeys--the table this entity lives in
        // stores two copies of each entity with RK/PK interchanged to enable lookup by User or by Test.
        public Guid TestId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentMoniker { get; set; } = null!;
        public int StudentScore { get; set; }
        public int MaxPossibleScore { get; set; }
        public int TotalAttempts { get; set; }
        public DateTimeOffset LastAttemptTimestamp { get; set; }

        public TestResultsOverviewRow AsDTO()
        {
            return new TestResultsOverviewRow(
                StudentId,
                StudentMoniker,
                StudentScore,
                MaxPossibleScore,
                TotalAttempts,
                LastAttemptTimestamp
            );
        }
    }
}
