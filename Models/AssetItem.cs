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

		// Set for documents only; null for every other data type.
		[JsonPropertyName("document")]
		public DocumentInfo Document { get; set; }

		public bool IsVector => DataType == "vector";
		public bool IsRaster => DataType == "raster";
		public bool IsDocument => DataType == "document";

		// The catalog records the source PDF, so GET /api/catalog/assets/{id}/pdf can serve it.
		public bool HasPdf => IsDocument && Document?.Pdf == true;
	}

	// What a document carries beyond the columns every asset shares.
	internal class DocumentInfo
	{
		// The title the parser read from the PDF. Often missing, or just a heading.
		[JsonPropertyName("title")]
		public string Title { get; set; }

		[JsonPropertyName("pages")]
		public int? Pages { get; set; }

		[JsonPropertyName("pdf")]
		public bool Pdf { get; set; }
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
