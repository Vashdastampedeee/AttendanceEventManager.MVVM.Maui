using EventManager.ViewModels;

namespace EventManager.Views;

public partial class Dashboard : ContentPage
{
	public Dashboard(DashboardViewModel dashboardViewModel)
	{
		InitializeComponent();
		BindingContext = dashboardViewModel;
	}
}