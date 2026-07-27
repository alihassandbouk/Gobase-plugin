using System.Windows.Input;
using System.Windows.Media.Imaging;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace Rasid.Pages.Solutions
{
	internal class SolutionCardViewModel : ViewModelBase
	{
		public int Id { get; set; }

		public string Slug { get; set; }//internal key for solution, used in API calls

		public string Name { get; set; }

		public string DescriptionHtml { get; set; }

		public string ImageUrl { get; set; }

		public string ResultImageUrl { get; set; }

		public double EuroPerKm2 { get; set; }

		public string Status { get; set; }

		private BitmapImage _thumbnail;

		public BitmapImage Thumbnail
		{
			get => _thumbnail;
			set
			{
				_thumbnail = value;
				NotifyPropertyChanged();
			}
		}

		public string StatusLabel => Status switch
		{
			"prod" => "Available",
			"beta" => "Beta",
			"dev" => "Coming Soon",
			_ => Status
		};

		public bool IsAvailable => Status == "prod" || Status == "beta";

		public string DescriptionText
		{
			get
			{
				if (string.IsNullOrEmpty(DescriptionHtml))
					return string.Empty;
				//remove html tags and decode html entities
				var text = System.Text.RegularExpressions.Regex.Replace(
					DescriptionHtml, 
					"<.*?>", 
					string.Empty);
				text = System.Net.WebUtility.HtmlDecode(text);

				text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

				return text;
			}
		}

		public string ShortDescriptionText
		{
			get
			{
				var fullText = DescriptionText;
				if (string.IsNullOrEmpty(fullText))
					return string.Empty;

				const int maxLength = 150;
				if (fullText.Length <= maxLength)
					return fullText;

				var shortened = fullText.Substring(0, maxLength);
				var lastPeriod = shortened.LastIndexOf('.');
				if (lastPeriod > 50)
					return fullText.Substring(0, lastPeriod + 1);

				var lastSpace = shortened.LastIndexOf(' ');
				if (lastSpace > 0)
					return fullText.Substring(0, lastSpace) + "...";

				return shortened + "...";
			}
		}

		public bool HasLongDescription => !string.IsNullOrEmpty(DescriptionText) && DescriptionText.Length > 150;

		private bool _isDescriptionExpanded;
		public bool IsDescriptionExpanded
		{
			get => _isDescriptionExpanded;
			set
			{
				if (SetProperty(ref _isDescriptionExpanded, value))
				{
					NotifyPropertyChanged(nameof(ShowMoreText));
				}
			}
		}

		public string ShowMoreText => IsDescriptionExpanded ? "Show less" : "Show more";

		public string PriceText => EuroPerKm2 > 0 
			? $"€{EuroPerKm2:F2} per km²" 
			: "Free";

		private ICommand _createProjectCommand;
		public ICommand CreateProjectCommand
		{
			get => _createProjectCommand;
			set => SetProperty(ref _createProjectCommand, value);
		}

		private ICommand _toggleDescriptionCommand;
		public ICommand ToggleDescriptionCommand => 
			_toggleDescriptionCommand ??= new RelayCommand(() => IsDescriptionExpanded = !IsDescriptionExpanded);
	}
}