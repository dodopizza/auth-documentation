using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
namespace AuthCode.Dotnet.LowLevel;

/// <summary>
/// Implements part of key code exchange according to RFC:
/// https://datatracker.ietf.org/doc/html/rfc7636
/// </summary>
static class ProofKeyForCodeExchangeByOAuthPublicClients
{

    public static string GetRequiredConfig(this IConfiguration configuration, string name)
    {
        var required = configuration[name];
        if (required is null)
        {
            throw new Exception(
                "Required configuration parameter is not specified. Forgot to create appsettings.json? Take a look at appsettings.template.json. Also take a look at the README.md");
        }

        return required;
    }
    /// <summary>
    /// Generate Code Verifier temporary string.
    /// Proof Key for Code Exchange by OAuth Public Clients
    /// https://datatracker.ietf.org/doc/html/rfc7636#section-4.1
    /// </summary>
    public static string GenerateCodeVerifier()
    {
        const string ALPHA = "abcdefghijklmnopqrstuvwxyz";
        const string DIGIT = "0123456789";
        const string UNRESERVED = ALPHA + DIGIT + "-._~";
        const int ENTROPY_SIZE = 128;
        var entropy = new byte[ENTROPY_SIZE];
        RandomNumberGenerator.Fill(entropy);
        var randomCharacters = new char[ENTROPY_SIZE];
        for (var i = 0; i < ENTROPY_SIZE; i++)
        {
            randomCharacters[i] = UNRESERVED[entropy[i] % UNRESERVED.Length];
        }

        return new string(randomCharacters);
    }

    /// <summary>
    /// Generate Code Challenge from the Code Verifier string.
    /// Proof Key for Code Exchange by OAuth Public Clients
    /// https://datatracker.ietf.org/doc/html/rfc7636#section-4.1
    /// </summary>
    public static string GetCodeChallenge(string codeVerifier)
    {
        return WebEncoders.Base64UrlEncode(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(codeVerifier)));
    }
}