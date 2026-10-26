using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GoBase.Services
{
	// One GeoJSON file holding a single geometry type, and the layer it becomes.
	internal record GeoJsonPart(string Path, string GeometryType, string LayerName);

	internal static class LayerLoader
	{
		private const string RootGroupName = "GoBase";

		// Converts each part into a feature class in the project's default geodatabase and adds it to
		// the active map, under a "GoBase" group. Several parts of one asset share a sub-group.
		public static async Task LoadGeoJsonPartsAsync(IReadOnlyList<GeoJsonPart> parts, string assetName)
		{
			var mapView = MapView.Active;
			if (mapView?.Map == null)
			{
				throw new System.InvalidOperationException(
					"Open a map in ArcGIS Pro before loading this layer.");
			}

			var featureClasses = new List<(string Path, string LayerName)>();
			foreach (var part in parts)
			{
				var featureClassPath = await ConvertGeoJsonAsync(part);
				featureClasses.Add((featureClassPath, part.LayerName));
			}

			await QueuedTask.Run(() =>
			{
				var map = mapView.Map;
				var root = map.GetLayersAsFlattenedList()
					.OfType<GroupLayer>()
					.FirstOrDefault(layer => layer.Name == RootGroupName)
					?? LayerFactory.Instance.CreateGroupLayer(map, 0, RootGroupName);

				var container = featureClasses.Count > 1
					? LayerFactory.Instance.CreateGroupLayer(root, 0, assetName)
					: root;

				foreach (var (path, layerName) in featureClasses)
				{
					LayerFactory.Instance.CreateLayer(
						new System.Uri(path),
						container,
						layerName: layerName);
				}
			});
		}

		private static async Task<string> ConvertGeoJsonAsync(GeoJsonPart part)
		{
			var geodatabasePath = Project.Current?.DefaultGeodatabasePath;
			if (string.IsNullOrWhiteSpace(geodatabasePath))
			{
				throw new System.InvalidOperationException(
					"The ArcGIS Pro project does not have a default geodatabase.");
			}

			// Geodatabase names allow letters, digits and underscores, and cannot start with a digit.
			var safeName = Regex.Replace(part.LayerName ?? "gobase_layer", @"[^A-Za-z0-9_]", "_");
			if (string.IsNullOrWhiteSpace(safeName))
				safeName = "gobase_layer";
			if (char.IsDigit(safeName[0]))
				safeName = "_" + safeName;

			var outputPath = Path.Combine(
				geodatabasePath,
				$"{safeName}_{System.DateTime.UtcNow:yyyyMMddHHmmssfff}");

			// The third parameter tells JSONToFeatures which geometry type to keep from the GeoJSON.
			var parameters = Geoprocessing.MakeValueArray(part.Path, outputPath, part.GeometryType);
			var result = await Geoprocessing.ExecuteToolAsync(
				"conversion.JSONToFeatures",
				parameters,
				null,
				null,
				GPExecuteToolFlags.None);

			if (result.IsFailed)
			{
				var messages = string.Join("\n", result.ErrorMessages.Select(message => message.Text));
				throw new System.InvalidOperationException(
					$"ArcGIS Pro could not convert the features ({part.GeometryType}).\n{messages}");
			}

			return outputPath;
		}
	}
}
