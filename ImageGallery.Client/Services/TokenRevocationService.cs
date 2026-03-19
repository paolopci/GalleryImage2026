using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ImageGallery.Client.Services;

public sealed class TokenRevocationService : ITokenRevocationService
{
    private const string ClientId = "imagegalleryclient";
    private const string ClientSecret = "secret";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TokenRevocationService> _logger;

    public TokenRevocationService(
        IHttpClientFactory httpClientFactory,
        ILogger<TokenRevocationService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RevokeCurrentUserTokensAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var refreshToken = await httpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);
        var accessToken = await httpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

        await RevokeTokenIfPresentAsync(
            refreshToken,
            "refresh_token",
            cancellationToken);

        await RevokeTokenIfPresentAsync(
            accessToken,
            "access_token",
            cancellationToken);
    }

    private async Task RevokeTokenIfPresentAsync(
        string? token,
        string tokenTypeHint,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogInformation("Revocation skipped: no {TokenTypeHint} available in the current session.", tokenTypeHint);
            return;
        }

        try
        {
            _logger.LogInformation("Attempting token revocation for {TokenTypeHint}.", tokenTypeHint);

            var httpClient = _httpClientFactory.CreateClient("TokenRevocationClient");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/connect/revocation");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ClientId}:{ClientSecret}")));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = token,
                ["token_type_hint"] = tokenTypeHint
            });

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Token revocation completed for {TokenTypeHint}.", tokenTypeHint);
                return;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning(
                "Token revocation failed for {TokenTypeHint}. StatusCode: {StatusCode}. Response: {ResponseBody}",
                tokenTypeHint,
                (int)response.StatusCode,
                responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token revocation failed for {TokenTypeHint}. Logout will continue.", tokenTypeHint);
        }
    }
}
