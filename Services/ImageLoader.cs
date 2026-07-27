using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Rasid.Services
{
	internal static class ImageLoader
	{
		private static readonly string CacheDir = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Rasid", "ImageCache");

		
		public static string ResolveUrl(string url, string apiHost)
		{
			if (string.IsNullOrEmpty(url)) return null;
			if (url.StartsWith("http://") || url.StartsWith("https://")) return url;
			return url.StartsWith("/") ? apiHost + url : apiHost + "/" + url;
		}

		private static string CachePath(string url)
		{
			using var sha = SHA256.Create();
			var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(url))).ToLowerInvariant();
			var ext = Path.GetExtension(url.Split('?')[0]);
			if (string.IsNullOrEmpty(ext)) ext = ".img";
			return Path.Combine(CacheDir, hash + ext);
		}
		private static async Task<byte[]> DownloadImageBytesAsync(string url)
		{
			if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
				return null;

			var isApiEndpoint = uri.AbsolutePath.StartsWith(
				"/api/",
				StringComparison.OrdinalIgnoreCase);

			if (isApiEndpoint)
			{
				return await ApiClient.Instance.Raw.GetByteArrayAsync(uri);
			}

			using var client = new HttpClient();

			client.DefaultRequestHeaders.UserAgent.ParseAdd(
				"RASID-ArcGISPro-Addin/1.0");

			using var response = await client.GetAsync(uri);

			if (!response.IsSuccessStatusCode)
			{
				System.Diagnostics.Debug.WriteLine(
					$"Image failed: {(int)response.StatusCode} " +
					$"{response.ReasonPhrase} - {uri}");

				return null;
			}

			return await response.Content.ReadAsByteArrayAsync();
		}
		public static async Task<BitmapImage> LoadAsync(string url, string apiHost, int? width = null, int? height = null)
		{
			var resolved = ResolveUrl(url, apiHost);
			if (resolved == null) return null;

			var cachePath = CachePath(resolved);
			byte[] bytes;

			if (File.Exists(cachePath))
			{
				bytes = await File.ReadAllBytesAsync(cachePath);
			}
			else
			{
				bytes = await DownloadAsync(resolved);
				if (bytes == null) return null;
				Directory.CreateDirectory(CacheDir);
				await File.WriteAllBytesAsync(cachePath, bytes);
			}

			return CreateBitmap(bytes);
		}

		private static async Task<byte[]> DownloadAsync(string url)
		{
			try
			{
				using var response = await ApiClient.Instance.Raw.GetAsync(url);
				if (!response.IsSuccessStatusCode) return null;
				return await response.Content.ReadAsByteArrayAsync();
			}
			catch
			{
				return null; 
			}
		}

		private static BitmapImage CreateBitmap(byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0)
				return null;

			using var stream = new MemoryStream(bytes);

			var bitmap = new BitmapImage();

			bitmap.BeginInit();
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.StreamSource = stream;
			bitmap.EndInit();
			bitmap.Freeze();

			return bitmap;
		}
	}
}