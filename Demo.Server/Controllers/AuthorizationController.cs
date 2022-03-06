using Demo.Server.Helpers;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Microsoft.Owin.Security;
using OpenIddict.Abstractions;
using OpenIddict.Server.Owin;
using Owin;

using static OpenIddict.Abstractions.OpenIddictConstants;
using Microsoft.Owin;

namespace Demo.Server.Controllers;

public class AuthorizationController : Controller
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;

    public AuthorizationController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
    }

    [HttpPost, Route("~/auth/token")]
    public async Task<ActionResult> Exchange()
    {
        // Retrieve OpenIdDict Request
        IOwinContext      context = HttpContext.GetOwinContext();
        OpenIddictRequest request = context.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsClientCredentialsGrantType())
        {
            // Retrieve the application details from the database.
            object application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                throw new InvalidOperationException("Details concerning the calling client application cannot be found.");

            // Create a new Identity
            ClaimsIdentity identity = new ClaimsIdentity(OpenIddictServerOwinDefaults.AuthenticationType);
            identity.AddClaim(new Claim(Claims.Subject, "Test Subject"));
            identity.AddClaim(new Claim(Claims.Name, "Test Name").SetDestinations(Destinations.AccessToken));

            ClaimsPrincipal principal = new(identity);

            // Set Scopes based on requested scopes
            // OpenIdDict already validates the credentials and valid scopes, no need to validate it here.
            principal.SetScopes(request.GetScopes());
            principal.SetResources(await _scopeManager.ListResourcesAsync(principal.GetScopes()).ToListAsync());


            // Create Authorization with requiered scopes and Types.
            object authorization = await _authorizationManager.CreateAsync(
                principal: principal,
                subject: "1",
                client: await _applicationManager.GetIdAsync(application),
                type: AuthorizationTypes.Permanent,
                scopes: principal.GetScopes()
            );

            principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));

            // Set Destinations of claims (The claims should go Access Token or ID Token?)
            foreach (Claim claim in principal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, principal));
            }

            // Return the Access Token to the newly created Identity
            context.Authentication.SignIn(new AuthenticationProperties(), (ClaimsIdentity)principal.Identity);

            return new EmptyResult();
        }

        else
        {
            // Other flows not enabled.
            context.Authentication.Challenge(
                    authenticationTypes: OpenIddictServerOwinDefaults.AuthenticationType,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerOwinConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerOwinConstants.Properties.ErrorDescription] =
                            "No :("
                    }));
        }

        return new EmptyResult();
    }

    private IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        switch (claim.Type)
        {
            case Claims.Name:
                yield return Destinations.AccessToken;

                if (principal.HasScope(Scopes.Profile))
                    yield return Destinations.IdentityToken;

                yield break;

            // Never include the security stamp in the access and identity tokens, as it's a secret value.
            case "AspNet.Identity.SecurityStamp": yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}
