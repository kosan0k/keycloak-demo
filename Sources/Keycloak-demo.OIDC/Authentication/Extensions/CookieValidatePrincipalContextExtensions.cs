using CSharpFunctionalExtensions;
using Keycloak_demo.OIDC.Authentication.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Keycloak_demo.OIDC.Authentication.Extensions;

internal static class CookieValidatePrincipalContextExtensions
{
    /// <summary>
    /// Checks if the access token is present and nearing expiration.
    /// </summary>
    internal static bool IsRefreshRequired(this CookieValidatePrincipalContext context, TimeSpan refreshThreshold)
    {
        return GetExpiresAt(context)
            .Map(expiresAt => expiresAt.Subtract(DateTimeOffset.UtcNow))
            .Map(timeRemaining => timeRemaining <= refreshThreshold)
            .GetValueOrDefault(false);
    }    

    /// <summary>
    /// Gets the refresh token, returning a failure Result if missing.
    /// </summary>
    internal static Result<string, Exception> GetRefreshToken(this CookieValidatePrincipalContext context)
    {
        var refreshToken = context.Properties.GetTokenValue("refresh_token");

        if (string.IsNullOrEmpty(refreshToken))
        {
            return Result.Failure<string, Exception>(
                new MissingTokenException("No refresh token found in cookie. Session cannot be validated."));
        }

        return Result.Success<string, Exception>(refreshToken);
    }

    /// <summary>
    /// Gets the 'expires_at' value from the cookie properties.
    /// </summary>
    private static Maybe<DateTimeOffset> GetExpiresAt(this CookieValidatePrincipalContext context)
    {
        var expiresAtString = context.Properties.GetTokenValue("expires_at");
        return DateTimeOffset.TryParse(expiresAtString, out var expiresAt)
            ? Maybe<DateTimeOffset>.From(expiresAt)
            : Maybe<DateTimeOffset>.None;
    }
}
