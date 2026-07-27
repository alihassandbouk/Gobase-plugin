using Rasid.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Rasid.Services
{
	internal static class GoPilotFileLinkParser
	{
		private static readonly HashSet<string> SupportedExtensions =
			new(StringComparer.OrdinalIgnoreCase)
			{
				"tif", "tiff", "png", "jpg", "jpeg",
				"shp", "geojson", "gpkg", "zip"
			};

		private static readonly HashSet<string> ImageExtensions =
			new(StringComparer.OrdinalIgnoreCase)
			{
				"png", "jpg", "jpeg"
			};

		private static readonly Regex LinkPattern = new(
			@"\[[^\]]*\]\((?<markdown>(?:https?://|/)[^\s)]+)\)|(?<plain>https?://[^\s<>""']+)",
			RegexOptions.Compiled | RegexOptions.IgnoreCase,
			TimeSpan.FromSeconds(1));

		public static IReadOnlyList<ChatFileAttachment> Extract(string text)
		{
			var files = new List<ChatFileAttachment>();
			if (string.IsNullOrWhiteSpace(text))
				return files;

			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (Match match in LinkPattern.Matches(text))
			{
				var rawUrl = match.Groups["markdown"].Success
					? match.Groups["markdown"].Value
					: match.Groups["plain"].Value.TrimEnd('.', ',', ';', ':', '!', '?');

				if (!TryCreateAbsoluteUri(rawUrl, out var uri))
					continue;

				var extension = Path.GetExtension(uri.AbsolutePath)
					.TrimStart('.');
				if (!SupportedExtensions.Contains(extension) ||
					!seen.Add(uri.AbsoluteUri))
					continue;

				var fileName = Uri.UnescapeDataString(
					Path.GetFileName(uri.AbsolutePath));
				if (string.IsNullOrWhiteSpace(fileName))
					fileName = $"rasid_result.{extension}";

				var isImage = ImageExtensions.Contains(extension);
				files.Add(new ChatFileAttachment
				{
					Url = uri.AbsoluteUri,
					FileName = fileName,
					Extension = extension.ToLowerInvariant(),
					IsImage = isImage,
					ActionText = isImage
						? $"Save {fileName}"
						: $"Download {fileName}"
				});
			}

			return files;
		}

		private static bool TryCreateAbsoluteUri(string url, out Uri uri)
		{
			if (Uri.TryCreate(url, UriKind.Absolute, out uri) &&
				(uri.Scheme == Uri.UriSchemeHttp ||
				 uri.Scheme == Uri.UriSchemeHttps))
				return true;

			if (url.StartsWith("/", StringComparison.Ordinal))
			{
				uri = new Uri(
					new Uri(ApiClient.Instance.ApiHost),
					url);
				return true;
			}

			uri = null;
			return false;
		}
	}
}
