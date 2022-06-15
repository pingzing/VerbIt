using System.ComponentModel.DataAnnotations;

namespace VerbIt.DataModels;

public record LoginRequest([Required] string Username, [Required] string Password);

public record CreateAdminUserRequest([Required] string Username, [Required] string Password);

// --- Master List ---

public record CreateMasterListRequest([Required] string Name, [Required] [MinLength(1)] CreateMasterListRowRequest[] Rows);

public record CreateMasterListRowRequest([Required] [MinLength(1)] string[][] Words);

public record DeleteMasterListRowsRequest([Required] [MinLength(1)] Guid[] RowIds);

public record EditMasterListRequest(string? ListName, [MinLength(1)] EditMasterListRowRequest[]? Rows);

public record EditMasterListRowRequest(
    [Required] Guid RowId,
    [Range(1, int.MaxValue)] int? RowNum,
    [MinLength(1)] string[][]? Words
);

public record AddMasterListRowsRequest([Required] [MinLength(1)] CreateMasterListRowRequest[] Rows);

// --- Tests ---

public record CreateTestRowRequest([Required] [MinLength(1)] string[][] Words);

public record CreateTestRequest(
    [Required] string Name,
    [Required] Guid SourceList,
    [Required] [MinLength(1)] CreateTestRowRequest[] Rows
);
