using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

public class RemoveCookieTransformFactory : ITransformFactory
{
  private ILogger<TokenHandler> logger;

  public RemoveCookieTransformFactory(ILogger<TokenHandler> logger)
  {
    this.logger = logger;
  }

  public bool Validate(TransformRouteValidationContext context, IReadOnlyDictionary<string, string> transformValues)
  {
    if (transformValues.TryGetValue("RemoveCookie", out var value))
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
    if (transformValues.TryGetValue("RemoveCookie", out var value))
    {
      if (string.IsNullOrEmpty(value))
      {
        throw new ArgumentException("A non-empty RemoveCookie value is required");
      }
      context.AddRequestTransform(ctx =>
      {
        var headers = ctx.ProxyRequest.Headers;
        headers.Remove("Cookie");
        return default;
      });

      return true;
    }

    return false;
  }
}
