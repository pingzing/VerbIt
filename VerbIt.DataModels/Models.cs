using System.ComponentModel.DataAnnotations;

namespace VerbIt.DataModels;

public record AuthenticatedUser(string Name, string Role);

public record MasterListRow(
    [Required] Guid ListId,
    [Required] Guid RowId,
    [Required] string ListName,
    [Required] int RowNum,
    [Required] [MinLength(1)] string[][] Words,
    [Required] DateTimeOffset ListCreationTimestamp
);

public record SavedMasterList(Guid ListId, string ListName, DateTimeOffset ListCreationTimestamp, int TotalRows);
