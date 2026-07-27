using System;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace Rasid.Services.Geometry
{

    internal class AoiSketchTool : MapTool //inherits from MapTool, which is a base class for creating custom tools in ArcGIS Pro
    {
        internal const string ToolId = "Rasid_AoiSketchTool";

        internal static event Action<double, double> MeasurementsUpdated;
        internal static event Action<Polygon> PolygonFinished;
        internal static event Action SketchCancelled;
        internal static event Action ToolDeactivated;
		private static IDisposable? _aoiOverlay;

		private bool _sketchHandled;

        public AoiSketchTool()
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Polygon;
            SketchOutputMode = SketchOutputMode.Map;
        }

        protected override Task OnToolActivateAsync(bool hasMapViewChanged)
        {
            _sketchHandled = false;
            return Task.CompletedTask;
        }

		protected override async Task<bool> OnSketchCompleteAsync(
	  ArcGIS.Core.Geometry.Geometry geometry)
		{
			if (geometry is not ArcGIS.Core.Geometry.Polygon polygon ||
				polygon.PointCount < 3)
			{
				return false;
			}

			/*
			 * Keep two geometries:
			 *
			 * polygon:
			 * Used for displaying the AOI on the current map.
			 *
			 * projected:
			 * Converted to WGS84 and sent to GoPilot/API.
			 */
			var projected = await QueuedTask.Run(() =>
				GeometryEngine.Instance.Project(
					polygon,
					SpatialReferences.WGS84
				) as ArcGIS.Core.Geometry.Polygon);

			if (projected == null)
				return false;

			await QueuedTask.Run(() =>
			{
				var mapView = MapView.Active;

				if (mapView == null)
					return;

				//Remove the previously drawn AOI.
				_aoiOverlay?.Dispose();
				_aoiOverlay = null;

				var outline = SymbolFactory.Instance.ConstructStroke(
					ColorFactory.Instance.GreenRGB,
					2.5,
					SimpleLineStyle.Solid);

				//Transparent interior with a visible outline.
				var polygonSymbol =
					SymbolFactory.Instance.ConstructPolygonSymbol(
						null,
						outline);

				_aoiOverlay = mapView.AddOverlay(
					polygon,
					polygonSymbol.MakeSymbolReference());
			});

			_sketchHandled = true;

			PolygonFinished?.Invoke(projected);

			return true;
		}

		
		public static void ClearAoiOverlay()
		{
			_aoiOverlay?.Dispose();
			_aoiOverlay = null;
		}
		protected override Task<bool> OnSketchCanceledAsync()
        {
            _sketchHandled = true;
            SketchCancelled?.Invoke();
            return Task.FromResult(true);
        }

        protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
        {
            // Also clean up when the user switches to another ArcGIS Pro tool
            // without explicitly pressing the toolbar Cancel button.
            if (!_sketchHandled)
            {
                _sketchHandled = true;
                ToolDeactivated?.Invoke();
            }

            return Task.CompletedTask;
        }
    }
}
