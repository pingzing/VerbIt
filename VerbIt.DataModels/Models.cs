namespace VerbIt.DataModels;

public record AuthenticatedUser(string Name, string Role);

public record MasterListRow(
    Guid ListId,
    Guid RowId,
    string ListName,
    int RowNum,
    string[][] Words,
    DateTimeOffset ListCreationTimestamp
);

public record SavedMasterList(Guid ListId, string ListName, DateTimeOffset ListCreationTimestamp, int TotalRows);
