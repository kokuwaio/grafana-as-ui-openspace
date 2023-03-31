using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

public static class GatewaySetup
{
  private static readonly string ENV_GATEWAY_CONFIG = "GATEWAY_CONFIG";

  public static void AddConfigFiles(this WebApplicationBuilder builder)
  {
    var envConfig = Environment.GetEnvironmentVariable(ENV_GATEWAY_CONFIG);
    var cmdLineArgs = Environment.GetCommandLineArgs();

    if (cmdLineArgs != null && cmdLineArgs.Count() > 1)
    {
      builder.Configuration.AddJsonFile(cmdLineArgs[1], false, true);
    }
    else if (envConfig != null)
    {
      builder.Configuration.AddJsonFile(envConfig, false, true);
    }
  }

  private static void AddTokenExchangeService(this WebApplicationBuilder builder, GatewayConfig config)
  {
    var strategy = config.TokenExchangeStrategy;
    if (string.IsNullOrEmpty(strategy))
    {
      strategy = "none";
    }

    switch (strategy.ToLower())
    {
      case "none":
        builder.Services.AddSingleton<ITokenExchangeService, NullTokenExchangeService>();
        break;

      case "azuread":
        builder.Services.AddSingleton<ITokenExchangeService, AzureAdTokenExchangeService>();
        break;

      case "default":
        builder.Services.AddSingleton<ITokenExchangeService, TokenExchangeService>();
        break;

      default:
        throw new ArgumentException(
          $"Unsupported TokenExchangeStrategy in config found: {config.TokenExchangeStrategy}. Possible values: none, AzureAd, default");
    }
  }

  private static bool IsApiRequest(HttpRequest request, string[] configApiUrls)
  {
    return !string.IsNullOrEmpty(request.Path.Value) && configApiUrls.Any(i => request.Path.Value.StartsWith(i));
  }

  private static bool IsAjaxRequest(HttpRequest request)
  {
    if (request == null)
      throw new ArgumentNullException(nameof(request));

    return !string.IsNullOrEmpty(request.Headers["X-Requested-With"]) &&
           string.Equals(
             request.Headers["X-Requested-With"],
             "XmlHttpRequest",
             StringComparison.OrdinalIgnoreCase);
  }


  public static void AddGateway(this WebApplicationBuilder builder, GatewayConfig config, DiscoveryDocument disco)
  {
    builder.Services.AddReverseProxy()
      .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
      .AddTransformFactory<RemoveCookieTransformFactory>()
      .AddTransformFactory<GrafanaAuthProxyTransformFactory>();

    builder.Services.AddSingleton<DiscoveryDocument>(disco);
    builder.Services.AddSingleton<GatewayConfig>(config);

    builder.Services.AddSingleton<TokenRefreshService>();
    builder.AddTokenExchangeService(config);

    builder.Services.AddSingleton<ApiTokenService>();
    builder.Services.AddSingleton<GatewayService>();
    builder.Services.AddSingleton<TokenHandler>();

    var sessionTimeoutInMin = config.SessionTimeoutInMin;
    builder.Services.AddSession(options => { options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeoutInMin); });

    builder.Services.AddAntiforgery(setup => { setup.HeaderName = "X-XSRF-TOKEN"; });

    builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    builder.Services.AddSingleton<DiscoveryService>();

    builder.Services.AddAuthorization(options =>
    {
      options.AddPolicy("authPolicy", policy => { policy.RequireAuthenticatedUser(); });
    });

    builder.Services.AddAuthentication(options =>
      {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
      })
      .AddCookie(setup =>
      {
        setup.ExpireTimeSpan = TimeSpan.FromMinutes(sessionTimeoutInMin);
        setup.SlidingExpiration = true;
        setup.Cookie.IsEssential = true;
        setup.Cookie.Name = ".inoa-frontend";
        // setup.DataProtectionProvider = DataProtectionProvider.Create("inoa-frontend");
        setup.Events.OnValidatePrincipal += context =>
        {
          var token = context.HttpContext.Session.GetString(SessionKeys.REFRESH_TOKEN);
          if (string.IsNullOrEmpty(token))
          {
            context.RejectPrincipal();
          }

          return Task.FromResult(0);
        };
      })
      .AddOpenIdConnect(options =>
      {
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.Authority = config.Authority;
        options.ClientId = config.ClientId;
        options.UsePkce = true;
        options.ClientSecret = config.ClientSecret;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = config.QueryUserInfoEndpoint;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.RequireHttpsMetadata = false;
        options.BackchannelHttpHandler = new CustomHttpMessageHandler(config.Authority, config.DiscoveryUrl);

        options.Events.OnRedirectToIdentityProvider = (n) =>
        {
          // Override the redirect URI to be what you want
          if (n.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication)
          {
            var conf = n.HttpContext.RequestServices.GetRequiredService<GatewayConfig>();
            if (IsAjaxRequest(n.Request) || IsApiRequest(n.Request, conf.ApiUrls))
            {
              n.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
              n.HandleResponse();
              return Task.FromResult(0);
            }
          }

          return Task.FromResult(0);
        };

        var scopes = config.Scopes;
        var scopeArray = scopes.Split(" ");
        foreach (var scope in scopeArray)
        {
          options.Scope.Add(scope);
        }

        options.Events.OnTokenValidated = (context) =>
        {
          var tokenHandler = context.HttpContext.RequestServices.GetRequiredService<TokenHandler>();
          tokenHandler.HandleToken(context);
          return Task.FromResult(0);
        };

        options.Events.OnRedirectToIdentityProviderForSignOut = (context) =>
        {
          LogoutHandler.HandleLogout(context, config);
          return Task.CompletedTask;
        };
      });
  }

  private static void UseYarp(this WebApplication app)
  {
    app.MapReverseProxy(pipeline => { pipeline.UseGatewayPipeline(); });
  }

  public static void UseGateway(this WebApplication app)
  {
    app.UseRouting();
    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseCookiePolicy();
    // app.UseXsrfCookie();
    app.UseGatewayEndpoints();
    app.UseYarp();
  }
}
