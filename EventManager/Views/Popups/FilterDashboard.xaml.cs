using EventManager.ViewModels.Popups;
using Mopups.Pages;

namespace EventManager.Views.Popups;

public partial class FilterDashboard : PopupPage
{
	public FilterDashboard(FilterDashboardViewModel filterDashboardViewModel)
	{
		InitializeComponent();
		BindingContext = filterDashboardViewModel;
	}
}