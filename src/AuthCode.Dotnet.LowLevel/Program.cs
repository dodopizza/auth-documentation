// See https://aka.ms/new-console-template for more information

using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using AuthCode.Dotnet.LowLevel;
using Microsoft.AspNetCore.DataProtection;

const string myAppName = "myapp";
const string authenticationCookieName = "myapp.auth";
const string codeVerifierCookieName = "myapp.signin.codeVerifier";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory()));

var configuration = builder.Configuration;

var clientId = configuration.GetRequiredConfig("Oidc:clientId");
var clientSecret = configuration.GetRequiredConfig("Oidc:clientSecret");
var scopes = configuration.GetRequiredConfig("Oidc:scopes");
var authority = configuration.GetRequiredConfig("Oidc:authority");
var myAppUri = configuration.GetRequiredConfig("Oidc:myAppUri");
var redirectUri = configuration.GetRequiredConfig("Oidc:redirectUri");

var app = builder.Build();

app.MapGet("/", (HttpContext context, ILogger<Program> logger, IDataProtectionProvider dataProtectionProvider) =>
{
    var authCookie = context.Request.Cookies[authenticationCookieName];

    if (authCookie is null)
    {
        logger.LogInformation("Auth cookie is not present. Sign in required.");
        GoToSignIn(context, dataProtectionProvider, logger);
        return;
    }

    logger.LogInformation("Auth cookie detected. Redirecting to protected area.");
    context.RespondWithRedirect("/protected-resource");
});

async Task Unauthorized(HttpContext context)
{
    await context.RespondWithHtml(403, "<html><body>Unauthorized.</body></html>");
}

void GoToSignIn(HttpContext context, IDataProtectionProvider dataProtectionProvider, ILogger logger)
{
    var codeVerifier = ProofKeyForCodeExchangeByOAuthPublicClients.GenerateCodeVerifier();

    logger.LogInformation("Generating code verifier...");

    var codeChallenge = ProofKeyForCodeExchangeByOAuthPublicClients.GetCodeChallenge(codeVerifier);

    logger.LogInformation("Generating code challenge...");

    var signInUrl =
        $"https://{authority}/connect/authorize"
        + $"?scope={HttpUtility.UrlEncode(scopes + " offline_access")}"
        + $"&response_type=code"
        + $"&client_id={HttpUtility.UrlEncode(clientId)}"
        + $"&redirect_uri={HttpUtility.UrlEncode(redirectUri)}"
        + $"&code_challenge={HttpUtility.UrlEncode(codeChallenge)}"
        + $"&code_challenge_method=S256";

    var protector = dataProtectionProvider.CreateProtector(myAppName);

    // Protecting cookie.
    // One should never store unencrypted value for codeVerifier in cookie.
    var encodedCodeVerifier = protector.Protect(codeVerifier);

    var cookies = new Dictionary<string, string> { { codeVerifierCookieName, encodedCodeVerifier } };

    logger.LogInformation("Setting secure cookie for code verifier...");

    context.SetSecureCookies(cookies);

    logger.LogInformation("Redirecting to signin...");

    context.RespondWithRedirect(signInUrl);
}

app.MapGet("/signin-oidc",
    async (HttpContext context, IDataProtectionProvider dataProtectionProvider, ILogger<Program> logger) =>
    {
        var code = context.Request.Query["code"].ToString();
        var encryptedCodeVerifier = context.Request.Cookies[codeVerifierCookieName];
        if (encryptedCodeVerifier is null)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Fatal: Code Verifier Cookie is missing or expired");
            return;
        }

        var protector = dataProtectionProvider.CreateProtector(myAppName);
        var codeVerifier = protector.Unprotect(encryptedCodeVerifier);

        using var client = new HttpClient();

        var result = await client.PostAsync($"https://{authority}/connect/token",
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    { "client_id", HttpUtility.UrlEncode(clientId) },
                    { "client_secret", clientSecret },
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "code_verifier", codeVerifier },
                    { "scope", scopes + " offline_access" },
                    { "redirect_uri", redirectUri },
                }));

        if (!result.IsSuccessStatusCode)
        {
            await context.RespondWithHtml(500,
                $"You've got a code, but failed to get token. Reason status code: {context.Response.StatusCode}");
            return;
        }

        var responseDataJson = await result.Content.ReadAsStringAsync(CancellationToken.None);

        logger.LogInformation("Received token. Token not shown here for security reasons.");

        var cookies = new Dictionary<string, string> { { authenticationCookieName, protector.Protect(responseDataJson) } };
        context.SetSecureCookies(cookies);
        context.RespondWithRedirect("/protected-resource");
    });

app.MapGet("/protected-resource",
    async (HttpContext context, IDataProtectionProvider dataProtectionProvider, ILogger<Program> logger) =>
    {
        var protectedCookie = context.Request.Cookies[authenticationCookieName];

        if (protectedCookie is null)
        {
            logger.LogInformation("Auth cookie is not present, redirecting to signin...");
            GoToSignIn(context, dataProtectionProvider, logger);
            return;
        }

        logger.LogInformation("Auth cookie detected.");
        var protector = dataProtectionProvider.CreateProtector(myAppName);
        var authCookie = protector.Unprotect(protectedCookie);
        Dictionary<string, object>? tokenInformation =
            JsonSerializer.Deserialize<Dictionary<string, object>>(authCookie);
        if (tokenInformation is null)
        {
            await Unauthorized(context);
            return;
        }

        // There is a ton of useful info here for us.
        // The main part is the access_token. It is used to get access to protected resources.
        var token = tokenInformation["access_token"].ToString();

        // Token will expire, so another useful part is refresh_token that you can use to refresh the token to get new token.
        // It will be available only if you requested offline access when submitting your request for API credentials,
        // so this part is not included in this example.

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        logger.LogInformation("Making authenticated API call to /connect/userinfo...");

        var responseJson = await client.GetStringAsync($"https://{authority}/connect/userinfo");
        var userInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);
        if (userInfo is null)
        {
            await context.RespondWithHtml(403, "<html><body>Unauthorized.</body></html>");
            return;
        }

        logger.LogInformation("Received reply from authenticated API call.");

        logger.LogInformation("Displaying useful info which can be shown only to authorized user...");

        await context.RespondWithHtml(200, $"""
<html>
    <body>
        <h1>Protected Area</h1>
        <p>User Info:</p>
        <pre>{responseJson}</pre>
        </pre>
    </body>
</html>
""");
    });

app.Run(myAppUri);