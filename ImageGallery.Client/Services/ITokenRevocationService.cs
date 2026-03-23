using Microsoft.AspNetCore.Http;

namespace ImageGallery.Client.Services;

public interface ITokenRevocationService
{
    Task RevokeCurrentUserTokensAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}
