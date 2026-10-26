using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoBase.Models
{
	// GET /api/map/assets/{asset_id}/data
	// Not a GeoJSON FeatureCollection: there is no top-level "type", and paging adds next_cursor.
	internal class FeaturePage
	{
		[JsonPropertyName("asset_id")]
		public string AssetId { get; set; }

		[JsonPropertyName("count")]
		public int Count { get; set; }

		// Pass back as ?cursor= for the next page; null on the last page.
		[JsonPropertyName("next_cursor")]
		public long? NextCursor { get; set; }

		// GeoJSON Features in EPSG:4326, kept raw so they can be written out unchanged.
		[JsonPropertyName("features")]
		public List<JsonElement> Features { get; set; } = new();
	}
}
