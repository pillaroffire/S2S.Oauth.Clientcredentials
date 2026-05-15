using ArthurKamsu.S2S.Oauth.Clientcredentials.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Net.Http;

namespace ArthurKamsu.S2S.Oauth.Clientcredentials
{
    public static partial class AuthorizedHttpClientBuilderExtensions
    {
        public static IHttpClientBuilder AddAuthorizedHttpClient<TDecryptor>(this IServiceCollection services, string name, Action<HttpClient> configureClient, Action<ClientCredentialsOptions> configureCreds)
            where TDecryptor : class, ICredentialsDecryptor
        {
            services.TryAddSingleton<IAccessTokenProvider, AccessTokenProvider>();

            services.TryAddSingleton<ICredentialsDecryptor, TDecryptor>();

            services.TryAddSingleton(TimeProvider.System);

            services.Configure(name, configureCreds);

            return services.AddHttpClient(name, configureClient)
                .AddHttpMessageHandler(sp =>
                {
                    return new OauthClientCredentialsDelegatingHandler(name, sp.GetRequiredService<IAccessTokenProvider>());
                });
        }
    }
}
