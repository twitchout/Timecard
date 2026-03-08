using TimecardApp;

namespace TimecardApp.Pages;

public partial class Logout : ContentPage
{
	public Logout()
	{
		InitializeComponent();

		ConfirmLogoutBtn.Clicked += OnConfirmLogoutBtnClicked;
		CancelBtn.Clicked += OnCancelBtnClicked;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// Clear any previous alert messages
		LogoutAlert.Text = string.Empty;
	}

	private async void OnConfirmLogoutBtnClicked(object? sender, EventArgs e)
	{
		try
		{
			// Clear login preferences
			Preferences.Remove("IsLoggedIn");
			Preferences.Remove("EmployeeId");
			Preferences.Remove("EmployeeName");
			Preferences.Remove("EmployeeRole");

			System.Diagnostics.Debug.WriteLine("Logout: Preferences cleared");

			// Update flyout visibility immediately
			if (Application.Current?.Windows.Count > 0 && 
			    Application.Current.Windows[0].Page is AppShell shell)
			{
				shell.UpdateFlyoutVisibility();
				System.Diagnostics.Debug.WriteLine("Logout: Flyout visibility updated");
			}

			// Show confirmation message
			LogoutAlert.Text = "Logged out successfully!";

			// Brief pause to show the message
			await Task.Delay(500);

			// Navigate to Login page (MainPage route)
			await Shell.Current.GoToAsync("//MainPage");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
			LogoutAlert.Text = $"Error: {ex.Message}";
		}
	}

	private async void OnCancelBtnClicked(object? sender, EventArgs e)
	{
		// Navigate back to Timeclock using absolute Shell navigation
		var employeeId = Preferences.Get("EmployeeId", 0);
		var employeeName = Preferences.Get("EmployeeName", "");
		var firstName = employeeName.Split(' ')[0];

		if (employeeId > 0)
		{
			await Shell.Current.GoToAsync($"//Timeclock");
		}
	}
}
