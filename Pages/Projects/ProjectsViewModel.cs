using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using Rasid.Models;
using Rasid.Pages.Base;
using Rasid.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Rasid.Pages.Projects
{
    internal class ProjectsViewModel : PageViewModelBase
    {
        private readonly RasidApiClient _api;

        public ObservableCollection<ProjectCardViewModel> Projects { get; } = new();
        public event Action<ProjectItem> ProjectOpened;
        public ICommand RefreshCommand { get; }

        private string _statusText = "Loading projects...";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public ProjectsViewModel()
        {
            _api = new RasidApiClient(ApiClient.Instance);
            RefreshCommand = new RelayCommand(async () => await LoadProjectsAsync());
            _ = LoadProjectsAsync();
        }

        private async Task LoadProjectsAsync()
        {
            StatusText = "Loading projects...";
            try
            {
                var projects = await _api.GetUserProjectsAsync();
                Projects.Clear();
                foreach (var project in projects)
                {
                    var capturedProject = project;
                    Projects.Add(new ProjectCardViewModel
                    {
                        Project = capturedProject,
                        OpenCommand = new RelayCommand(() => ProjectOpened?.Invoke(capturedProject))
                    });
                }

                StatusText = Projects.Count == 0 ? "No projects found. Create one from Solutions." : null;
            }
            catch (Exception ex)
            {
                StatusText = $"Failed to load projects: {ex.Message}";
            }
        }
    }

    internal class ProjectCardViewModel : PropertyChangedBase
    {
        public ProjectItem Project { get; set; }
        public ICommand OpenCommand { get; set; }
    }
}
