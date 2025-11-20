using Keycloak_demo.OIDC.Authentication;
using Keycloak_demo.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Keycloak_demo.OIDC;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // This line provides the 'IHttpClientFactory' service.
        builder.Services.AddHttpClient();

        // Register your new event class with DI
        // Scoped is a good lifetime for event handlers.
        builder.Services.AddScoped<CustomCookieEvents>();

        // 1. Add Authentication Services
        builder.Services.AddAuthentication(options =>
        {
            // Use a cookie for the user's local session
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            // When authentication is required, challenge with OIDC
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Name = "keycloak.cookie";

            // Set the session duration. This is the "inactivity" timeout.
            // If the user is inactive for 60 minutes, the cookie will expire.
            options.ExpireTimeSpan = TimeSpan.FromMinutes(60);

            // This is the key setting. If true, the middleware will re-issue a new
            // cookie with a new expiration time on any request that occurs after
            // the halfway point of the ExpireTimeSpan has passed.
            options.SlidingExpiration = true;

            // Tell the cookie handler to use your DI-managed class
            // It will be resolved from the service provider for each request.
            options.EventsType = typeof(CustomCookieEvents);
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            // 2. Configure Keycloak Authority
            // This is the URL of our HTTPS-enabled Keycloak realm
            options.Authority = builder.Configuration["Keycloak:Authority"];

            // 3. Configure Client ID and Secret
            options.ClientId = builder.Configuration["Keycloak:ClientId"];
            options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];

            // 4. Configure OIDC Flow
            options.ResponseType = OpenIdConnectResponseType.Code; // Use Authorization Code Flow 
            options.Scope.Add("openid");
            options.Scope.Add("profile");

            // 5. Store Tokens for API Calls
            // This is CRITICAL for calling a downstream API.
            options.SaveTokens = true;

            // 6. Configure HTTPS Handling (The EXPERT Solution)
            // We DO NOT set RequireHttpsMetadata = false.
            // We keep it TRUE and provide a handler that trusts our local CA.
            options.RequireHttpsMetadata = true;
            if (builder.Environment.IsDevelopment())
            {
                // Use our custom handler to trust the mkcert CA
                options.BackchannelHttpHandler = DevCertificateTrust.CreateTrustingHttpClientHandler(builder.Configuration["CertPath"]!);
            }

            // 7. Configure Claim Mapping
            // Prevents.NET from renaming claims (e.g., 'sub' to 'nameidentifier')
            options.MapInboundClaims = false; // 
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // Use the 'name' claim for User.Identity.Name
                NameClaimType = "name",

                // Tell.NET to find roles in the 'roles' claim we created with our mapper
                RoleClaimType = "roles"
            };

            options.UseTokenLifetime = false;
        });

        // Add Authorization services

        // Other way to enable role-based authorization if keyclock mapper was not configured
        builder.Services.AddTransient<IClaimsTransformation, RoleClaimsTransformation>();
        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor(); // Needed to get tokens for API calls

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        // 8. Enable Authentication and Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet(
                pattern: "/index", 
                handler: () => "You are loged now")
            .RequireAuthorization();

        app.Run();
    }
}
