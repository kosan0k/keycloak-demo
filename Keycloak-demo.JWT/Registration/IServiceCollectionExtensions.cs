using Keycloak_demo.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Keycloak_demo.JWT.Registration;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration,
        bool isDevelopment)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Configure Keycloak Authority
                options.Authority = configuration["Keycloak:Issuer"];
                // Configure HTTPS Handling (The EXPERT Solution)
                // The JWT middleware ALSO makes backchannel calls (to.well-known and /certs)
                // which will fail without this handler.
                options.RequireHttpsMetadata = true;
                if (isDevelopment)
                {
                    // Re-use our custom handler from Part III-C to trust the mkcert CA
                    options.BackchannelHttpHandler = DevCertificateTrust.CreateTrustingHttpClientHandler(configuration["CertPath"]!);
                }
                // Configure Token Validation Parameters
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Validate the audience. This is critical security.
                    ValidateAudience = true,
                    // This MUST match the Client ID of our API
                    ValidAudience = configuration["Keycloak:Audience"]!,

                    ValidateIssuer = true, //
                    ValidIssuer = configuration["Keycloak:Issuer"],
                    ValidateIssuerSigningKey = true,

                    // Tell.NET to find roles in the 'roles' claim
                    RoleClaimType = "roles",
                    NameClaimType = "name"
                };
            });

        return services;
    }
}
