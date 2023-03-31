public class CustomHttpMessageHandler : HttpClientHandler
{
  private string _authority { get; set; }
  private string _internalAuthority { get; set; }

  public CustomHttpMessageHandler(string authority, string internalAuthority)
  {
    _authority = authority;
    _internalAuthority = internalAuthority;
  }

  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
    CancellationToken cancellationToken)
  {
    request.RequestUri = new Uri(request.RequestUri.OriginalString.Replace(_authority, _internalAuthority));
    return base.SendAsync(request, cancellationToken);
  }
}
