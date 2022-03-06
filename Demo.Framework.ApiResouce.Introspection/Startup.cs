
using System.Reflection;
using System.Web.Http;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Integration.WebApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using OpenIddict.Validation.Owin;
using Owin;

[assembly: OwinStartup(typeof(Demo.Framework.ApiResource.Introspection.Startup))]

namespace Demo.Framework.ApiResource.Introspection;

public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        var container = CreateContainer();

        //app.UseErrorPage();

        // Register the Autofac scope injector middleware.
        app.UseAutofacLifetimeScopeInjector(container);

        // Register the two OpenIddict server/validation middleware.
        app.UseMiddlewareFromContainer<OpenIddictValidationOwinMiddleware>();

        var configuration = new HttpConfiguration
        {
            DependencyResolver = new AutofacWebApiDependencyResolver(container)
        };

        configuration.MapHttpAttributeRoutes();

        // Configure ASP.NET Web API to use token authentication.
        configuration.Filters.Add(new HostAuthenticationFilter(OpenIddictValidationOwinDefaults.AuthenticationType));

        // Register the Web API/Autofac integration middleware.
        app.UseAutofacWebApi(configuration);
        app.UseWebApi(configuration);
    }

    private static IContainer CreateContainer()
    {
        var services = new ServiceCollection();

        services.AddOpenIddict()
                .AddValidation(options =>
                {
                    // Note: the validation handler uses OpenID Connect discovery
                    // to retrieve the address of the introspection endpoint.
                    options.SetIssuer("https://localhost:44336/");
                    options.AddAudiences("resource_server_1");

                    // Configure the validation handler to use introspection and register the client
                    // credentials used when communicating with the remote introspection endpoint.
                    options.UseIntrospection()
                            .SetClientId("resource_server_1")
                            .SetClientSecret("846B62D0-DEF9-4215-A99D-86E6B8DAB342");

                    // Register the System.Net.Http integration.
                    options.UseSystemNetHttp();

                    // Register the ASP.NET Core host.
                    options.UseOwin();

                });

        // Create a new Autofac container and import the OpenIddict services.
        var builder = new ContainerBuilder();
        builder.Populate(services);
        builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

        return builder.Build();
    }
}
