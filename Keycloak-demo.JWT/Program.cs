
using Keycloak_demo.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Keycloak_demo.JWT
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Add Authentication Services
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    // 2. Configure Keycloak Authority
                    options.Authority = "https://keycloak.dev.local:8443/realms/dotnet-app-realm";
                    // 3. Configure HTTPS Handling (The EXPERT Solution)
                    // The JWT middleware ALSO makes backchannel calls (to.well-known and /certs)
                    // which will fail without this handler.
                    options.RequireHttpsMetadata = true;
                    if (builder.Environment.IsDevelopment())
                    {
                        // Re-use our custom handler from Part III-C to trust the mkcert CA
                        options.BackchannelHttpHandler = DevCertificateTrust.CreateTrustingHttpClientHandler(builder.Configuration["CertPath"]!);
                    }
                    // 4. Configure Token Validation Parameters
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // Validate the audience. This is critical security.
                        ValidateAudience = true,
                        // This MUST match the Client ID of our API
                        ValidAudience = builder.Configuration["Keycloak:Audience"]!,

                        ValidateIssuer = true, //
                        ValidIssuer = builder.Configuration["Keycloak:ValidIssuer"],
                        ValidateIssuerSigningKey = true,

                        // Tell.NET to find roles in the 'roles' claim
                        RoleClaimType = "roles",
                        NameClaimType = "name"
                    };
                });

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseAuthentication();
            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapGet("/index", (HttpContext httpContext) =>
            {
                return "Ok";
            })
            .WithName("index");

            app.Run();
        }
    }
}
