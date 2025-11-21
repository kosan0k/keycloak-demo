using System.Security.Authentication;

namespace Keycloak_demo.OIDC.Authentication.Exceptions;

/// <summary>
/// Thrown when a required token (like a refresh token) is not found.
/// </summary>
public class MissingTokenException(string message) : AuthenticationException(message)
{
}
