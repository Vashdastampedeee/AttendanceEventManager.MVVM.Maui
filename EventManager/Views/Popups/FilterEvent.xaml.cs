using EventManager.ViewModels.Popups;
using Mopups.Pages;

namespace EventManager.Views.Popups;

public partial class FilterEvent : PopupPage
{
	public FilterEvent(FilterEventViewModel filterEventViewModel)
	{
		InitializeComponent();
		BindingContext = filterEventViewModel;
	}
}