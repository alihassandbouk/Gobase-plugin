using System.Threading.Tasks;
using System.Windows;

namespace Rasid.Pages.Shared
{
	internal static class DrawingHelpDialogView
	{
		public static bool ShouldShow() => false;

		public static Task<bool> ShowHelpAsync(string context)
		{
			return Task.FromResult(true);
		}
	}
}
