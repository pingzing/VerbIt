namespace VerbIt.DataModels;

public record AuthenticatedUser(string Name, string Role);

public record MasterList(string Name, int Number, string[][] Words);
