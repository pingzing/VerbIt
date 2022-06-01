using Azure;
using Azure.Data.Tables;
using System.Runtime.Serialization;
using System.Text.Json;
using VerbIt.DataModels;

namespace VerbIt.Backend.Entities
{
    public class MasterListRowEntity : ITableEntity
    {
        /// <summary>
        /// An opaque GUID in string form. Has a convenience property at <see cref="ListId"/>.
        /// </summary>
        public string PartitionKey { get; set; } = null!;

        /// <summary>
        /// An opaque GUID in string form. Has a convenience proeprty at <see cref="Number"/>.
        /// </summary>
        public string RowKey { get; set; } = null!;

        /// <summary>
        /// Convnience property for getting the PartitionKey.
        /// </summary>
        [IgnoreDataMember]
        public Guid ListId
        {
            get => Guid.Parse(PartitionKey);
            set => PartitionKey = value.ToString();
        }

        /// <summary>
        /// Convenience property for getting the RowKey.
        /// </summary>
        [IgnoreDataMember]
        public Guid RowId
        {
            get => Guid.Parse(RowKey);
            set => RowKey = value.ToString();
        }

        /// <summary>
        /// Name of the list this row is associated with.
        /// </summary>
        public string ListName { get; set; } = null!;

        /// <summary>
        /// Used for ordering the row.
        /// </summary>
        public int RowNum { get; set; }

        public string WordsJson { get; set; } = null!;
        public DateTimeOffset ListCreationTimestamp { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public MasterListRow AsDTO()
        {
            string[][] deserializedWordList = JsonSerializer.Deserialize<string[][]>(WordsJson)!;
            return new MasterListRow(ListId, RowId, ListName, RowNum, deserializedWordList, ListCreationTimestamp);
        }

        public static MasterListRowEntity FromDTO(MasterListRow dto)
        {
            return new MasterListRowEntity
            {
                ListId = dto.ListId,
                RowId = dto.RowId,
                ListName = dto.ListName,
                RowNum = dto.RowNum,
                WordsJson = JsonSerializer.Serialize(dto.Words),
                ListCreationTimestamp = dto.ListCreationTimestamp
            };
        }

        public static MasterListRowEntity FromCreateRequest(
            Guid listId,
            string listName,
            int rowNum,
            string[][] words,
            DateTimeOffset listCreationTimstamp
        )
        {
            return new MasterListRowEntity
            {
                ListId = listId,
                RowId = Guid.NewGuid(),
                ListName = listName,
                RowNum = rowNum,
                WordsJson = JsonSerializer.Serialize(words),
                ListCreationTimestamp = listCreationTimstamp
            };
        }
    }
}
