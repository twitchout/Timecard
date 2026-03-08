using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using TimecardApp.Models;

namespace TimecardApp.Pages;

public partial class AdminPanel : ContentPage, INotifyPropertyChanged
{
	private readonly HttpClient _httpClient;
#if ANDROID
	// Android emulator uses 10.0.2.2 to access host machine's localhost
	private readonly string _baseUrl = "http://10.0.2.2:5090";
#else
	private readonly string _baseUrl = "http://localhost:5090";
#endif
	private List<Employee> _employees = new();
	private int _selectedEmployeeId = 0;

	public ObservableCollection<AdminTimeEntryDisplay> TimeEntries { get; set; } = new();

	public AdminPanel()
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

		// Check if user is admin
		var role = Preferences.Get("EmployeeRole", "").ToLower();
		if (!IsAdminRole(role))
		{
			await DisplayAlert("Access Denied", "You don't have permission to access this page.", "OK");
			await Shell.Current.GoToAsync("//Timeclock");
			return;
		}

		await LoadEmployees();
	}

	private bool IsAdminRole(string role)
	{
		var adminRoles = new[] { "admin", "administrator", "manager", "supervisor" };
		return adminRoles.Contains(role);
	}

	private async Task LoadEmployees()
	{
		try
		{
			var response = await _httpClient.GetAsync("/api/employees");
			if (response.IsSuccessStatusCode)
			{
				var allEmployees = await response.Content.ReadFromJsonAsync<List<Employee>>() ?? new List<Employee>();

				// Exclude the current admin from the list
				var currentEmployeeId = Preferences.Get("EmployeeId", 0);
				_employees = allEmployees.Where(e => e.Id != currentEmployeeId).ToList();

				EmployeePicker.ItemsSource = _employees.Select(e => e.Name).ToList();
				System.Diagnostics.Debug.WriteLine($"Loaded {_employees.Count} employees (excluding current admin)");
			}
			else
			{
				await DisplayAlert("Error", "Failed to load employees", "OK");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error loading employees: {ex.Message}");
			await DisplayAlert("Error", $"Failed to load employees: {ex.Message}", "OK");
		}
	}

	private async void OnEmployeeSelected(object? sender, EventArgs e)
	{
		if (EmployeePicker.SelectedIndex < 0)
		{
			System.Diagnostics.Debug.WriteLine("No employee selected (index < 0)");
			return;
		}

		System.Diagnostics.Debug.WriteLine($"Employee selected at index: {EmployeePicker.SelectedIndex}");

		var selectedEmployee = _employees[EmployeePicker.SelectedIndex];
		_selectedEmployeeId = selectedEmployee.Id;

		SelectedEmployeeLabel.Text = $"📋 Time Entries for {selectedEmployee.Name}";
		SelectedEmployeeLabel.FontAttributes = FontAttributes.Bold;
		SelectedEmployeeLabel.FontSize = 18;
		SelectedEmployeeLabel.IsVisible = true;
		TimeEntriesCollection.IsVisible = true;
		ApproveAllBtn.IsVisible = true;

		System.Diagnostics.Debug.WriteLine($"Loading time entries for employee: {selectedEmployee.Name} (ID: {selectedEmployee.Id})");

		await LoadEmployeeTimeEntries(selectedEmployee.Id);
	}

	private async Task LoadEmployeeTimeEntries(int employeeId)
	{
		try
		{
			System.Diagnostics.Debug.WriteLine($"Fetching time entries for employee ID: {employeeId}");
			var response = await _httpClient.GetAsync($"/api/timeentries/employee/{employeeId}");

			System.Diagnostics.Debug.WriteLine($"Response status: {response.StatusCode}");

			if (response.IsSuccessStatusCode)
			{
				var entries = await response.Content.ReadFromJsonAsync<List<TimeEntry>>() ?? new List<TimeEntry>();
				System.Diagnostics.Debug.WriteLine($"Loaded {entries.Count} time entries");

				var displayEntries = entries
					.OrderByDescending(e => e.ClockIn)
					.Select(e => new AdminTimeEntryDisplay
					{
						Id = e.Id,
						EmployeeId = e.EmployeeId,
						ClockInTime = DateTimeOffset.FromUnixTimeSeconds(e.ClockIn).ToLocalTime().ToString("ddd MM/dd/yyyy hh:mm tt"),
						ClockOutTime = e.ClockOut.HasValue 
							? DateTimeOffset.FromUnixTimeSeconds(e.ClockOut.Value).ToLocalTime().ToString("ddd MM/dd/yyyy hh:mm tt")
							: "Still clocked in",
						Duration = e.ClockOut.HasValue 
							? CalculateDuration(e.ClockIn, e.ClockOut.Value)
							: "In progress",
						Notes = e.Notes ?? "",
						IsApproved = e.IsApproved,
						ApprovalStatus = e.IsApproved ? "✓ Approved" : "⏳ Pending",
						ApprovalColor = e.IsApproved ? Colors.Green : Colors.Orange,
						CanApprove = !e.IsApproved
					})
					.ToList();

				TimeEntries.Clear();
				foreach (var entry in displayEntries)
				{
					TimeEntries.Add(entry);
				}

				System.Diagnostics.Debug.WriteLine($"Added {TimeEntries.Count} entries to collection");
			}
			else
			{
				var error = await response.Content.ReadAsStringAsync();
				System.Diagnostics.Debug.WriteLine($"Error response: {error}");
				await DisplayAlert("Error", $"Failed to load time entries: {response.StatusCode}", "OK");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error loading time entries: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
			await DisplayAlert("Error", $"Failed to load time entries: {ex.Message}", "OK");
		}
	}

	private string CalculateDuration(long clockIn, long clockOut)
	{
		var duration = TimeSpan.FromSeconds(clockOut - clockIn);
		return $"{(int)duration.TotalHours}h {duration.Minutes}m";
	}

	private async void OnEditTimeEntry(object? sender, EventArgs e)
	{
		if (sender is not Button button || button.CommandParameter is not AdminTimeEntryDisplay entry)
			return;

		// Prompt for new clock in time
		var clockInResult = await DisplayPromptAsync(
			"Edit Clock In",
			"Enter new clock in time (MM/dd/yyyy HH:mm):",
			initialValue: DateTimeOffset.FromUnixTimeSeconds(GetUnixFromDisplay(entry.ClockInTime)).ToLocalTime().ToString("MM/dd/yyyy HH:mm"));

		if (string.IsNullOrEmpty(clockInResult)) return;

		string? clockOutResult = null;
		if (entry.ClockOutTime != "Still clocked in")
		{
			clockOutResult = await DisplayPromptAsync(
				"Edit Clock Out",
				"Enter new clock out time (MM/dd/yyyy HH:mm):",
				initialValue: DateTimeOffset.FromUnixTimeSeconds(GetUnixFromDisplay(entry.ClockOutTime)).ToLocalTime().ToString("MM/dd/yyyy HH:mm"));

			if (clockOutResult == null) return;
		}

		var notesResult = await DisplayPromptAsync(
			"Edit Notes",
			"Enter notes:",
			initialValue: entry.Notes);

		try
		{
			if (DateTime.TryParse(clockInResult, out var newClockIn))
			{
				var clockInUnix = new DateTimeOffset(newClockIn).ToUnixTimeSeconds();
				long? clockOutUnix = null;

				if (!string.IsNullOrEmpty(clockOutResult) && DateTime.TryParse(clockOutResult, out var newClockOut))
				{
					clockOutUnix = new DateTimeOffset(newClockOut).ToUnixTimeSeconds();
				}

				var updateDto = new
				{
					ClockIn = clockInUnix,
					ClockOut = clockOutUnix,
					Notes = notesResult ?? entry.Notes
				};

				var response = await _httpClient.PutAsJsonAsync($"/api/timeentries/{entry.Id}", updateDto);
				if (response.IsSuccessStatusCode)
				{
					await DisplayAlert("Success", "Time entry updated successfully", "OK");
					await LoadEmployeeTimeEntries(_selectedEmployeeId);
				}
				else
				{
					await DisplayAlert("Error", "Failed to update time entry", "OK");
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error editing time entry: {ex.Message}");
			await DisplayAlert("Error", $"Failed to update: {ex.Message}", "OK");
		}
	}

	private async void OnApproveTimeEntry(object? sender, EventArgs e)
	{
		if (sender is not Button button || button.CommandParameter is not AdminTimeEntryDisplay entry)
			return;

		var confirm = await DisplayAlert("Confirm", "Approve this time entry?", "Yes", "No");
		if (!confirm) return;

		try
		{
			System.Diagnostics.Debug.WriteLine($"Approving time entry ID: {entry.Id}");

			// First, get the full time entry data
			var getResponse = await _httpClient.GetAsync($"/api/timeentries/{entry.Id}");
			if (!getResponse.IsSuccessStatusCode)
			{
				await DisplayAlert("Error", "Failed to retrieve time entry", "OK");
				return;
			}

			var timeEntry = await getResponse.Content.ReadFromJsonAsync<TimeEntry>();
			if (timeEntry == null)
			{
				await DisplayAlert("Error", "Time entry not found", "OK");
				return;
			}

			// Update the IsApproved field
			timeEntry.IsApproved = true;

			// Send the full updated time entry via PUT
			var response = await _httpClient.PutAsJsonAsync($"/api/timeentries/{entry.Id}", timeEntry);

			System.Diagnostics.Debug.WriteLine($"Approve response status: {response.StatusCode}");

			if (response.IsSuccessStatusCode)
			{
				await DisplayAlert("Success", "Time entry approved", "OK");
				await LoadEmployeeTimeEntries(_selectedEmployeeId);
			}
			else
			{
				var error = await response.Content.ReadAsStringAsync();
				System.Diagnostics.Debug.WriteLine($"Approve error: {error}");
				await DisplayAlert("Error", $"Failed to approve time entry: {response.StatusCode}\n{error}", "OK");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error approving time entry: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
			await DisplayAlert("Error", $"Failed to approve: {ex.Message}", "OK");
		}
	}

	private async void OnDeleteTimeEntry(object? sender, EventArgs e)
	{
		if (sender is not Button button || button.CommandParameter is not AdminTimeEntryDisplay entry)
			return;

		var confirm = await DisplayAlert("Confirm", "Delete this time entry? This cannot be undone.", "Delete", "Cancel");
		if (!confirm) return;

		try
		{
			var response = await _httpClient.DeleteAsync($"/api/timeentries/{entry.Id}");
			if (response.IsSuccessStatusCode)
			{
				await DisplayAlert("Success", "Time entry deleted", "OK");
				await LoadEmployeeTimeEntries(_selectedEmployeeId);
			}
			else
			{
				await DisplayAlert("Error", "Failed to delete time entry", "OK");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error deleting time entry: {ex.Message}");
			await DisplayAlert("Error", $"Failed to delete: {ex.Message}", "OK");
		}
	}

	private async void OnAddEmployeeClicked(object? sender, EventArgs e)
	{
		var name = await DisplayPromptAsync("New Employee", "Enter employee name:");
		if (string.IsNullOrEmpty(name)) return;

		var email = await DisplayPromptAsync("New Employee", "Enter email:");
		if (string.IsNullOrEmpty(email)) return;

		var password = await DisplayPromptAsync("New Employee", "Enter password:");
		if (string.IsNullOrEmpty(password)) return;

		var role = await DisplayPromptAsync("New Employee", "Enter role (Employee/Manager/Admin):");
		if (string.IsNullOrEmpty(role)) return;

		var rateStr = await DisplayPromptAsync("New Employee", "Enter hourly rate:");
		if (string.IsNullOrEmpty(rateStr) || !decimal.TryParse(rateStr, out var rate)) return;

		try
		{
			var newEmployee = new
			{
				Name = name,
				Email = email,
				Password = password,
				Role = role,
				HourlyRate = rate,
				IsActive = true
			};

			System.Diagnostics.Debug.WriteLine($"Creating employee - Name: {name}, Email: {email}, Role: {role}, Rate: {rate}");

			var response = await _httpClient.PostAsJsonAsync("/api/employees", newEmployee);

			System.Diagnostics.Debug.WriteLine($"Create employee response: {response.StatusCode}");

			if (response.IsSuccessStatusCode)
			{
				var createdEmployee = await response.Content.ReadFromJsonAsync<Employee>();
				System.Diagnostics.Debug.WriteLine($"Employee created successfully - ID: {createdEmployee?.Id}, Name: {createdEmployee?.Name}");

				await DisplayAlert("Success", $"Employee '{name}' added successfully with ID: {createdEmployee?.Id}", "OK");
				await LoadEmployees();
			}
			else
			{
				var error = await response.Content.ReadAsStringAsync();
				System.Diagnostics.Debug.WriteLine($"Failed to create employee: {error}");
				await DisplayAlert("Error", $"Failed to add employee: {error}", "OK");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error adding employee: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
			await DisplayAlert("Error", $"Failed to add employee: {ex.Message}", "OK");
		}
	}

	private async void OnApproveAllClicked(object? sender, EventArgs e)
	{
		if (_selectedEmployeeId == 0)
		{
			await DisplayAlert("Error", "No employee selected", "OK");
			return;
		}

		var pendingCount = TimeEntries.Count(e => !e.IsApproved);
		if (pendingCount == 0)
		{
			await DisplayAlert("Info", "No pending entries to approve", "OK");
			return;
		}

		var confirm = await DisplayAlert("Confirm", $"Approve all {pendingCount} pending time entries?", "Yes", "No");
		if (!confirm) return;

		try
		{
			int successCount = 0;
			int failCount = 0;

			foreach (var entry in TimeEntries.Where(e => !e.IsApproved).ToList())
			{
				try
				{
					// Get the full time entry
					var getResponse = await _httpClient.GetAsync($"/api/timeentries/{entry.Id}");
					if (!getResponse.IsSuccessStatusCode)
					{
						failCount++;
						continue;
					}

					var timeEntry = await getResponse.Content.ReadFromJsonAsync<TimeEntry>();
					if (timeEntry == null)
					{
						failCount++;
						continue;
					}

					// Approve it
					timeEntry.IsApproved = true;
					var response = await _httpClient.PutAsJsonAsync($"/api/timeentries/{entry.Id}", timeEntry);

					if (response.IsSuccessStatusCode)
						successCount++;
					else
						failCount++;
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Error approving entry {entry.Id}: {ex.Message}");
					failCount++;
				}
			}

			if (failCount == 0)
			{
				await DisplayAlert("Success", $"All {successCount} entries approved successfully!", "OK");
			}
			else
			{
				await DisplayAlert("Partial Success", $"{successCount} entries approved, {failCount} failed.", "OK");
			}

			await LoadEmployeeTimeEntries(_selectedEmployeeId);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error in ApproveAll: {ex.Message}");
			await DisplayAlert("Error", $"Failed to approve entries: {ex.Message}", "OK");
		}
	}

	private long GetUnixFromDisplay(string displayTime)
	{
		// Parse the display time back to Unix timestamp
		if (DateTime.TryParse(displayTime, out var dt))
		{
			return new DateTimeOffset(dt).ToUnixTimeSeconds();
		}
		return 0;
	}
}

public class AdminTimeEntryDisplay
{
	public int Id { get; set; }
	public int EmployeeId { get; set; }
	public string ClockInTime { get; set; } = string.Empty;
	public string ClockOutTime { get; set; } = string.Empty;
	public string Duration { get; set; } = string.Empty;
	public string Notes { get; set; } = string.Empty;
	public bool IsApproved { get; set; }
	public string ApprovalStatus { get; set; } = string.Empty;
	public Color ApprovalColor { get; set; } = Colors.Gray;
	public bool CanApprove { get; set; }
}
