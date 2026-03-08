using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using TimecardApp.Models;

namespace TimecardApp.Pages;

[QueryProperty(nameof(EmployeeId), "employeeId")]
public partial class Timecard : ContentPage, INotifyPropertyChanged
{
	private readonly HttpClient _httpClient;
#if ANDROID
	// Android emulator uses 10.0.2.2 to access host machine's localhost
	private readonly string _baseUrl = "http://10.0.2.2:5090";
#else
	private readonly string _baseUrl = "http://localhost:5090";
#endif
	private int _employeeId;
	private string _totalHoursThisWeek = "0h 0m";
	private decimal _hourlyRate = 0;
	private string _estimatedPaycheck = "$0.00";

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

	public string TotalHoursThisWeek
	{
		get => _totalHoursThisWeek;
		set
		{
			if (_totalHoursThisWeek != value)
			{
				_totalHoursThisWeek = value;
				OnPropertyChanged(nameof(TotalHoursThisWeek));
			}
		}
	}

	public string EstimatedPaycheck
	{
		get => _estimatedPaycheck;
		set
		{
			if (_estimatedPaycheck != value)
			{
				_estimatedPaycheck = value;
				OnPropertyChanged(nameof(EstimatedPaycheck));
			}
		}
	}

	public ObservableCollection<TimeEntryDisplay> TimeEntries { get; set; } = new();

	public Timecard()
	{
		InitializeComponent();
		BindingContext = this;

		var handler = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
		};

		_httpClient = new HttpClient(handler)
		{
			BaseAddress = new Uri(_baseUrl),
			Timeout = TimeSpan.FromSeconds(30)
		};
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		// Check if user is logged in
		if (!Preferences.Get("IsLoggedIn", false))
		{
			await Shell.Current.GoToAsync("//MainPage");
			return;
		}

		// ALWAYS reload from Preferences to ensure correct user data
		var storedEmployeeId = Preferences.Get("EmployeeId", 0);

		// If stored ID is different, refresh everything
		if (storedEmployeeId != _employeeId || storedEmployeeId == 0)
		{
			System.Diagnostics.Debug.WriteLine($"Timecard: User changed! Old ID: {_employeeId}, New ID: {storedEmployeeId}");
			_employeeId = storedEmployeeId;
			_hourlyRate = 0; // Reset rate to force reload
			TimeEntries.Clear(); // Clear old user's entries
		}
		else
		{
			System.Diagnostics.Debug.WriteLine($"Timecard: Same user, ID: {_employeeId}");
		}

		if (_employeeId > 0)
		{
			await LoadEmployeeRate();
			await LoadCurrentWeekTimeEntries();
		}
		else
		{
			System.Diagnostics.Debug.WriteLine("Timecard: No employee ID available");
			TimeEntries.Clear();
			TotalHoursThisWeek = "0h 0m";
			EstimatedPaycheck = "$0.00";
		}
	}

	private async Task LoadEmployeeRate()
	{
		try
		{
			var response = await _httpClient.GetAsync($"/api/employees/{_employeeId}");
			if (response.IsSuccessStatusCode)
			{
				var employee = await response.Content.ReadFromJsonAsync<Employee>();
				if (employee != null)
				{
					_hourlyRate = employee.HourlyRate;
					System.Diagnostics.Debug.WriteLine($"Loaded hourly rate: ${_hourlyRate}");
				}
			}
			else
			{
				System.Diagnostics.Debug.WriteLine($"Failed to load employee rate: {response.StatusCode}");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Exception loading employee rate: {ex.Message}");
		}
	}

	private async Task LoadCurrentWeekTimeEntries()
	{
		try
		{
			var response = await _httpClient.GetAsync($"/api/timeentries/employee/{_employeeId}");
			if (response.IsSuccessStatusCode)
			{
				var entries = await response.Content.ReadFromJsonAsync<List<TimeEntry>>() ?? new List<TimeEntry>();

				// Get the start of the current week (Monday)
				var today = DateTime.Today;
				var daysSinceMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
				var monday = today.AddDays(-daysSinceMonday);
				var sunday = monday.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);

				var mondayUnix = new DateTimeOffset(monday).ToUnixTimeSeconds();
				var sundayUnix = new DateTimeOffset(sunday).ToUnixTimeSeconds();

				// Filter entries for current week
				var currentWeekEntries = entries
					.Where(e => e.ClockIn >= mondayUnix && e.ClockIn <= sundayUnix)
					.OrderByDescending(e => e.ClockIn)
					.Select(e => new TimeEntryDisplay
					{
						Id = e.Id,
						ClockInTime = DateTimeOffset.FromUnixTimeSeconds(e.ClockIn).ToLocalTime().ToString("ddd MM/dd hh:mm tt"),
						ClockOutTime = e.ClockOut.HasValue 
							? DateTimeOffset.FromUnixTimeSeconds(e.ClockOut.Value).ToLocalTime().ToString("ddd MM/dd hh:mm tt")
							: "Still clocked in",
						Duration = e.ClockOut.HasValue 
							? CalculateDuration(e.ClockIn, e.ClockOut.Value)
							: "In progress",
						Notes = e.Notes ?? "",
						ApprovalStatus = e.IsApproved ? "✓ Approved" : "⏳ Pending",
						ApprovalColor = e.IsApproved ? Colors.Green : Colors.Orange
					})
					.ToList();

				// Calculate total hours for the week (only completed entries)
				var completedEntries = entries
					.Where(e => e.ClockIn >= mondayUnix && e.ClockIn <= sundayUnix && e.ClockOut.HasValue);

				long totalSeconds = 0;
				foreach (var entry in completedEntries)
				{
					totalSeconds += entry.ClockOut!.Value - entry.ClockIn;
				}

				var totalDuration = TimeSpan.FromSeconds(totalSeconds);
				TotalHoursThisWeek = $"{(int)totalDuration.TotalHours}h {totalDuration.Minutes}m";

				// Calculate estimated paycheck
				var totalHoursDecimal = (decimal)totalDuration.TotalHours;
				var estimatedPay = totalHoursDecimal * _hourlyRate;
				EstimatedPaycheck = $"${estimatedPay:F2}";

				TimeEntries.Clear();
				foreach (var entry in currentWeekEntries)
				{
					TimeEntries.Add(entry);
				}
			}
			else
			{
				System.Diagnostics.Debug.WriteLine($"API Error: {response.StatusCode}");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Exception loading time entries: {ex.Message}");
		}
	}

	private string CalculateDuration(long clockIn, long clockOut)
	{
		var duration = TimeSpan.FromSeconds(clockOut - clockIn);
		return $"{(int)duration.TotalHours}h {duration.Minutes}m";
	}
}

public class TimeEntryDisplay
{
	public int Id { get; set; }
	public string ClockInTime { get; set; } = string.Empty;
	public string ClockOutTime { get; set; } = string.Empty;
	public string Duration { get; set; } = string.Empty;
	public string Notes { get; set; } = string.Empty;
	public string ApprovalStatus { get; set; } = string.Empty;
	public Color ApprovalColor { get; set; } = Colors.Gray;
}