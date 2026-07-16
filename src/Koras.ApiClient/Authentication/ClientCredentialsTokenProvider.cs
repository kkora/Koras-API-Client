using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Koras.ApiClient.Authentication;

/// <summary>
/// Acquires bearer tokens using the OAuth 2.0 client-credentials grant, caching them until shortly
/// before expiry (see <see cref="ClientCredentialsOptions.RefreshSkew"/>) with single-flight
/// refresh: concurrent callers share one token request.
/// </summary>
/// <remarks>
/// Thread-safe; register as a singleton per named client (done automatically by
/// <c>WithClientCredentials</c>). Failures surface as <see cref="ApiClientException"/> with
/// category <see cref="ApiErrorCategory.Authentication"/>, which the client normalizes into the
/// response's <see cref="ApiError"/>.
/// </remarks>
public sealed class ClientCredentialsTokenProvider : IAccessTokenProvider, IDisposable
{
    private static readonly JsonSerializerOptions TokenJsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private readonly HttpClient _httpClient;
    private readonly ClientCredentialsOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private volatile CachedToken? _cache;

    /// <summary>Initializes the provider.</summary>
    /// <param name="httpClient">The HTTP client used to call the token endpoint.</param>
    /// <param name="options">The grant configuration; endpoint, client id, and secret are required.</param>
    /// <param name="timeProvider">The clock used for expiry decisions; defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A required option is missing.</exception>
    public ClientCredentialsTokenProvider(HttpClient httpClient, ClientCredentialsOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        if (options.TokenEndpoint is null || string.IsNullOrEmpty(options.ClientId) || string.IsNullOrEmpty(options.ClientSecret))
        {
            throw new ArgumentException(
                "ClientCredentialsOptions requires TokenEndpoint, ClientId, and ClientSecret.",
                nameof(options));
        }

        _httpClient = httpClient;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<AccessToken> GetTokenAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh && TryGetCachedToken(out var cached))
        {
            return cached;
        }

        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!forceRefresh && TryGetCachedToken(out cached))
            {
                return cached;
            }

            var token = await RequestTokenAsync(cancellationToken).ConfigureAwait(false);
            _cache = new CachedToken(token);
            return token;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _refreshLock.Dispose();

    private bool TryGetCachedToken(out AccessToken token)
    {
        if (_cache is { } cache && cache.Token.ExpiresOn > _timeProvider.GetUtcNow())
        {
            token = cache.Token;
            return true;
        }

        token = default;
        return false;
    }

    private async Task<AccessToken> RequestTokenAsync(CancellationToken cancellationToken)
    {
        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("client_id", _options.ClientId!),
            new("client_secret", _options.ClientSecret!),
        };
        if (_options.Scopes.Count > 0)
        {
            form.Add(new KeyValuePair<string, string>("scope", string.Join(' ', _options.Scopes)));
        }

        foreach (var parameter in _options.AdditionalParameters)
        {
            form.Add(new KeyValuePair<string, string>(parameter.Key, parameter.Value));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(form),
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiClientException(new ApiError
            {
                Category = ApiErrorCategory.Authentication,
                Code = "token_request_failed",
                Message = "The OAuth token endpoint could not be reached.",
                Exception = ex,
            });
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiClientException(new ApiError
                {
                    Category = ApiErrorCategory.Authentication,
                    Code = "token_request_failed",
                    Message = $"The OAuth token endpoint returned HTTP {(int)response.StatusCode}.",
                    StatusCode = response.StatusCode,
                });
            }

            TokenResponse? payload;
            try
            {
                payload = await response.Content.ReadFromJsonAsync<TokenResponse>(TokenJsonOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                throw new ApiClientException(new ApiError
                {
                    Category = ApiErrorCategory.Authentication,
                    Code = "token_response_invalid",
                    Message = "The OAuth token endpoint returned a response that is not valid JSON.",
                    Exception = ex,
                });
            }

            if (payload is null || string.IsNullOrEmpty(payload.AccessToken))
            {
                throw new ApiClientException(new ApiError
                {
                    Category = ApiErrorCategory.Authentication,
                    Code = "token_response_invalid",
                    Message = "The OAuth token endpoint response did not contain an access_token.",
                });
            }

            var lifetime = TimeSpan.FromSeconds(payload.ExpiresIn > 0 ? payload.ExpiresIn : 3600);
            var expiresOn = _timeProvider.GetUtcNow() + lifetime - _options.RefreshSkew;
            return new AccessToken(payload.AccessToken, expiresOn);
        }
    }

    private sealed class CachedToken(AccessToken token)
    {
        public AccessToken Token { get; } = token;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
}
