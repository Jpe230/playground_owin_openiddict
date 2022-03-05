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

namespace Demo.Server.Controllers
{
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
            var context = HttpContext.GetOwinContext();
            var request = context.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (request.IsClientCredentialsGrantType())
            {

                // Retrieve the application details from the database.
                var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                    throw new InvalidOperationException("Details concerning the calling client application cannot be found.");


                var identity = new ClaimsIdentity(OpenIddictServerOwinDefaults.AuthenticationType);
                identity.AddClaim(new Claim(Claims.Subject, "Test Subject"));
                identity.AddClaim(new Claim(Claims.Name, "Test Name").SetDestinations(Destinations.AccessToken));

                var principal = new ClaimsPrincipal(identity);

                principal.SetScopes(request.GetScopes());
                principal.SetResources(await _scopeManager.ListResourcesAsync(principal.GetScopes()).ToListAsync());

                var authorization = await _authorizationManager.CreateAsync(
                    principal: principal,
                    subject: "1",
                    client: await _applicationManager.GetIdAsync(application),
                    type: AuthorizationTypes.Permanent,
                    scopes: principal.GetScopes()
                );

                principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));

                foreach (var claim in principal.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, principal));
                }

                context.Authentication.SignIn(new AuthenticationProperties(), (ClaimsIdentity)principal.Identity);

                return new EmptyResult();
            }

            else
            {
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
}
