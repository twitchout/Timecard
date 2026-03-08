using System.Net.Http.Json;
using TimecardApp.Models;

namespace TimecardApp.Pages;

public partial class Timeclock : ContentPage
{
	private readonly HttpClient _httpClient;
	// Using PC's IP address instead of 10.0.2.2 for Android emulator connectivity
	private readonly string _baseUrl = "http://192.168.1.23:5090";
	private int _employeeId = 1; // Hardcoded for testing - change to actual employee ID
	private TimeEntry? _activeTimeEntry;

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
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await CheckActiveTimeEntry();
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
				}
				else
				{
					TimePunchBtn.Text = "Clock In";
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
			var dto = new CreateTimeEntryDto
			{
				EmployeeId = _employeeId,
				ClockIn = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				Notes = "Clocked in via app"
			};

			var response = await _httpClient.PostAsJsonAsync("/api/timeentries", dto);
			
			if (response.IsSuccessStatusCode)
			{
				_activeTimeEntry = await response.Content.ReadFromJsonAsync<TimeEntry>();
				TimePunchBtn.Text = "Clock Out";
				TimePunchAlert.Text = "You have successfully clocked in.";
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
			var dto = new ClockOutDto
			{
				ClockOut = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				Notes = "Clocked out via app"
			};

			var response = await _httpClient.PostAsJsonAsync($"/api/timeentries/{_activeTimeEntry.Id}/clock-out", dto);

			if (response.IsSuccessStatusCode)
			{
				TimePunchBtn.Text = "Clock In";
				TimePunchAlert.Text = "You have successfully clocked out.";
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