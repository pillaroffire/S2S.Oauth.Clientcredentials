namespace ArthurKamsu.S2S.Oauth.Clientcredentials.Core
{
    public interface ICredentialsDecryptor
    {
        System.Threading.Tasks.ValueTask<string> DecryptAsync(string cipherText, string name, System.Threading.CancellationToken cancellationToken);
    }
}
