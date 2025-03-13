using EventManager.ViewModels.Popups;
using Mopups.Pages;

namespace EventManager.Views.Popups;

public partial class FilterLog : PopupPage
{
	public FilterLog(FilterLogViewModel filterLogViewModel)
	{
		InitializeComponent();
		BindingContext = filterLogViewModel;
	}
}