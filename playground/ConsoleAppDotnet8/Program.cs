// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using ArthurKamsu.S2S.Oauth.Clientcredentials;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

services.AddLogging(configure => configure.AddConsole());

services.AddAuthorizedHttpClient<DummyDecryptor>("paypal",
    httpClientConfig =>
    {
        httpClientConfig.BaseAddress = new Uri("https://www.paypal.com/");
        httpClientConfig.Timeout = TimeSpan.FromSeconds(25);
    },
    clientCredentialsConfig =>
    {
        clientCredentialsConfig.TokenEndpoint = new Uri("https://localhost:5003/connect/token");
        clientCredentialsConfig.EncryptedClientId = "paypal-encrypted-client-id";
        clientCredentialsConfig.EncryptedClientSecret = "paypal-encrypted-secret";
        clientCredentialsConfig.ShouldRedactHeaderValue = value => value == "Authorization";
    });

var serviceProvider = services.BuildServiceProvider();

var paypalClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("paypal");

var response = await paypalClient.GetAsync("/v1/payments/payment");

var content = await response.Content.ReadAsStringAsync();

Console.WriteLine(content);

class DummyDecryptor : ArthurKamsu.S2S.Oauth.Clientcredentials.Core.ICredentialsDecryptor
{
    public ValueTask<string> DecryptAsync(string cipherText, string name, CancellationToken cancellationToken)
    {
        return new ValueTask<string>(cipherText);
    }
}
