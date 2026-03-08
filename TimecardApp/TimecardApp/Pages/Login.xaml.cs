using System.Net.Http.Json;
using TimecardApp.Pages;

namespace TimecardApp
{
    public partial class MainPage : ContentPage
    {
        private readonly HttpClient _httpClient;
#if ANDROID
        // Android emulator uses 10.0.2.2 to access host machine's localhost
        private readonly string _baseUrl = "http://10.0.2.2:5090";
#else
        private readonly string _baseUrl = "http://localhost:5090";
#endif

        public MainPage()
        {
            InitializeComponent();
            LoginBtn.Clicked += OnLoginBtnClicked;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            // Listen for theme changes to update password toggle icon
            Application.Current!.RequestedThemeChanged += (s, e) =>
            {
                UpdatePasswordToggleIcon();
            };

            // Set initial icon
            UpdatePasswordToggleIcon();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Clear any previous error or alert messages
            ErrorLabel.Text = string.Empty;
            ErrorLabel.IsVisible = false;
        }

        private void UpdatePasswordToggleIcon()
        {
            var isDarkTheme = Application.Current?.RequestedTheme == AppTheme.Dark;

            if (PasswordEntry.IsPassword)
            {
                // Password is hidden, show "eye" icon to reveal it
                TogglePasswordBtn.Source = isDarkTheme ? "eye_dark.png" : "eye.png";
            }
            else
            {
                // Password is visible, show "eye slash" icon to hide it
                TogglePasswordBtn.Source = isDarkTheme ? "eye_slash_dark.png" : "eye_slash.png";
            }
        }

        private void OnPasswordCompleted(object? sender, EventArgs e)
        {
            // Trigger login when Enter is pressed in password field
            OnLoginBtnClicked(sender, e);
        }

        private void OnTogglePasswordVisibility(object? sender, EventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            UpdatePasswordToggleIcon();
        }

        private async void OnLoginBtnClicked(object? sender, EventArgs e)
        {
            // Clear previous error
            ErrorLabel.IsVisible = false;
            LoginBtn.IsEnabled = false;
            LoginBtn.Text = "Logging in...";

            // Validate inputs
            var email = EmailEntry.Text?.Trim();
            var password = PasswordEntry.Text;

            if (string.IsNullOrWhiteSpace(email))
            {
                ErrorLabel.Text = "Please enter your email";
                ErrorLabel.IsVisible = true;
                LoginBtn.IsEnabled = true;
                LoginBtn.Text = "Submit";
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorLabel.Text = "Please enter your password";
                ErrorLabel.IsVisible = true;
                LoginBtn.IsEnabled = true;
                LoginBtn.Text = "Submit";
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting login for: {email}");
                
                // Call authentication endpoint
                var loginDto = new { Email = email, Password = password };
                var response = await _httpClient.PostAsJsonAsync("/api/employees/login", loginDto);

                System.Diagnostics.Debug.WriteLine($"Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                    System.Diagnostics.Debug.WriteLine($"Login successful! Employee ID: {loginResponse?.EmployeeId}");

                    if (loginResponse != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Login Response - ID: {loginResponse.EmployeeId}, Name: {loginResponse.Name}, Role: {loginResponse.Role}");

                        // Store employee information in Preferences
                        Preferences.Set("EmployeeId", loginResponse.EmployeeId);
                        Preferences.Set("EmployeeName", loginResponse.Name);
                        Preferences.Set("EmployeeRole", loginResponse.Role);
                        Preferences.Set("IsLoggedIn", true);

                        System.Diagnostics.Debug.WriteLine($"Stored Preferences - ID: {Preferences.Get("EmployeeId", 0)}, Name: {Preferences.Get("EmployeeName", "")}");

                        // DON'T call UpdateFlyoutVisibility here - it will be called after navigation
                        // if (Application.Current?.Windows.Count > 0 && 
                        //     Application.Current.Windows[0].Page is AppShell shell)
                        // {
                        //     shell.UpdateFlyoutVisibility();
                        // }

                        // Extract first name from the full name
                        var firstName = loginResponse.Name.Split(' ')[0];

                        // Clear password field for security
                        PasswordEntry.Text = string.Empty;
                        EmailEntry.Text = string.Empty;

                        // Navigate to Timeclock page with employee ID and first name
                        System.Diagnostics.Debug.WriteLine($"Navigating to Timeclock with ID: {loginResponse.EmployeeId}");
                        await Shell.Current.GoToAsync($"{nameof(Timeclock)}?employeeId={loginResponse.EmployeeId}&firstName={Uri.EscapeDataString(firstName)}");
                        
                        // Update flyout after a short delay to ensure navigation completes
                        await Task.Delay(100);
                        if (Application.Current?.Windows.Count > 0 && 
                            Application.Current.Windows[0].Page is AppShell shell)
                        {
                            shell.UpdateFlyoutVisibility();
                        }
                    }
                    else
                    {
                        ErrorLabel.Text = "Login response was empty";
                        ErrorLabel.IsVisible = true;
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Unauthorized: {errorContent}");
                    ErrorLabel.Text = "Invalid email or password. Please try again.";
                    ErrorLabel.IsVisible = true;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Bad Request: {errorContent}");
                    ErrorLabel.Text = "Invalid email or password. Please try again.";
                    ErrorLabel.IsVisible = true;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || 
                         response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Server Error {response.StatusCode}: {errorContent}");
                    ErrorLabel.Text = "Server is currently unavailable. Please try again later.";
                    ErrorLabel.IsVisible = true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Error {response.StatusCode}: {errorContent}");
                    ErrorLabel.Text = "Unable to log in. Please check your connection and try again.";
                    ErrorLabel.IsVisible = true;
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Network error: {ex.Message}");
                ErrorLabel.Text = "Unable to connect to server. Please check your internet connection.";
                ErrorLabel.IsVisible = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                ErrorLabel.Text = "An unexpected error occurred. Please try again.";
                ErrorLabel.IsVisible = true;
            }
            finally
            {
                LoginBtn.IsEnabled = true;
                LoginBtn.Text = "Submit";
            }
        }

        private class LoginResponse
        {
            public int EmployeeId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }
    }
}
