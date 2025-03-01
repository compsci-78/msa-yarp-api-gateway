using ApiGateway;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

using ApiGateway.Config;

public static class OauthProxyExtension
{
    public static void AddOAuthProxy(this IServiceCollection services)
    {
        services.AddSingleton<CookieOidcRefresher>();
        
        var proxyOptions = services.GetOptions<OAuthProxyOptions>(OAuthProxyOptions.SectionName);
        
        services.AddAuthentication(options =>
            {
                //Sets cookie authentication scheme
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(cookie =>
            {
                //Sets the cookie name and max-age, so the cookie is invalidated.
                cookie.Cookie.Name = "keycloak.cookie";
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                // cookie.SlidingExpiration = true;
                cookie.SessionStore = new RedisSessionStore(services);
            })
            .AddOpenIdConnect(options =>
            {
                //Use default signin scheme
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                options.Authority = proxyOptions.Authority;
                options.RequireHttpsMetadata = false;
                
                // options.Events = new OpenIdConnectEvents()
                // {
                //     OnRedirectToIdentityProvider = c =>
                //     {
                //         //c.NoResult();
                //         
                //         c.Response.StatusCode = 302;
                //         c.Response.ContentType = "text/plain";
                //         c.Response.Headers.Origin ="http//localhost:5173";
                //         // Debug only for security reasons
                //         // return c.Response.WriteAsync(c.Exception.ToString());
                //         // c.HandleResponse();
                //         
                //         return c.Response.WriteAsync("An error occured processing your authentication.");
                //     },
                // };
                //

                options.ClientId = proxyOptions.ClientId;
                options.ClientSecret = proxyOptions.ClientSecret;
                options.ResponseType = OpenIdConnectResponseType.Code;
                //SameSite is needed for Chrome/Firefox, as they will give http error 500 back, if not set to unspecified.
                options.NonceCookie.SameSite = SameSiteMode.Unspecified;
                options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;
                

                // options.Scope.Clear();
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");

                // options.MapInboundClaims = false; // Don't rename claim types
                // options.CallbackPath = "/signin-oidc"; // Set the callback path
                // options.SignedOutCallbackPath = ""; // Set the sign-out callback path
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateTokenReplay = true,
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles"
                };

                options.SaveTokens = true;
            });
        
        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<CookieOidcRefresher>((cookieOptions, refresher) =>
            {
                cookieOptions.Events.OnValidatePrincipal = context =>
                    refresher.ValidateOrRefreshCookieAsync(context, OpenIdConnectDefaults.AuthenticationScheme);
            });
    }

    public static void AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("authenticatedUser", policy => { policy.RequireAuthenticatedUser(); });
    }
}
