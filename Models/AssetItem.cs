using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoBase.Models
{
	// One row of GET /api/catalog/assets
	internal class AssetItem
	{
		[JsonPropertyName("asset_id")]
		public string AssetId { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		// "vector", "raster", "document" or "unparsed"
		[JsonPropertyName("data_type")]
		public string DataType { get; set; }

		// "geoparquet", "cog", "pdf", ...
		[JsonPropertyName("format")]
		public string Format { get; set; }

		[JsonPropertyName("dataset")]
		public string Dataset { get; set; }

		[JsonPropertyName("dataset_label")]
		public string DatasetLabel { get; set; }

		[JsonPropertyName("bytes")]
		public long? Bytes { get; set; }

		// [minLon, minLat, maxLon, maxLat] in EPSG:4326, or null
		[JsonPropertyName("bbox")]
		public double[] Bbox { get; set; }

		[JsonPropertyName("summary")]
		public string Summary { get; set; }

		[JsonPropertyName("ingested_at")]
		public DateTime? IngestedAt { get; set; }

		public bool IsVector => DataType == "vector";
		public bool IsRaster => DataType == "raster";
		public bool IsDocument => DataType == "document";
	}

	// GET /api/catalog/assets
	internal class AssetPage
	{
		[JsonPropertyName("count")]
		public int Count { get; set; }

		[JsonPropertyName("results")]
		public List<AssetItem> Results { get; set; } = new();
	}
}
