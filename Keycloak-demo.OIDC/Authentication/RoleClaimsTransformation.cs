using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace Keycloak_demo.OIDC.Authentication;

public class RoleClaimsTransformation : IClaimsTransformation
{
    private static readonly JsonSerializerOptions _serializeOptions = new()
    { 
        PropertyNameCaseInsensitive = true // Ignore case when deserializing JSON
    };

    public class RealmAccess 
    { 
        public List<string>? Roles { get; init; } // one user can be assigned multiple roles
    } 

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;

        // when decoding the token with jwt.io roles is present under realm_access
        var realmAccessClaim = identity?.FindFirst("realm_access");
        if (realmAccessClaim != null)
        {
            // Deserialize the realm_access JSON to extract the roles
            var realmAccess = JsonSerializer.Deserialize<RealmAccess>(realmAccessClaim.Value, _serializeOptions);

            if (realmAccess?.Roles != null)
            {
                foreach (var role in realmAccess.Roles)
                {
                    // Add each role as a Claim of type ClaimTypes.Role
                    identity!.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }
        }

        return Task.FromResult(principal);
    }    
}
