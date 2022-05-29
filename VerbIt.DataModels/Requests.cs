namespace VerbIt.DataModels;

public record LoginRequest(string Username, string Password);

public record CreateAdminUserRequest(string Username, string Password);
