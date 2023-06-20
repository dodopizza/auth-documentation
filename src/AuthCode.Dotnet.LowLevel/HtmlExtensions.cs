using System.Net.Mime;
using System.Text;

namespace AuthCode.Dotnet.LowLevel;

public static class HtmlExtensions
{
    public static void RespondWithRedirect(this HttpContext context, string location)
    {
        context.Response.StatusCode = 302;
        context.Response.Headers.Append("Location", location);
    }

    public static async Task RespondWithHtml(this HttpContext context, int statusCode, string html)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = MediaTypeNames.Text.Html;
        context.Response.ContentLength = Encoding.UTF8.GetByteCount(html);
        await context.Response.WriteAsync(html);
    }

    public static void SetSecureCookies(this HttpContext context, IDictionary<string, string> cookies)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            Secure = true,
            MaxAge = TimeSpan.FromHours(1)
        };

        foreach (var (name, value) in cookies)
        {
            context.Response.Cookies.Append(name, value, cookieOptions);
        }
    }
}