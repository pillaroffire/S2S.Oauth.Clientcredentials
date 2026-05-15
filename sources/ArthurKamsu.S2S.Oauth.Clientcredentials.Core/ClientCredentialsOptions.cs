using Microsoft.Extensions.Http;
using System;

namespace ArthurKamsu.S2S.Oauth.Clientcredentials.Core
{
    public class ClientCredentialsOptions : HttpClientFactoryOptions
    {
        public string EncryptedClientId { get; set; }

        public string EncryptedClientSecret { get; set; }

        public Uri TokenEndpoint { get; set; }
    }
}
