using System;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Mapping;
using Rasid.Pages.Shared;

namespace Rasid.Services.Geometry
{
    internal sealed class GeometryInputManager : IDisposable
    {
        public event Action<GeoJsonResult> GeometryReady;
        public event Action DrawStarted;
        public event Action DrawCancelled;

        private DrawingToolbarWindow _toolbarWindow;
        private DrawingToolbarViewModel _toolbarViewModel;
        private bool _isDrawing;
        private bool _disposed;// Flag to indicate if the object has been disposed

        public async Task<bool> StartDrawModeAsync(string helpContext = "general")
        {
            ThrowIfDisposed();

            if (_isDrawing)
                CancelDrawMode();

            if (DrawingHelpDialogView.ShouldShow() &&
                !await DrawingHelpDialogView.ShowHelpAsync(helpContext))
            {
                return false;
            }

            if (MapView.Active == null)
            {
                MessageBox.Show(
                    "Open or activate a map before drawing an area.",
                    "RASID — No active map");
                return false;
            }

            SubscribeToSketchTool(); 

            _toolbarViewModel = new DrawingToolbarViewModel();
            _toolbarViewModel.CancelRequested += CancelDrawMode;

            _toolbarWindow = new DrawingToolbarWindow
            {
                DataContext = _toolbarViewModel,
                Owner = FrameworkApplication.Current.MainWindow
            };
            _toolbarWindow.Show();

            try
            {
                _isDrawing = true;
                await FrameworkApplicationHelpers.ActivateToolAsync(AoiSketchTool.ToolId);
                DrawStarted?.Invoke();
                return true;
            }
            catch
            {
                StopDrawMode(restoreExploreTool: false);
                throw;
            }
        }

        private void SubscribeToSketchTool()
        {
            UnsubscribeFromSketchTool();//prevent multiple subscriptions
            //subscribe to the events of the AoiSketchTool class
            AoiSketchTool.PolygonFinished += OnPolygonFinished;
            AoiSketchTool.MeasurementsUpdated += OnMeasurementsUpdated;
            AoiSketchTool.SketchCancelled += OnSketchCancelled;
            AoiSketchTool.ToolDeactivated += OnToolDeactivated;
        }

        private void UnsubscribeFromSketchTool()
        {
            //unsubscribe from the events of the AoiSketchTool class to avoid memory leaks and unintended behavior
            AoiSketchTool.PolygonFinished -= OnPolygonFinished;
            AoiSketchTool.MeasurementsUpdated -= OnMeasurementsUpdated;
            AoiSketchTool.SketchCancelled -= OnSketchCancelled;
            AoiSketchTool.ToolDeactivated -= OnToolDeactivated;
        }

        private void OnMeasurementsUpdated(double area, double perimeter)
        {
            _toolbarViewModel?.UpdateInfo(area, perimeter);
        }

        private void OnPolygonFinished(Polygon wgs84Polygon)
        {
            if (!_isDrawing || wgs84Polygon == null)
                return;

            var geojson = GeoJsonConverter.FromPolygon(wgs84Polygon);
            StopDrawMode(restoreExploreTool: true);

            GeometryReady?.Invoke(new GeoJsonResult
            {
                GeoJson = geojson,
                Source = "draw",
                Name = "Drawn Polygon"
            });
        }

        private void OnSketchCancelled()
        {
            if (!_isDrawing)
                return;

            StopDrawMode(restoreExploreTool: true);
            DrawCancelled?.Invoke();
        }

        private void OnToolDeactivated()
        {
            if (!_isDrawing)
                return;

            // The user intentionally selected another ArcGIS Pro tool, so do
            // not force the Explore tool and overwrite their selection.
            StopDrawMode(restoreExploreTool: false);
            DrawCancelled?.Invoke();
        }

        public void CancelDrawMode()
        {
            if (!_isDrawing)
                return;

            StopDrawMode(restoreExploreTool: true);
            DrawCancelled?.Invoke();
        }

        private void StopDrawMode(bool restoreExploreTool)
        {
            _isDrawing = false;
            UnsubscribeFromSketchTool();

            if (_toolbarViewModel != null)
                _toolbarViewModel.CancelRequested -= CancelDrawMode;

            _toolbarViewModel = null;

            if (_toolbarWindow != null)
            {
                _toolbarWindow.Close();
                _toolbarWindow = null;
            }

            if (restoreExploreTool)
                _ = FrameworkApplicationHelpers.DeactivateToExploreAsync();
        }

        public async Task<bool> ShowLayerPickerAsync(string title = "Select Layer")
        {
            ThrowIfDisposed();

            var dialog = new LayerPickerDialog
            {
                Title = title,
                Owner = FrameworkApplication.Current.MainWindow
            };
            var vm = new LayerPickerViewModel();
            dialog.DataContext = vm;
            await vm.LoadLayersAsync();

            if (dialog.ShowDialog() == true && vm.SelectedLayer != null)
            {
                var geojson = await GeoJsonConverter.FromLayerAsync(vm.SelectedLayer);
                GeometryReady?.Invoke(new GeoJsonResult
                {
                    GeoJson = geojson,
                    Source = "layer",
                    Name = vm.SelectedLayer.Name
                });
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_isDrawing)
                StopDrawMode(restoreExploreTool: true);
            else
                UnsubscribeFromSketchTool();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GeometryInputManager));
        }
    }

    internal sealed class GeoJsonResult
    {
        public string GeoJson { get; set; }
        public string Source { get; set; }
        public string Name { get; set; }
    }
}
