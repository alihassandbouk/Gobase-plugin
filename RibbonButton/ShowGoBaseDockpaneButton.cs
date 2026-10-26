using ArcGIS.Desktop.Framework.Contracts;

namespace GoBase.RibbonButton
{
	internal class ShowGoBaseDockpaneButton : Button
	{
		protected override void OnClick()
		{
			try
			{
				System.Diagnostics.Debug.WriteLine("GoBase button clicked");
				Dockpane.GoBaseDockpaneViewModel.Show();
			}
			catch (System.Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error showing dockpane: {ex.Message}\nStack: {ex.StackTrace}");
				ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Error opening the GoBase panel:\n{ex.Message}", "Error");
			}
		}

		protected override void OnUpdate()
		{
			this.Enabled = true;
		}
	}
}