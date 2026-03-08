using TimecardApp.Pages;

namespace TimecardApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            LoginBtn.Clicked += OnLoginBtnClicked;
        }

        private async void OnLoginBtnClicked(object? sender, EventArgs e)
        {
            // For now, just navigate to Timeclock
            // Backend connection is ready when you want to add employee ID validation
            await Shell.Current.GoToAsync(nameof(Timeclock));
        }
    }
}
