﻿namespace VerbIt.DataModels;

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
    int[] HiddenColumnIndices,
    string? Hint,
    DateTimeOffset TestCreationTimestamp
);

// Shown to the admin on the Overall Tests page
public record TestOverviewResponse(TestOverviewEntry[] OverviewEntries, string? ContinuationToken);

public record TestOverviewEntry(
    Guid TestId,
    string TestName,
    int TotalRows,
    DateTimeOffset TestCreationTimestamp,
    bool IsAvailable,
    bool IsRetakeable,
    Guid SourceList,
    string SourceListName
);

// Detailed view of a test, when selected directly
public record TestWithResults(
    Guid TestId,
    string TestName,
    DateTimeOffset TestCreationTimestamp,
    bool IsAvailable,
    bool IsRetakeable,
    Guid SourceList,
    string SourceListName,
    TestRowSimple[] Questions,
    TestResultsOverviewRow[] ResultsSummaries
);

// Single question shown on the Admin's detailed test info page.
public record TestRowSimple(int RowNum, string[][] Words, int[] HiddenColumnIndices);

public record TestResultsOverviewRow(
    Guid StudentId,
    string StudentMoniker,
    int StudentScore,
    int MaxPossibleScore,
    int TotalAttempts,
    DateTimeOffset LatestAttemptTimestamp
);

// End detail test view

// Info shown to a user on the page before they take the test
public record UserTestInfo(Guid TestId, string TestName, int TotalRows, bool IsRetakeable);
