
using Keycloak_demo.JWT.Registration;

namespace Keycloak_demo.JWT
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services
                .AddJwtAuthentication(
                    configuration: builder.Configuration,
                    isDevelopment: builder.Environment.IsDevelopment())
                .AddAuthorization()
                .AddOpenApi(); // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

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
