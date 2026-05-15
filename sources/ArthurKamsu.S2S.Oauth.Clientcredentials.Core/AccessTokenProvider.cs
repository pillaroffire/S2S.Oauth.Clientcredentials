using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArthurKamsu.S2S.Oauth.Clientcredentials.Core
{
    public interface IAccessTokenProvider
    {
        ValueTask<string> GetAccessTokenAsync(string name, CancellationToken cancellationToken);
    }

    internal sealed class AccessTokenProvider : IAccessTokenProvider
    {
        private readonly IOptionsMonitor<ClientCredentialsOptions> _optionsMonitor;

        private readonly IHttpClientFactory _httpClientFactory;

        private readonly ILogger<AccessTokenProvider> _logger;

        private readonly ICredentialsDecryptor _encryptionService;

        private readonly TimeProvider _timeProvider;

        private readonly ConcurrentDictionary<string, (string Token, DateTimeOffset ExpiryInUtc, TimeSpan RenewalBuffer)> _tokensEntries;

        public AccessTokenProvider(
            IOptionsMonitor<ClientCredentialsOptions> optionsMonitor,
            IHttpClientFactory httpClientFactory,
            ILogger<AccessTokenProvider> logger,
            ICredentialsDecryptor credentialsDecryptor,
            TimeProvider timeProvider)
        {
            var concurrencyLevel = Environment.ProcessorCount * 2;

            _tokensEntries = new ConcurrentDictionary<string, (string Token, DateTimeOffset ExpiryInUtc, TimeSpan RenewalBuffer)>(concurrencyLevel, Constants.TokenEntriesInitialCapacity);

            _optionsMonitor = optionsMonitor;

            _httpClientFactory = httpClientFactory;

            _logger = logger;

            _encryptionService = credentialsDecryptor;

            _timeProvider = timeProvider;
        }

        public async ValueTask<string> GetAccessTokenAsync(string name, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
            }

            if (_tokensEntries.TryGetValue(name, out var tokenEntry) && (tokenEntry.ExpiryInUtc > _timeProvider.GetUtcNow().Add(tokenEntry.RenewalBuffer)))
            {
                return tokenEntry.Token;
            }

            var httpClient = _httpClientFactory.CreateClient();

            var options = _optionsMonitor.Get(name);

            using (var request = new HttpRequestMessage(HttpMethod.Post, options.TokenEndpoint))
            {
                request.Headers.Add(Constants.AuthorizationHeaderName,
                    $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(await _encryptionService.DecryptAsync(options.EncryptedClientId, name, cancellationToken) + ":" + await _encryptionService.DecryptAsync(options.EncryptedClientSecret, name, cancellationToken)))}");

                try
                {
                    using (var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            //return string.Empty;
                            return $"Error retrieving token for '{name}'. Status code: {response.StatusCode}";
                        }

                        var token = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        

                        _tokensEntries.AddOrUpdate(name, (token, DateTimeOffset.MaxValue, TimeSpan.FromSeconds(3600)), (key, oldValue) => (token, DateTimeOffset.MaxValue, TimeSpan.FromSeconds(3600)));

                        return token;
                    }
                }
                catch (Exception)
                {
                    _logger.LogError($"Error retrieving token for '{name}'. Request Auth Header: {request.Headers.Authorization} Endpoint: {request.RequestUri}");

                    return $"Error retrieving token for '{name}'. Request Auth Header: {request.Headers.Authorization} Endpoint: {request.RequestUri}";
                }
            }
        }

        internal bool TokenIsUseful((string Token, DateTime Expiry, TimeSpan RenewalBuffer) tokenEntry)
        {
            return tokenEntry.Expiry > _timeProvider.GetUtcNow().Add(tokenEntry.RenewalBuffer);
        }
    }
}
