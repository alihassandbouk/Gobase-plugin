using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoBase.Models;

namespace GoBase.Services
{
	// One method per GoBase-Web endpoint the add-in uses. Paths and parameters follow backend/openapi.yaml.
	internal class GoBaseApiClient
	{
		// A feature page can take a while when the server scans a large GeoParquet file.
		private static readonly TimeSpan FeaturePageTimeout = TimeSpan.FromMinutes(2);

		// PDFs are read whole from the lake before the first byte is sent.
		private static readonly TimeSpan PdfTimeout = TimeSpan.FromMinutes(2);

		private readonly ApiClient _api;
		public GoBaseApiClient(ApiClient api) { _api = api; }

		public Task<UserIdentity> GetMeAsync() =>
			_api.GetAsync<UserIdentity>("/api/auth/me");

		public Task<AssetPage> ListAssetsAsync(
			string search, IEnumerable<string> dataTypes, GeoBox bbox, int limit, int offset)
		{
			var query = new List<string> { $"limit={limit}", $"offset={offset}" };
			if (!string.IsNullOrWhiteSpace(search))
				query.Add("q=" + Uri.EscapeDataString(search.Trim()));
			foreach (var dataType in dataTypes ?? Array.Empty<string>())
				query.Add("data_type=" + Uri.EscapeDataString(dataType));
			if (bbox != null)
				query.Add("bbox=" + bbox.ToQueryValue());

			return _api.GetAsync<AssetPage>("/api/catalog/assets?" + string.Join("&", query));
		}

		public Task<FeaturePage> GetFeaturePageAsync(string assetId, GeoBox bbox, long? cursor)
		{
			var query = new List<string>();
			if (bbox != null)
				query.Add("bbox=" + bbox.ToQueryValue());
			if (cursor != null)
				query.Add($"cursor={cursor}");

			var path = $"/api/map/assets/{Uri.EscapeDataString(assetId)}/data";
			if (query.Count > 0)
				path += "?" + string.Join("&", query);
			return _api.GetAsync<FeaturePage>(path, FeaturePageTimeout);
		}

		// The whole PDF behind a document, saved into destinationFolder. Returns the file's path.
		public Task<string> DownloadDocumentPdfAsync(string assetId, string destinationFolder, string fallbackFileName) =>
			_api.DownloadAsync(
				$"/api/catalog/assets/{Uri.EscapeDataString(assetId)}/pdf",
				destinationFolder,
				fallbackFileName,
				PdfTimeout);
	}
}
