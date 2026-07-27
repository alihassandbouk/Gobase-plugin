using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rasid.Services
{
	internal static class LayerLoader
	{
		public static bool LayerExists(string layerName) =>
			MapView.Active?.Map.GetLayersAsFlattenedList()
				.Any(layer => layer.Name == layerName) ?? false;

		public static async Task LoadResultAsync(
			string filepath,
			string layerName)
		{
			if (Path.GetExtension(filepath).Equals(
				".geojson",
				System.StringComparison.OrdinalIgnoreCase))
			{
				filepath = await ConvertGeoJsonAsync(filepath, layerName);
			}

			var mapView = MapView.Active;
			if (mapView?.Map == null)
			{
				throw new System.InvalidOperationException(
					"Open a map in ArcGIS Pro before loading this result.");
			}

			await QueuedTask.Run(() =>
			{
				var map = mapView.Map;
				var group = map.GetLayersAsFlattenedList()
					.OfType<GroupLayer>()
					.FirstOrDefault(layer => layer.Name == "RASID")
					?? LayerFactory.Instance.CreateGroupLayer(
						map,
						0,
						"RASID");

				LayerFactory.Instance.CreateLayer(
					new System.Uri(filepath),
					group,
					layerName: layerName);
			});
		}

		private static async Task<string> ConvertGeoJsonAsync(
			string filepath,
			string layerName)
		{
			var geodatabasePath = Project.Current?.DefaultGeodatabasePath;
			if (string.IsNullOrWhiteSpace(geodatabasePath))
			{
				throw new System.InvalidOperationException(
					"The ArcGIS Pro project does not have a default geodatabase.");
			}

			var safeName = Regex.Replace(
				layerName ?? "gopilot_result",
				@"[^A-Za-z0-9_]",
				"_");
			if (string.IsNullOrWhiteSpace(safeName))
				safeName = "gopilot_result";
			if (char.IsDigit(safeName[0]))
				safeName = "_" + safeName;

			var outputPath = Path.Combine(
				geodatabasePath,
				$"{safeName}_{System.DateTime.UtcNow:yyyyMMddHHmmssfff}");
			var parameters = Geoprocessing.MakeValueArray(
				filepath,
				outputPath);
			var result = await Geoprocessing.ExecuteToolAsync(
				"conversion.JSONToFeatures",
				parameters,
				null,
				null,
				GPExecuteToolFlags.None);

			if (result.IsFailed)
			{
				throw new System.InvalidOperationException(
					"ArcGIS Pro could not convert the GeoJSON file to a feature class.");
			}

			return outputPath;
		}
	}
}
