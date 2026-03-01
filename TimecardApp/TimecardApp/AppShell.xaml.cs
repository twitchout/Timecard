namespace TimecardApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Set initial icon based on theme
            UpdateIconForTheme();

            // Listen for theme changes
            Application.Current!.RequestedThemeChanged += (s, e) =>
            {
                UpdateIconForTheme();
            };
        }

        private void UpdateIconForTheme()
        {
            var isDarkTheme = Application.Current?.RequestedTheme == AppTheme.Dark;
            var iconFile = isDarkTheme ? "login_dark.png" : "login.png";

            // update flyout icon(s) based on theme
            LoginFlyoutItem.FlyoutIcon = ImageSource.FromFile(iconFile);
        }
    }
}
