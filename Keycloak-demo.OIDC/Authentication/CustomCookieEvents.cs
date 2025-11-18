using CSharpFunctionalExtensions;
using Duende.IdentityModel.Client;
using Keycloak_demo.OIDC.Authentication.Exceptions;
using Keycloak_demo.OIDC.Authentication.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Keycloak_demo.OIDC.Authentication;

public class CustomCookieEvents : CookieAuthenticationEvents
{
    private readonly ILogger<CustomCookieEvents> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _refreshThreshold = TimeSpan.FromMinutes(5);

    public CustomCookieEvents(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CustomCookieEvents> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (!context.IsRefreshRequired(_refreshThreshold))
        {
            return; // Not expiring soon, do nothing.
        }

        // The ROP chain now produces a Result<TokenResponse, Exception>
        var result = await context.GetRefreshToken()
            .Bind(RequestNewTokensAsync);

        if (result.IsSuccess)
        {
            // SUCCESS PATH
            UpdateCookieTokens(context, result.Value);
        }
        else
        {
            // FAILURE PATH
            _logger.LogError(result.Error, "Error on getting or refreshing token");

            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
    }    

    /// <summary>
    /// Requests new tokens from Keycloak, returning a failure Result if the request fails.
    /// </summary>
    private async Task<Result<TokenResponse, Exception>> RequestNewTokensAsync(string refreshToken)
    {
        Result<TokenResponse, Exception> result;

        try
        {
            var client = _httpClientFactory.CreateClient();
            var tokenEndpoint = $"{_configuration["Keycloak:Authority"]}/protocol/openid-connect/token";

            var response = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = tokenEndpoint,
                ClientId = _configuration["Keycloak:ClientId"]!,
                ClientSecret = _configuration["Keycloak:ClientSecret"],
                RefreshToken = refreshToken
            });

            result = response.IsError
                ? Result.Failure<TokenResponse, Exception>(
                    new TokenRefreshException(
                        response.ErrorType,
                        response.Error,
                        response.ErrorDescription))
                : Result.Success<TokenResponse, Exception>(response);            
        }
        catch(Exception ex)
        {
            result = new TokenRefreshException(
                message: "Error on requesting new token",
                innerException: ex);
        }

        return result;
    }

    /// <summary>
    /// Happy path operation: Updates the cookie with fresh tokens.
    /// </summary>
    private static void UpdateCookieTokens(CookieValidatePrincipalContext context, TokenResponse response)
    {
        var newAccessToken = response.AccessToken ?? string.Empty;
        var newRefreshToken = response.RefreshToken ?? string.Empty;
        var newExpiresAt = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn);

        context.Properties.UpdateTokenValue("access_token", newAccessToken);
        context.Properties.UpdateTokenValue("refresh_token", newRefreshToken);
        context.Properties.UpdateTokenValue("expires_at", newExpiresAt.ToString("o"));

        context.ShouldRenew = true;
    }
}
