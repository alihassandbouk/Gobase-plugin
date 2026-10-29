using System.Threading.Tasks;
using GoBase.Models;

namespace GoBase.Services
{
    internal sealed class AuthService
    {
        // One Windows credential holds both: the server URL as its user name, the API key as its secret.
        private const string TargetName = "gobase_addin";

        public static AuthService Instance { get; } = new AuthService();

        public string ServerUrl { get; private set; }
        public string ApiKey { get; private set; }

        private AuthService()
        {
        }

        public void Use(string serverUrl, string apiKey)
        {
            ServerUrl = ApiClient.NormalizeServerUrl(serverUrl);
            ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
            ApiClient.Instance.Configure(ServerUrl, ApiKey);
        }

        public void Save() => WindowsCredentialStore.Save(TargetName, ServerUrl, ApiKey);

        public bool Load()
        {
            if (!WindowsCredentialStore.TryRead(TargetName, out var serverUrl, out var apiKey))
                return false;

            Use(serverUrl, apiKey);
            return ServerUrl != null;
        }

        // Forgets the key but keeps the server URL in memory, so the login form can show it again.
        public void Clear()
        {
            Use(ServerUrl, null);
            WindowsCredentialStore.Delete(TargetName);
        }

        // Throws ApiException with IsAuthFailure when the key is refused, other exceptions when the server is unreachable.
        public Task<UserIdentity> GetIdentityAsync() => new GoBaseApiClient(ApiClient.Instance).GetMeAsync();

        // Signs in as on the web: the username and password are traded once for an API key
        // named after this computer, and only the key is kept. Throws ApiException 400 for
        // a wrong username or password, and 404 when the server has no such endpoint yet.
        public async Task<UserIdentity> SignInAsync(string serverUrl, string username, string password)
        {
            Use(serverUrl, null);
            var api = new GoBaseApiClient(ApiClient.Instance);
            var issued = await api.IssueApiKeyAsync(username.Trim(), password, DeviceKeyName);

            Use(ServerUrl, issued.Key);
            var identity = await api.GetMeAsync();
            Save();
            return identity;
        }

        // What the key is called on the server, so an Admin can tell the computers apart.
        private static string DeviceKeyName => $"ArcGIS Pro on {System.Environment.MachineName}";
    }
}
