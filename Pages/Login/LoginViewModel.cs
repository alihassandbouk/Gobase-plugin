using ArcGIS.Desktop.Framework;
using GoBase.Pages.Base;
using GoBase.Services;
using System;
using System.Net.Http;
using System.Windows.Input;

namespace GoBase.Pages.Login
{
    internal class LoginViewModel : PageViewModelBase
    {
        private string _serverUrl;
        public string ServerUrl
        {
            get => _serverUrl;
            set => SetProperty(ref _serverUrl, value);
        }

        private string _apiKey;
        public string ApiKey
        {
            get => _apiKey;
            set => SetProperty(ref _apiKey, value);
        }

        private bool _isKeyVisible;
        public bool IsKeyVisible
        {
            get => _isKeyVisible;
            set => SetProperty(ref _isKeyVisible, value);
        }

        private string _errorText;
        public string ErrorText
        {
            get => _errorText;
            set => SetProperty(ref _errorText, value);
        }

        private bool _isConnecting;
        public bool IsConnecting
        {
            get => _isConnecting;
            set => SetProperty(ref _isConnecting, value);
        }

        public ICommand ToggleVisibilityCommand { get; }
        public ICommand ConnectCommand { get; }

        public event Action LoggedIn;

        // errorText lets the panel explain why it came back here, e.g. the server was unreachable at start-up.
        public LoginViewModel(string errorText = null)
        {
            ServerUrl = AuthService.Instance.ServerUrl;
            ErrorText = errorText;
            ToggleVisibilityCommand = new RelayCommand(() => IsKeyVisible = !IsKeyVisible);
            ConnectCommand = new RelayCommand(async () => await ConnectAsync());
        }

        private async System.Threading.Tasks.Task ConnectAsync()
        {
            if (IsConnecting)
                return;

            ErrorText = null;
            if (string.IsNullOrWhiteSpace(ServerUrl))
            {
                ErrorText = "Enter the GoBase server address first.";
                return;
            }
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                ErrorText = "Enter your API key first.";
                return;
            }

            IsConnecting = true;
            try
            {
                AuthService.Instance.Use(ServerUrl, ApiKey);
                await AuthService.Instance.GetIdentityAsync();
                AuthService.Instance.Save();
                LoggedIn?.Invoke();
            }
            catch (ApiException ex) when (ex.IsAuthFailure)
            {
                AuthService.Instance.Use(ServerUrl, null);
                ErrorText = "The server did not accept this API key.";
            }
            catch (HttpRequestException ex)
            {
                AuthService.Instance.Use(ServerUrl, null);
                ErrorText = $"Could not reach {AuthService.Instance.ServerUrl}: {ex.Message}";
            }
            catch (Exception ex)
            {
                AuthService.Instance.Use(ServerUrl, null);
                ErrorText = $"Could not connect to GoBase: {ex.Message}";
            }
            finally
            {
                IsConnecting = false;
            }
        }
    }
}
