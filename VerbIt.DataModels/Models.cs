namespace VerbIt.DataModels;

public record AuthenticatedUser(string Name, string Role);

// Master lists

public record MasterListRow(
    Guid ListId,
    Guid RowId,
    string ListName,
    int RowNum,
    string[][] Words,
    DateTimeOffset ListCreationTimestamp
);

public record SavedMasterList(Guid ListId, string ListName, DateTimeOffset ListCreationTimestamp, int TotalRows);

// Tests

public record TestRow(
    Guid TestId,
    Guid RowId,
    string TestName,
    int RowNum,
    string[][] Words,
    DateTimeOffset TestCreationTimestamp
);

public record AdminTestInfo(
    Guid TestId,
    string TestName,
    int TotalRows,
    DateTimeOffset TestCreationTimestamp,
    bool IsAvailable,
    bool IsRetakeable,
    Guid SourceList
);

public record UserTestInfo(Guid TestId, string TestName, int TotalRows, bool IsRetakeable);
