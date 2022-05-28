namespace VerbIt.Backend.Models;

public class JwtSettings
{
    public const string Key = nameof(JwtSettings);

    public string SecretKey { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int ExpiryMinutes { get; set; }
}
