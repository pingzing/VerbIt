using Azure;
using Azure.Data.Tables;
using System.Runtime.Serialization;
using System.Text.Json;
using VerbIt.DataModels;

namespace VerbIt.Backend.Entities
{
    public class TestRowEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        /// <summary>
        /// Convenience property for getting the <see cref="PartitionKey"/>.
        /// </summary>
        [IgnoreDataMember]
        public Guid TestId
        {
            get => Guid.Parse(PartitionKey);
            set => PartitionKey = value.ToString();
        }

        /// <summary>
        /// Convenience property for getting the <see cref="RowKey"/>.
        /// </summary>
        [IgnoreDataMember]
        public Guid RowId
        {
            get => Guid.Parse(RowKey);
            set => RowKey = value.ToString();
        }

        public string TestName { get; set; } = null!;

        public int RowNum { get; set; }

        public DateTimeOffset TestCreationTimestamp { get; set; }

        public string WordsJson { get; set; } = null!;

        public byte[] HiddenColumnsIndices { get; set; } = null!;

        public TestRow AsDTO()
        {
            string[][] deserializedWordList = JsonSerializer.Deserialize<string[][]>(WordsJson)!;
            int[] hiddenColumnIndices = HiddenColumnsIndices.Cast<int>().ToArray();
            return new TestRow(TestId, RowId, TestName, RowNum, deserializedWordList, hiddenColumnIndices, TestCreationTimestamp);
        }

        public TestRowSimple AsSimpleDTO()
        {
            string[][] deserializedWordList = JsonSerializer.Deserialize<string[][]>(WordsJson)!;
            int[] hiddenColumnIndices = HiddenColumnsIndices.Cast<int>().ToArray();
            return new TestRowSimple(RowNum, deserializedWordList, hiddenColumnIndices);
        }
    }

    public static class CreateTestRowExtensions
    {
        public static TestRowEntity AsEntity(
            this CreateTestRowRequest request,
            Guid testId,
            string testName,
            int rowNum,
            DateTimeOffset testCreationTimestamp
        )
        {
            return new TestRowEntity
            {
                TestId = testId,
                RowId = Guid.NewGuid(),
                TestName = testName,
                RowNum = rowNum,
                TestCreationTimestamp = testCreationTimestamp,
                WordsJson = JsonSerializer.Serialize(request.Words),
                HiddenColumnsIndices = request.ColumnsHiddenIndices.Cast<byte>().ToArray(),
            };
        }
    }
}
