using System;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GoBase.Models;

namespace GoBase.Services
{
	internal static class MapExtent
	{
		public static bool HasActiveMap => MapView.Active?.Map != null;

		// The active map's visible area in longitude/latitude, or null when no map is open.
		public static Task<GeoBox> GetCurrentAsync()
		{
			var mapView = MapView.Active;
			if (mapView?.Map == null)
				return Task.FromResult<GeoBox>(null);

			// Geometry calls must run on Pro's map thread (QueuedTask).
			return QueuedTask.Run(() =>
			{
				var extent = mapView.Extent;
				if (extent == null || extent.IsEmpty)
					return null;

				var projected = GeometryEngine.Instance.Project(extent, SpatialReferences.WGS84);
				var box = projected?.Extent;
				if (box == null || box.IsEmpty)
					return null;

				return GeoBox.Clamp(box.XMin, box.YMin, box.XMax, box.YMax);
			});
		}

		// Zooms the active map to an asset's bbox. A point asset has a zero-size box, so it is padded.
		public static async Task ZoomToAsync(GeoBox box)
		{
			var mapView = MapView.Active;
			if (mapView == null || box == null)
				return;

			const double pad = 0.01;
			var minLon = box.MinLon;
			var maxLon = box.MaxLon;
			var minLat = box.MinLat;
			var maxLat = box.MaxLat;
			if (maxLon - minLon < pad) { minLon -= pad; maxLon += pad; }
			if (maxLat - minLat < pad) { minLat -= pad; maxLat += pad; }

			var envelope = EnvelopeBuilderEx.CreateEnvelope(
				Math.Max(minLon, -180), Math.Max(minLat, -90),
				Math.Min(maxLon, 180), Math.Min(maxLat, 90),
				SpatialReferences.WGS84);
			await mapView.ZoomToAsync(envelope, TimeSpan.FromSeconds(0.5));
		}
	}
}
