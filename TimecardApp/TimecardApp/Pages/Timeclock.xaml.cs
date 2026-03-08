using System.Net.Http.Json;
using TimecardApp.Models;

namespace TimecardApp.Pages;

[QueryProperty(nameof(EmployeeId), "employeeId")]
[QueryProperty(nameof(FirstName), "firstName")]
public partial class Timeclock : ContentPage
{
	private readonly HttpClient _httpClient;
#if ANDROID
	// Android emulator uses 10.0.2.2 to access host machine's localhost
	private readonly string _baseUrl = "http://10.0.2.2:5090";
#else
	private readonly string _baseUrl = "http://localhost:5090";
#endif
	private int _employeeId;
	private string _firstName = string.Empty;
	private TimeEntry? _activeTimeEntry;

	public int EmployeeId
	{
		get => _employeeId;
		set
		{
			_employeeId = value;
			if (_employeeId == 0)
			{
				// If no employee ID is provided via navigation, get it from Preferences
				_employeeId = Preferences.Get("EmployeeId", 0);
			}
		}
	}

	public string FirstName
	{
		get => _firstName;
		set
		{
			_firstName = value;
			if (string.IsNullOrEmpty(_firstName))
			{
				// If no first name is provided via navigation, get it from Preferences
				var fullName = Preferences.Get("EmployeeName", string.Empty);
				_firstName = !string.IsNullOrEmpty(fullName) ? fullName.Split(' ')[0] : string.Empty;
			}
			UpdateGreeting();
		}
	}

	public Timeclock()
	{
		InitializeComponent();


		
		var handler = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
		};

		_httpClient = new HttpClient(handler)
		{
			BaseAddress = new Uri(_baseUrl),
			Timeout = TimeSpan.FromSeconds(30)
		};

		TimePunchBtn.Clicked += OnTimePunchBtnClicked;
		ViewTimecardBtn.Clicked += OnViewTimecardBtnClicked;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		// Clear any previous alert messages
		TimePunchAlert.Text = string.Empty;

		// Check if user is logged in
		if (!Preferences.Get("IsLoggedIn", false))
		{
			await Shell.Current.GoToAsync("//MainPage");
			return;
		}

		// ALWAYS reload from Preferences to ensure correct user data
		var storedEmployeeId = Preferences.Get("EmployeeId", 0);
		var storedName = Preferences.Get("EmployeeName", string.Empty);

		// If stored ID is different from current, we have a new user - refresh everything
		if (storedEmployeeId != _employeeId || storedEmployeeId == 0)
		{
			System.Diagnostics.Debug.WriteLine($"Timeclock: User changed! Old ID: {_employeeId}, New ID: {storedEmployeeId}");
			_employeeId = storedEmployeeId;
			_firstName = !string.IsNullOrEmpty(storedName) ? storedName.Split(' ')[0] : string.Empty;
			_activeTimeEntry = null; // Clear cached time entry
			UpdateGreeting();
		}
		else
		{
			System.Diagnostics.Debug.WriteLine($"Timeclock: Same user, ID: {_employeeId}");
		}

		System.Diagnostics.Debug.WriteLine($"Timeclock OnAppearing - ID: {_employeeId}, Name: {_firstName}");

		await CheckActiveTimeEntry();
	}

	private void UpdateGreeting()
	{
		if (!string.IsNullOrEmpty(_firstName))
		{
			// Find the greeting label in the XAML and update it
			var greetingLabel = this.FindByName<Label>("GreetingLabel");
			if (greetingLabel != null)
			{
				greetingLabel.Text = $"Welcome, {_firstName}! Let's start the shift!";
			}
		}
	}

	private async void OnViewTimecardBtnClicked(object? sender, EventArgs e)
	{
		// Use absolute Shell navigation to the Timecard FlyoutItem
		await Shell.Current.GoToAsync("//Timecard");
	}

	private async Task CheckActiveTimeEntry()
	{
		try
		{
			var response = await _httpClient.GetAsync($"/api/timeentries/employee/{_employeeId}");
			if (response.IsSuccessStatusCode)
			{
				var entries = await response.Content.ReadFromJsonAsync<List<TimeEntry>>() ?? new List<TimeEntry>();
				_activeTimeEntry = entries.FirstOrDefault(e => e.ClockOut == null);

				if (_activeTimeEntry != null)
				{
					TimePunchBtn.Text = "Clock Out";
					GreetingLabel.Text = $"Ready to clock out, {_firstName}?";
					NotesLabel.IsVisible = true;
					NotesEntry.IsVisible = true;
				}
				else
				{
					TimePunchBtn.Text = "Clock In";
					GreetingLabel.Text = $"Welcome, {_firstName}! Let's start the shift!";
					NotesLabel.IsVisible = false;
					NotesEntry.IsVisible = false;
				}
			}
			else
			{
				System.Diagnostics.Debug.WriteLine($"API Error: {response.StatusCode}");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Exception in CheckActiveTimeEntry: {ex.Message}");
		}
	}

	private async void OnTimePunchBtnClicked(object? sender, EventArgs e)
	{
		if (TimePunchBtn.Text == "Clock In")
		{
			await ClockIn();
		}
		else
		{
			await ClockOut();
		}
	}

	private async Task ClockIn()
	{
		try
		{
			System.Diagnostics.Debug.WriteLine($"ClockIn: Attempting to clock in for Employee ID: {_employeeId}");

			var dto = new CreateTimeEntryDto
			{
				EmployeeId = _employeeId,
				ClockIn = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				Notes = "Clocked in via app"
			};

			System.Diagnostics.Debug.WriteLine($"ClockIn DTO: EmployeeId={dto.EmployeeId}, ClockIn={dto.ClockIn}, Notes={dto.Notes}");

			var response = await _httpClient.PostAsJsonAsync("/api/timeentries", dto);

			if (response.IsSuccessStatusCode)
			{
				_activeTimeEntry = await response.Content.ReadFromJsonAsync<TimeEntry>();
				TimePunchBtn.Text = "Clock Out";
				GreetingLabel.Text = $"Ready to clock out, {FirstName}?";
				TimePunchAlert.Text = "You have successfully clocked in.";
				NotesLabel.IsVisible = true;
				NotesEntry.IsVisible = true;
				NotesEntry.Text = string.Empty;
			}
			else
			{
				var error = await response.Content.ReadAsStringAsync();
				System.Diagnostics.Debug.WriteLine($"Clock In Error: {response.StatusCode} - {error}");
				TimePunchAlert.Text = $"Error {response.StatusCode}: {error}";
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Exception in ClockIn: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
			TimePunchAlert.Text = $"Exception: {ex.Message}";
		}
	}

	private async Task ClockOut()
	{
		if (_activeTimeEntry == null) return;

		try
		{
			var notes = string.IsNullOrWhiteSpace(NotesEntry.Text) 
				? "Clocked out via app" 
				: NotesEntry.Text.Trim();

			var dto = new ClockOutDto
			{
				ClockOut = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				Notes = notes
			};

			var response = await _httpClient.PostAsJsonAsync($"/api/timeentries/{_activeTimeEntry.Id}/clock-out", dto);

			if (response.IsSuccessStatusCode)
			{
				TimePunchBtn.Text = "Clock In";
				GreetingLabel.Text = $"Welcome, {_firstName}! Let's start the shift!";
				TimePunchAlert.Text = "You have successfully clocked out.";
				NotesLabel.IsVisible = false;
				NotesEntry.IsVisible = false;
				NotesEntry.Text = string.Empty;
				_activeTimeEntry = null;
			}
			else
			{
				var error = await response.Content.ReadAsStringAsync();
				System.Diagnostics.Debug.WriteLine($"Clock Out Error: {response.StatusCode} - {error}");
				TimePunchAlert.Text = "Failed to clock out. Check backend connection.";
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Exception in ClockOut: {ex.Message}");
			TimePunchAlert.Text = $"Error: {ex.Message}";
		}
	}
}