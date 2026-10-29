using System;
using System.Text.Json.Serialization;

namespace GoBase.Models
{
	// POST /api/auth/api-keys: the key a username and password were traded for.
	internal class ApiKeyIssued
	{
		// Shown only in this response; the add-in keeps it in Windows Credential Manager.
		[JsonPropertyName("key")]
		public string Key { get; set; }

		[JsonPropertyName("prefix")]
		public string Prefix { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("created_at")]
		public DateTime CreatedAt { get; set; }
	}
}
