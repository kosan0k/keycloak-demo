using Keycloak_demo.OIDC.Registration;

namespace Keycloak_demo.OIDC;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add keycloak authentication
        builder.Services
            .AddKeycloakOpenIdAuthentication(
                configuration: builder.Configuration,
                isDevelopment: builder.Environment.IsDevelopment())
            .AddAuthorization()
            .AddHttpContextAccessor(); // Needed to get tokens for API calls

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
