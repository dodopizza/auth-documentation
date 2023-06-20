var builder = WebApplication.CreateBuilder(args);

var identityUrl = builder.Configuration.GetValue<string>("Oidc:authority");
var sessionCookieLifetime = 60;
var clientId = builder.Configuration.GetValue<string>("Oidc:clientId");
var clientSecret = builder.Configuration.GetValue<string>("Oidc:clientSecret");

var oidcScheme = "openid";
var cookieScheme = "cookie";

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = oidcScheme;
        options.DefaultChallengeScheme = oidcScheme;
    })
    .AddCookie(cookieScheme, setup => setup.ExpireTimeSpan = TimeSpan.FromMinutes(sessionCookieLifetime))
    .AddOpenIdConnect(oidcScheme, options =>
    {
        options.SignInScheme = cookieScheme;
        options.Authority = $"https://{identityUrl}";
        options.ClientId = clientId;
        options.ClientSecret = clientSecret;

        options.ResponseType = "code";

        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.RequireHttpsMetadata = false;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("offline_access");
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();