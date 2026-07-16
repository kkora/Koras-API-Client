namespace Koras.ApiClient;

/// <summary>Internal naming conventions shared between the DI registration and the client factory.</summary>
internal static class KorasApiClientDefaults
{
    public const string DefaultClientName = "Default";

    public static string HttpClientName(string name) => "Koras.ApiClient." + name;

    public static string TokenHttpClientName(string name) => "Koras.ApiClient.OAuth." + name;

    public static string TokenProviderKey(string name) => "Koras.ApiClient.TokenProvider." + name;
}
