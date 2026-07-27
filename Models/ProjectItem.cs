using System.Text.Json.Serialization;

namespace Rasid.Models
{
	internal class ProjectItem
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("slug")]
		public string Slug { get; set; }

		[JsonPropertyName("title")]
		public string Title { get; set; }
	}
}
