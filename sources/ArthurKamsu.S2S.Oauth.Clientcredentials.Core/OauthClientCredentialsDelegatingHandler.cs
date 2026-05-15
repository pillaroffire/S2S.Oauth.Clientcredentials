using System;

namespace ArthurKamsu.S2S.Oauth.Clientcredentials.Core
{
    internal sealed class OauthClientCredentialsDelegatingHandler : System.Net.Http.DelegatingHandler
    {
        private readonly string _name;

        private readonly IAccessTokenProvider _tokenProvider;

        public OauthClientCredentialsDelegatingHandler(string name, IAccessTokenProvider tokenProvider)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
            }

            _name = name;

            _tokenProvider = tokenProvider;
        }

        protected override async System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var accessToken = await _tokenProvider.GetAccessTokenAsync(_name, cancellationToken);

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
