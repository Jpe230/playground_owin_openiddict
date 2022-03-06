using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Integration.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using OpenIddict.Server.Owin;
using OpenIddict.Validation.Owin;
using Owin;
using System.Web.Mvc;

[assembly: OwinStartupAttribute(typeof(Demo.Server.Startup))]

namespace Demo.Server;

public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        app.CreatePerOwinContext(ApplicationDbContext.Create);

        IContainer container = CreateContainer();

        // Register the Autofac scope injector middleware.
        app.UseAutofacLifetimeScopeInjector(container);

        // Register the two OpenIddict server/validation middleware.
        app.UseMiddlewareFromContainer<OpenIddictServerOwinMiddleware>();
        app.UseMiddlewareFromContainer<OpenIddictValidationOwinMiddleware>();

        // Configure ASP.NET MVC 5.2 to use Autofac when activating controller instances.
        DependencyResolver.SetResolver(new AutofacDependencyResolver(container));

        TestData.GenerateTestData(container);
    }

    private static IContainer CreateContainer()
    {
        ServiceCollection services = new();

        services.AddOpenIddict()

            // Register the OpenIddict core components.
            .AddCore(options =>
            {
                // Configure OpenIddict to use the Entity Framework 6.x stores and models.
                // Note: call ReplaceDefaultEntities() to replace the default OpenIddict entities.
                options.UseEntityFramework()
                       .UseDbContext<ApplicationDbContext>();
            })

            // Register the OpenIddict server components.
            .AddServer(options =>
            {
                // Enable the authorization, logout and token endpoints.
                options.SetAuthorizationEndpointUris("/auth/authorize")
                       .SetIntrospectionEndpointUris("/auth/introspect")
                       .SetTokenEndpointUris("/auth/token");

                // Allow Client Credentials for M2M
                options.AllowAuthorizationCodeFlow()
                       .AllowClientCredentialsFlow();

                // Register the signing and encryption credentials.
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                // Register the OWIN host and configure the OWIN-specific options.
                options.UseOwin()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough();
            })

            // Register the OpenIddict validation components.
            .AddValidation(options =>
            {
                // Import the configuration from the local OpenIddict server instance.
                options.UseLocalServer();

                // Register the OWIN host.
                options.UseOwin();
            });

        // Create a new Autofac container and import the OpenIddict services.
        ContainerBuilder builder = new();
        builder.Populate(services);

        // Register the MVC controllers.
        builder.RegisterControllers(typeof(Startup).Assembly);

        return builder.Build();
    }

}
