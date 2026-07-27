using System.Text.Json.Serialization;

namespace Rasid.Models
{
	internal class GoPilotTaskStatus
	{
		[JsonPropertyName("status")]
		public string Status { get; set; }

		[JsonPropertyName("result")]
		public string ResultText { get; set; }

		[JsonPropertyName("llm_message")]
		public LlmMessage LlmMessage { get; set; }

		[JsonPropertyName("progress")]
		public double Progress { get; set; }
	}

	internal class LlmMessage
	{
		[JsonPropertyName("content")]
		public string Content { get; set; }
	}
}
