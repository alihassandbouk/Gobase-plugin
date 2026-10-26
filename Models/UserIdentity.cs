using System.Text.Json.Serialization;

namespace GoBase.Models
{
	// GET /api/auth/me
	internal class UserIdentity
	{
		[JsonPropertyName("username")]
		public string Username { get; set; }

		// "admin" or "viewer"
		[JsonPropertyName("role")]
		public string Role { get; set; }
	}
}
