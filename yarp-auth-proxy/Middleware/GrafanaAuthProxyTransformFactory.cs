using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

public class GrafanaAuthProxyTransformFactory : ITransformFactory
{
  private ILogger<TokenHandler> logger;

  public GrafanaAuthProxyTransformFactory(ILogger<TokenHandler> logger)
  {
    this.logger = logger;
  }

  public bool Validate(TransformRouteValidationContext context, IReadOnlyDictionary<string, string> transformValues)
  {
    if (transformValues.TryGetValue("GrafanaAuthProxy", out var value))
    {
      if (string.IsNullOrEmpty(value))
      {
        context.Errors.Add(new ArgumentException("A non-empty GrafanaAuthProxy value is required"));
      }

      return true; // Matched
    }

    return false;
  }

  public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
  {
    if (transformValues.TryGetValue("GrafanaAuthProxy", out var value))
    {
      if (string.IsNullOrEmpty(value))
      {
        throw new ArgumentException("A non-empty GrafanaAuthProxy value is required");
      }

      context.AddRequestTransform(ctx =>
      {
        var firstOrDefault = ctx.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "email");
        var headers = ctx.ProxyRequest.Headers;
        if (firstOrDefault != null)
        {
          headers.Remove("Authorization");
          headers.TryAddWithoutValidation("X-WEBAUTH-USER", firstOrDefault.Value);
          headers.TryAddWithoutValidation("X-WEBAUTH-ROLE", "Admin");
        }
        return default;
      });

      return true;
    }

    return false;
  }
}
