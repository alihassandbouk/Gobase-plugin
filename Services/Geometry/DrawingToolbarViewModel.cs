// DrawingToolbarViewModel.cs
using System;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace Rasid.Services.Geometry
{
	internal class DrawingToolbarViewModel : PropertyChangedBase
	{
		private string _infoText = "Click to add points";
		public string InfoText { get => _infoText; set => SetProperty(ref _infoText, value); }

		public ICommand CancelCommand { get; }
		public event Action CancelRequested;

		public DrawingToolbarViewModel()
		{
			CancelCommand = new RelayCommand(() => CancelRequested?.Invoke());
		}

		public void UpdateInfo(double areaSqM, double perimeterM)
		{
			string Format(double v, string small, double divisor, string big) =>
				v < divisor ? $"{v:F1} {small}" : $"{v / divisor:F2} {big}";

			var areaText = Format(areaSqM, "m²", 1_000_000, "km²");
			var perimeterText = Format(perimeterM, "m", 1000, "km");
			InfoText = $"Area: {areaText}\nPerimeter: {perimeterText}";
		}
	}
}