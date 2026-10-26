using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GoBase.Models;

namespace GoBase.Services
{
	// A document's PDF: downloaded whole, then opened in the computer's default PDF viewer.
	internal static class DocumentOpener
	{
		// Returns the saved file's path.
		public static Task<string> DownloadPdfAsync(GoBaseApiClient api, AssetItem asset)
		{
			// One folder per asset, so two documents whose files share a name never overwrite each other.
			var folder = Path.Combine(Path.GetTempPath(), "gobase_downloads", asset.AssetId);
			var fallbackName = (string.IsNullOrWhiteSpace(asset.Name) ? asset.AssetId : asset.Name) + ".pdf";
			fallbackName = Path.GetInvalidFileNameChars().Aggregate(fallbackName, (name, c) => name.Replace(c, '_'));

			return api.DownloadDocumentPdfAsync(asset.AssetId, folder, fallbackName);
		}

		// Throws when Windows has no app registered for the file type.
		public static void Open(string path) =>
			Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
	}
}
