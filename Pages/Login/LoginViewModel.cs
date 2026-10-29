using ArcGIS.Desktop.Framework;
using GoBase.Pages.Base;
using GoBase.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Windows.Input;

namespace GoBase.Pages.Login
{
    // Sign in with a username and password, as on the web. The add-in trades them for an
    // API key once and keeps only the key (AuthService.SignInAsync).
    internal class LoginViewModel : PageViewModelBase
    {
        private string _serverUrl;
        public string ServerUrl
        {
            get => _serverUrl;
            set => SetProperty(ref _serverUrl, value);
        }

        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        // Held only until the sign-in attempt ends, then cleared.
        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private bool _isPasswordVisible;
        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set => SetProperty(ref _isPasswordVisible, value);
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

        // Raised when the password is cleared, so the view can empty its PasswordBox,
        // which WPF does not let a binding do.
        public event Action PasswordCleared;

        // errorText lets the panel explain why it came back here, e.g. the server was unreachable at start-up.
        public LoginViewModel(string errorText = null)
        {
            ServerUrl = AuthService.Instance.ServerUrl;
            ErrorText = errorText;
            ToggleVisibilityCommand = new RelayCommand(() => IsPasswordVisible = !IsPasswordVisible);
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
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password))
            {
                ErrorText = "Enter your username and password.";
                return;
            }

            IsConnecting = true;
            try
            {
                await AuthService.Instance.SignInAsync(ServerUrl, Username, Password);
                LoggedIn?.Invoke();
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                ErrorText = "Incorrect username or password.";
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ErrorText = "This server cannot sign in ArcGIS Pro yet: it needs the GoBase-Web update that adds API keys.";
            }
            catch (HttpRequestException ex)
            {
                ErrorText = $"Could not reach {AuthService.Instance.ServerUrl}: {ex.Message}";
            }
            catch (Exception ex)
            {
                ErrorText = $"Could not connect to GoBase: {ex.Message}";
            }
            finally
            {
                // On failure, make sure no half-configured key is left in use.
                if (ErrorText != null)
                    AuthService.Instance.Use(ServerUrl, null);

                Password = null;
                PasswordCleared?.Invoke();
                IsConnecting = false;
            }
        }
    }
}
