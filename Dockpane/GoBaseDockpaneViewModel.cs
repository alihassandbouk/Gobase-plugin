using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using GoBase.Pages.About;
using GoBase.Pages.Base;
using GoBase.Pages.Catalog;
using GoBase.Pages.Login;
using GoBase.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoBase.Dockpane
{
    // The panel itself. It decides which page is showing: Login, Catalog or About.
    internal class GoBaseDockpaneViewModel : DockPane
    {
        //id declared in config.daml
        private const string DockPaneId = "GoBase_DockpaneID";

        public static void Show()
        {
            var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            if (pane == null)
                throw new InvalidOperationException($"DockPane '{DockPaneId}' could not be found. Check Config.daml.");

            pane.Activate();
        }

        private PageViewModelBase _currentPage;
        public PageViewModelBase CurrentPage
        {
            get => _currentPage;
            set
            {
                if (ReferenceEquals(_currentPage, value))
                    return;

                if (_currentPage is IDisposable disposable)
                    disposable.Dispose();

                SetProperty(ref _currentPage, value);
            }
        }

        private string _currentSection;
        public string CurrentSection
        {
            get => _currentSection;
            set => SetProperty(ref _currentSection, value);
        }

        private bool _isNavigationVisible;
        public bool IsNavigationVisible
        {
            get => _isNavigationVisible;
            set => SetProperty(ref _isNavigationVisible, value);
        }

        private string _welcomeText = "GoBase";
        public string WelcomeText
        {
            get => _welcomeText;
            set => SetProperty(ref _welcomeText, value);
        }

        // Older ArcGIS Pro versions bind this property TwoWay on every DockPane. Pro 3.7's DockPane no
        // longer declares it; keeping a writable one here avoids a binding error if a version sets it.
        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public ICommand ShowCatalogCommand { get; }
        public ICommand ShowAboutCommand { get; }
        public ICommand LogOutCommand { get; }

        public GoBaseDockpaneViewModel()
        {
            ShowCatalogCommand = new RelayCommand(ShowCatalog);
            ShowAboutCommand = new RelayCommand(() => Navigate("About", new AboutViewModel()));
            LogOutCommand = new RelayCommand(LogOut);

            Initialize();
        }

        private async void Initialize()
        {
            try
            {
                if (!AuthService.Instance.Load())
                {
                    ShowLogin();
                    return;
                }

                await CompleteLoginAsync();
            }
            catch (ApiException ex) when (ex.IsAuthFailure)
            {
                // The saved key was revoked or has expired: forget it.
                AuthService.Instance.Clear();
                ShowLogin("Your saved API key is no longer accepted. Enter a new one.");
            }
            catch (Exception ex)
            {
                // The server could not be reached. Keep the saved key; it may work again later.
                System.Diagnostics.Debug.WriteLine($"GoBase initialization failed: {ex}");
                ShowLogin($"Could not reach {AuthService.Instance.ServerUrl}: {ex.Message}");
            }
        }

        private void Navigate(string section, PageViewModelBase page)
        {
            IsNavigationVisible = true;
            CurrentSection = section;
            CurrentPage = page;
        }

        private void ShowCatalog() => Navigate("Catalog", new CatalogViewModel());

        private void ShowLogin(string errorText = null)
        {
            IsNavigationVisible = false;
            CurrentSection = "Login";
            WelcomeText = "GoBase";

            var loginVm = new LoginViewModel(errorText);
            loginVm.LoggedIn += async () =>
            {
                try
                {
                    await CompleteLoginAsync();
                }
                catch (Exception ex)
                {
                    ShowLogin($"Could not finish signing in: {ex.Message}");
                }
            };
            CurrentPage = loginVm;
        }

        // Throws when the key is refused or the server cannot be reached; callers decide what that means.
        private async Task CompleteLoginAsync()
        {
            var identity = await AuthService.Instance.GetIdentityAsync();
            WelcomeText = identity == null
                ? "GoBase"
                : $"Signed in as {identity.Username}";

            ShowCatalog();
        }

        private void LogOut()
        {
            AuthService.Instance.Clear();
            ShowLogin();
        }
    }
}
