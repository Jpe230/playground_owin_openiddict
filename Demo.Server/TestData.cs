using Autofac;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Demo.Server
{
    public class TestData
    {
        public static void GenerateTestData(IContainer container)
        {

            Task.Run(async delegate
            {
                using (var scope = container.BeginLifetimeScope())
                {
                    var context = scope.Resolve<ApplicationDbContext>();
                    context.Database.CreateIfNotExists();

                    IOpenIddictApplicationManager appManager = scope.Resolve<IOpenIddictApplicationManager>();
                    IOpenIddictScopeManager scopeManager = scope.Resolve<IOpenIddictScopeManager>();

                    if (await appManager.FindByClientIdAsync("console_app") is null)
                    {
                        await appManager.CreateAsync(new OpenIddictApplicationDescriptor
                        {
                            ClientId = "console_app",
                            Permissions =
                            {
                                Permissions.Endpoints.Authorization,
                                Permissions.Endpoints.Token,
                                Permissions.GrantTypes.AuthorizationCode,
                                Permissions.ResponseTypes.Code,
                                Permissions.Scopes.Email,
                                Permissions.Scopes.Profile,
                                Permissions.Scopes.Roles,
                                Permissions.Prefixes.Scope + "api1",
                                Permissions.Prefixes.Scope + "api2"
                            }
                        });
                    }

                    if (await appManager.FindByClientIdAsync("postman-1") is null)
                    {
                        await appManager.CreateAsync(new OpenIddictApplicationDescriptor
                        {
                            ClientId = "postman-1",
                            ClientSecret = "postman-secret",
                            DisplayName = "Postman-1",
                            RedirectUris = { new Uri("https://oauth.pstmn.io/v1/callback") },
                            Permissions =
                            {
                                Permissions.Endpoints.Authorization,
                                Permissions.Endpoints.Token,

                                Permissions.GrantTypes.AuthorizationCode,
                                Permissions.GrantTypes.ClientCredentials,

                                Permissions.Scopes.Email,
                                Permissions.Scopes.Profile,
                                Permissions.Scopes.Roles,

                                Permissions.Prefixes.Scope + "api1",
                                Permissions.Prefixes.Scope + "api2",

                                Permissions.ResponseTypes.Code,
                            }
                        });
                    }

                    if (await appManager.FindByClientIdAsync("postman-2") is null)
                    {
                        await appManager.CreateAsync(new OpenIddictApplicationDescriptor
                        {
                            ClientId = "postman-2",
                            ClientSecret = "postman-secret",
                            DisplayName = "Postman-2",
                            RedirectUris = { new Uri("https://oauth.pstmn.io/v1/callback") },
                            Permissions =
                            {
                                Permissions.Endpoints.Authorization,
                                Permissions.Endpoints.Token,

                                Permissions.GrantTypes.AuthorizationCode,
                                Permissions.GrantTypes.ClientCredentials,

                                Permissions.Scopes.Email,
                                Permissions.Scopes.Profile,
                                Permissions.Scopes.Roles,

                                Permissions.Prefixes.Scope + "api2",

                                Permissions.ResponseTypes.Code,
                            }
                        });
                    }

                    if (await appManager.FindByClientIdAsync("resource_server_1") is null)
                    {
                        await appManager.CreateAsync(new OpenIddictApplicationDescriptor
                        {
                            ClientId = "resource_server_1",
                            ClientSecret = "846B62D0-DEF9-4215-A99D-86E6B8DAB342",
                            Permissions =
                            {
                                Permissions.Endpoints.Introspection
                            }
                        });
                    }

                    // Note: no client registration is created for resource_server_2
                    // as it uses local token validation instead of introspection.
                    if (await scopeManager.FindByNameAsync("api1") is null)
                    {
                        await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                        {
                            Name = "api1",
                            Resources =
                            {
                                "resource_server_1"
                            }
                        });
                    }

                    if (await scopeManager.FindByNameAsync("api2") is null)
                    {
                        await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                        {
                            Name = "api2",
                            Resources =
                            {
                                "resource_server_2"
                            }
                        });
                    }
                }


            }).GetAwaiter().GetResult();

        }
    }
}
