using System.Net;
using System.Security.Claims;
using System.Web.Http;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Demo.Framework.ApiResource.Introspection.Controllers;

[RoutePrefix("api")]
public class ResourceController : ApiController
{
    [Authorize, HttpGet]
    [Route("message")]
    public IHttpActionResult GetMessage()
    {
        ClaimsPrincipal principal = (ClaimsPrincipal) User;
        string name = principal.FindFirst(Claims.Name).Value;

        return Content(HttpStatusCode.OK, $"{name} has been successfully authenticated in API 1");
    }
}
