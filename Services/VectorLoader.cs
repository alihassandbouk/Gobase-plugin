using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GoBase.Models;

namespace GoBase.Services
{
	internal record VectorLoadResult(int FeatureCount, int LayerCount, bool Truncated, int Skipped);

	// Reads a vector asset from GET /api/map/assets/{id}/data page by page and adds it to the active map.
	internal static class VectorLoader
	{
		// The web map's own ceiling: at most six pages of up to 10,000 features per view.
		public const int MaxPages = 6;

		public static async Task<VectorLoadResult> LoadAsync(
			GoBaseApiClient api, AssetItem asset, GeoBox bbox, IProgress<string> progress)
		{
			// An ArcGIS layer holds one geometry type, and many GoBase assets mix them,
			// so features are sorted into one bucket per Pro geometry type.
			var buckets = new Dictionary<string, List<JsonElement>>();
			var skipped = 0;
			var pages = 0;
			long? cursor = null;

			do
			{
				progress?.Report($"Reading features, page {pages + 1}...");
				var page = await api.GetFeaturePageAsync(asset.AssetId, bbox, cursor).ConfigureAwait(false);

				foreach (var feature in page.Features)
				{
					var geometryType = ProGeometryTypeOf(feature);
					if (geometryType == null)
					{
						skipped++;
						continue;
					}

					if (!buckets.TryGetValue(geometryType, out var bucket))
						buckets[geometryType] = bucket = new List<JsonElement>();
					bucket.Add(feature);
				}

				cursor = page.NextCursor;
				pages++;
			}
			while (cursor != null && pages < MaxPages);

			var featureCount = buckets.Values.Sum(bucket => bucket.Count);
			if (featureCount == 0)
				return new VectorLoadResult(0, 0, false, skipped);

			progress?.Report($"Adding {featureCount:N0} features to the map...");
			var parts = await Task.Run(() => WriteGeoJsonFiles(asset, buckets)).ConfigureAwait(false);
			try
			{
				await LayerLoader.LoadGeoJsonPartsAsync(parts, asset.Name).ConfigureAwait(false);
			}
			finally
			{
				foreach (var part in parts)
					TryDelete(part.Path);
			}

			return new VectorLoadResult(featureCount, parts.Count, cursor != null, skipped);
		}

		// The geometry_type keyword JSONToFeatures expects, or null for features it cannot hold
		// (no geometry, or a GeometryCollection). Pro lines and polygons are multipart already.
		private static string ProGeometryTypeOf(JsonElement feature)
		{
			if (!feature.TryGetProperty("geometry", out var geometry) ||
				geometry.ValueKind != JsonValueKind.Object ||
				!geometry.TryGetProperty("type", out var type))
				return null;

			return type.GetString() switch
			{
				"Point" => "POINT",
				"MultiPoint" => "MULTIPOINT",
				"LineString" or "MultiLineString" => "POLYLINE",
				"Polygon" or "MultiPolygon" => "POLYGON",
				_ => null
			};
		}

		private static List<GeoJsonPart> WriteGeoJsonFiles(
			AssetItem asset, Dictionary<string, List<JsonElement>> buckets)
		{
			var folder = Path.Combine(Path.GetTempPath(), "gobase_downloads");
			Directory.CreateDirectory(folder);

			var parts = new List<GeoJsonPart>();
			foreach (var (geometryType, features) in buckets)
			{
				var path = Path.Combine(folder, $"{asset.AssetId}_{geometryType.ToLowerInvariant()}_{Guid.NewGuid():N}.geojson");
				using (var stream = File.Create(path))
				using (var writer = new Utf8JsonWriter(stream))
				{
					writer.WriteStartObject();
					writer.WriteString("type", "FeatureCollection");
					writer.WriteStartArray("features");
					foreach (var feature in features)
						feature.WriteTo(writer);
					writer.WriteEndArray();
					writer.WriteEndObject();
				}

				// One bucket: the layer takes the asset's name. Several: each says what it holds.
				var layerName = buckets.Count == 1 ? asset.Name : $"{asset.Name} ({LabelOf(geometryType)})";
				parts.Add(new GeoJsonPart(path, geometryType, layerName));
			}

			return parts;
		}

		private static string LabelOf(string geometryType) => geometryType switch
		{
			"POINT" => "points",
			"MULTIPOINT" => "multipoints",
			"POLYLINE" => "lines",
			"POLYGON" => "polygons",
			_ => geometryType.ToLowerInvariant()
		};

		private static void TryDelete(string path)
		{
			try { File.Delete(path); } catch { }
		}
	}
}
