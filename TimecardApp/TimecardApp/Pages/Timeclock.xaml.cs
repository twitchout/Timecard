namespace TimecardApp.Pages;

public partial class Timeclock : ContentPage
{
	public Timeclock()
	{
		InitializeComponent();
		TimePunchBtn.Clicked += OnTimePunchBtnClicked;
	}

	private void OnTimePunchBtnClicked(object sender, EventArgs e)
	{
		// TODO:	make time punches store info
		//			update alert to display what time punch was

		if (TimePunchBtn.Text == "Clock In")
		{
			TimePunchBtn.Text = "Clock Out";
			TimePunchAlert.Text = "You have successfully clocked in.";
		}
		else
		{
			TimePunchBtn.Text = "Clock In";
			TimePunchAlert.Text = "You have successfully clocked out.";
		}
	}
}