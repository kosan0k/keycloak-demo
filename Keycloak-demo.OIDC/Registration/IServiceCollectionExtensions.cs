using Keycloak_demo.OIDC.Authentication;
using Keycloak_demo.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Keycloak_demo.OIDC.Registration;

internal static class IServiceCollectionExtensions
{
    internal static IServiceCollection AddKeycloakOpenIdAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        
        services
            // This line provides the 'IHttpClientFactory' service.
            .AddHttpClient()
            // Register your new event class with DI
            // Scoped is a good lifetime for event handlers.
            .AddScoped<CustomCookieEvents>()
            // Add Authentication Services
            .AddAuthentication(options =>
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
                // Configure Keycloak Authority
                // This is the URL of our HTTPS-enabled Keycloak realm
                options.Authority = configuration["Keycloak:Authority"];

                // Configure Client ID and Secret
                options.ClientId = configuration["Keycloak:ClientId"];
                options.ClientSecret = configuration["Keycloak:ClientSecret"];

                // Configure OIDC Flow
                options.ResponseType = OpenIdConnectResponseType.Code; // Use Authorization Code Flow 
                options.Scope.Add("openid");
                options.Scope.Add("profile");

                // Store Tokens for API Calls
                // This is CRITICAL for calling a downstream API.
                options.SaveTokens = true;

                // Configure HTTPS Handling (The EXPERT Solution)
                // We DO NOT set RequireHttpsMetadata = false.
                // We keep it TRUE and provide a handler that trusts our local CA.
                options.RequireHttpsMetadata = true;
                if (isDevelopment)
                {
                    // Use our custom handler to trust the mkcert CA
                    options.BackchannelHttpHandler = DevCertificateTrust.CreateTrustingHttpClientHandler(configuration["CertPath"]!);
                }

                // Configure Claim Mapping
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

        // Other way to enable role-based authorization if keyclock mapper was not configured
        services.AddTransient<IClaimsTransformation, RoleClaimsTransformation>();

        return services;
    }
}
