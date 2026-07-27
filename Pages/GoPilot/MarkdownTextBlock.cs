using Rasid.Services;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace Rasid.Pages.GoPilot
{
	public class MarkdownTextBlock : TextBlock
	{
		private static readonly Regex TokenPattern = new(
			@"(?<link>\[(?<label>[^\]]+)\]\((?<linkUrl>(?:https?://|/)[^\s)]+)\))|" +
			@"(?<bold>\*\*(?<boldText>.+?)\*\*)|" +
			@"(?<code>`(?<codeText>[^`]+)`)|" +
			@"(?<italic>(?<!\*)\*(?<italicText>[^*\r\n]+)\*(?!\*))|" +
			@"(?<url>https?://[^\s<>""']+)",
			RegexOptions.Compiled | RegexOptions.IgnoreCase,
			TimeSpan.FromSeconds(1));

		public static readonly DependencyProperty MarkdownProperty =
			DependencyProperty.Register(
				nameof(Markdown),
				typeof(string),
				typeof(MarkdownTextBlock),
				new PropertyMetadata(string.Empty, OnMarkdownChanged));

		public string Markdown
		{
			get => (string)GetValue(MarkdownProperty);
			set => SetValue(MarkdownProperty, value);
		}

		private static void OnMarkdownChanged(
			DependencyObject dependencyObject,
			DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is MarkdownTextBlock block)
				block.Render(args.NewValue as string ?? string.Empty);
		}

		private void Render(string text)
		{
			Inlines.Clear();
			var position = 0;

			foreach (Match match in TokenPattern.Matches(text))
			{
				AppendText(text.Substring(position, match.Index - position));

				if (match.Groups["link"].Success)
				{
					AppendLink(
						match.Groups["label"].Value,
						match.Groups["linkUrl"].Value);
				}
				else if (match.Groups["bold"].Success)
				{
					Inlines.Add(new Bold(
						new Run(match.Groups["boldText"].Value)));
				}
				else if (match.Groups["code"].Success)
				{
					Inlines.Add(new Run(match.Groups["codeText"].Value)
					{
						FontFamily = new FontFamily("Consolas"),
						Background = new SolidColorBrush(
							Color.FromArgb(35, 80, 80, 80))
					});
				}
				else if (match.Groups["italic"].Success)
				{
					Inlines.Add(new Italic(
						new Run(match.Groups["italicText"].Value)));
				}
				else
				{
					var rawUrl = match.Groups["url"].Value;
					var clickableUrl = rawUrl.TrimEnd('.', ',', ';', ':', '!', '?');
					AppendLink(clickableUrl, clickableUrl);
					AppendText(rawUrl.Substring(clickableUrl.Length));
				}

				position = match.Index + match.Length;
			}

			AppendText(text.Substring(position));
		}

		private void AppendText(string text)
		{
			var lines = text.Replace("\r\n", "\n").Split('\n');
			for (var index = 0; index < lines.Length; index++)
			{
				if (index > 0)
					Inlines.Add(new LineBreak());
				if (lines[index].Length > 0)
					Inlines.Add(new Run(lines[index]));
			}
		}

		private void AppendLink(string label, string url)
		{
			if (!TryResolveUri(url, out var uri))
			{
				AppendText(label);
				return;
			}

			var hyperlink = new Hyperlink(new Run(label))
			{
				NavigateUri = uri,
				Cursor = System.Windows.Input.Cursors.Hand
			};
			hyperlink.SetBinding(
				ForegroundProperty,
				new Binding(nameof(Foreground)) { Source = this });
			hyperlink.RequestNavigate += (_, args) =>
			{
				Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri)
				{
					UseShellExecute = true
				});
				args.Handled = true;
			};
			Inlines.Add(hyperlink);
		}

		private static bool TryResolveUri(string url, out Uri uri)
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
