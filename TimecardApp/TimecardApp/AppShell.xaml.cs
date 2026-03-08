using TimecardApp.Pages;

namespace TimecardApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for navigation
            Routing.RegisterRoute(nameof(Timeclock), typeof(Timeclock));
            Routing.RegisterRoute(nameof(Timecard), typeof(Timecard));
            Routing.RegisterRoute(nameof(AdminPanel), typeof(AdminPanel));

            // Set initial icon based on theme
            UpdateIconForTheme();

            // Listen for theme changes
            Application.Current!.RequestedThemeChanged += (s, e) =>
            {
                UpdateIconForTheme();
            };

            // Handle initial state after Shell is loaded
            this.Loaded += OnShellLoaded;
        }

        private async void OnShellLoaded(object? sender, EventArgs e)
        {
            // Update flyout visibility on initial load
            UpdateFlyoutVisibility();

            // Update icons to ensure they're set with theme-appropriate versions
            UpdateIconForTheme();

            // Navigate to appropriate page based on login state
            var isLoggedIn = Preferences.Get("IsLoggedIn", false);
            if (isLoggedIn)
            {
                var employeeId = Preferences.Get("EmployeeId", 0);

                if (employeeId > 0)
                {
                    // Use absolute routing to navigate to Timeclock flyout item
                    // The Timeclock page will get employee info from Preferences
                    await Shell.Current.GoToAsync("//Timeclock");
                }
            }
        }

        public void UpdateFlyoutVisibility()
        {
            // Add null checks to prevent null reference exceptions
            if (LoginFlyoutItem == null || TimeclockFlyoutItem == null || TimecardFlyoutItem == null || LogoutFlyoutItem == null || AdminFlyoutItem == null)
            {
                return;
            }

            var isLoggedIn = Preferences.Get("IsLoggedIn", false);
            var role = Preferences.Get("EmployeeRole", "").ToLower();
            var isAdmin = IsAdminRole(role);

            System.Diagnostics.Debug.WriteLine($"UpdateFlyoutVisibility: IsLoggedIn={isLoggedIn}, Role={role}, IsAdmin={isAdmin}");

            // Show/hide flyout items based on login state
            LoginFlyoutItem.IsVisible = !isLoggedIn;
            TimeclockFlyoutItem.IsVisible = isLoggedIn;
            TimecardFlyoutItem.IsVisible = isLoggedIn;
            AdminFlyoutItem.IsVisible = isLoggedIn && isAdmin;
            LogoutFlyoutItem.IsVisible = isLoggedIn;
        }

        private bool IsAdminRole(string role)
        {
            var adminRoles = new[] { "admin", "administrator", "manager", "supervisor" };
            return adminRoles.Contains(role);
        }

        private void UpdateIconForTheme()
        {
            // Add null checks - items may not be initialized yet
            if (LoginFlyoutItem == null || TimeclockFlyoutItem == null || TimecardFlyoutItem == null || LogoutFlyoutItem == null || AdminFlyoutItem == null)
            {
                return;
            }

            var isDarkTheme = Application.Current?.RequestedTheme == AppTheme.Dark;

            // update flyout icon(s) based on theme
            LoginFlyoutItem.FlyoutIcon = ImageSource.FromFile(isDarkTheme ? "login_dark.png" : "login.png");
            TimeclockFlyoutItem.FlyoutIcon = ImageSource.FromFile(isDarkTheme ? "timeclock_dark.png" : "timeclock.png");
            TimecardFlyoutItem.FlyoutIcon = ImageSource.FromFile(isDarkTheme ? "timecard_dark.png" : "timecard.png");
            AdminFlyoutItem.FlyoutIcon = ImageSource.FromFile(isDarkTheme ? "admin_dark.png" : "admin.png");
            LogoutFlyoutItem.FlyoutIcon = ImageSource.FromFile(isDarkTheme ? "login_dark.png" : "login.png");
        }
    }
}
