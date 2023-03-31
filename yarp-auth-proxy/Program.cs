using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;

// Disable claim mapping to get claims 1:1 from the tokens
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.AddConfigFiles();

// Read config and OIDC discovery document
var config = builder.Configuration.GetGatewayConfig();
var discoService = new DiscoveryService();
var disco = await discoService.loadDiscoveryDocument(config.DiscoveryUrl);

// Configure Services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHealthChecks();
builder.AddGateway(config, disco);

// Build App and add Middleware
var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
  ForwardedHeaders = ForwardedHeaders.All
});

if (config.UseHttps)
{
  app.Use((context, next) =>
  {
    context.Request.Scheme = "https";
    return next();
  });
}

app.UseGateway();

// Start Gateway
if (string.IsNullOrEmpty(config.Url))
{
  app.Run();
}
else
{
  app.Run(config.Url);
}
