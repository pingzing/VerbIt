using System.Security.Claims;
using System.Text.Json;

namespace VerbIt.Client.Services;

public class JwtParser
{
    public static IEnumerable<Claim> ParseClaims(string jwt)
    {
        string? payload = jwt.Split('.')[1];
        if (payload == null)
        {
            return Array.Empty<Claim>();
        }

        byte[] jsonBytes = ParseBase64WithoutPadding(payload);

        Dictionary<string, object>? keyPairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes)!;
        return keyPairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!)).ToList();
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        // Add padding characters if they're missing.
        base64 += (base64.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => "",
        };

        return Convert.FromBase64String(base64);
    }
}
