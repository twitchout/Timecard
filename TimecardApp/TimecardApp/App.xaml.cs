using Microsoft.Extensions.DependencyInjection;
using TimecardApp.Pages;

namespace TimecardApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(Timeclock), typeof(Timeclock));
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}