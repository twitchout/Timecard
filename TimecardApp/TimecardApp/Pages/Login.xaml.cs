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

        private async void OnLoginBtnClicked(object sender, EventArgs e)
        {
            // TODO: create code to parse user input and perform appropriate login info

            // temporarily just redirect user to main portion of app for testing before adding functionality
            await Shell.Current.GoToAsync(nameof(Timeclock));
        }
    }
}
