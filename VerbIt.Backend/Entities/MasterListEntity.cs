using Azure;
using Azure.Data.Tables;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.Json;
using VerbIt.DataModels;

namespace VerbIt.Backend.Entities
{
    public class MasterListRowEntity : ITableEntity
    {
        /// <summary>
        /// The Master List's name. Has a convenience property at <see cref="Name"/>.
        /// </summary>
        public string PartitionKey { get; set; } = null!;

        /// <summary>
        /// The row number for the given set of words in this master list. Has a convenience proeprty at <see cref="Number"/>.
        /// </summary>
        public string RowKey { get; set; } = null!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        /// <summary>
        /// The Partition Key. A unique name for this master list.
        /// </summary>
        [IgnoreDataMember]
        public string Name
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        /// <summary>
        /// The Row Key. The 1-based number of the set of words for this row.
        /// </summary>
        [IgnoreDataMember]
        public int Number
        {
            get => int.Parse(RowKey, CultureInfo.InvariantCulture);
            set => RowKey = value.ToString(CultureInfo.InvariantCulture);
        }

        public string WordsJson { get; set; } = null!;

        public MasterListRow AsDTO()
        {
            string[][] deserializedWordList = JsonSerializer.Deserialize<string[][]>(WordsJson)!;
            return new MasterListRow(Name, Number, deserializedWordList);
        }

        public static MasterListRowEntity FromDTO(MasterListRow dto)
        {
            return new MasterListRowEntity
            {
                Name = dto.Name,
                Number = dto.Number,
                WordsJson = JsonSerializer.Serialize(dto.Words)
            };
        }
    }

    public class EditMasterListRowEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string WordsJson { get; set; } = null!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public static EditMasterListRowEntity FromDTO(MasterListRowEditRequest request, string tableName)
        {
            return new EditMasterListRowEntity
            {
                PartitionKey = tableName,
                RowKey = request.Number.ToString(CultureInfo.InvariantCulture),
                WordsJson = JsonSerializer.Serialize(request.Words)
            };
        }
    }

    public class DeleteMasterListRowEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public static DeleteMasterListRowEntity FromDTO(MasterListRowDeleteRequest request, string tableName)
        {
            return new DeleteMasterListRowEntity
            {
                PartitionKey = tableName,
                RowKey = request.Number.ToString(CultureInfo.InvariantCulture),
            };
        }
    }
}
