using EventManager.ViewModels;

namespace EventManager.Views;

public partial class Logs : ContentPage
{
	public Logs(LogsViewModel logsViewModel)
	{
		InitializeComponent();
		BindingContext = logsViewModel;
	}
}