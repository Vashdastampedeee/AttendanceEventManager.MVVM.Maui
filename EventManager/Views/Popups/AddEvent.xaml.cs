namespace EventManager.Views.Popups;
using EventManager.ViewModels.Popups;
using Mopups.Pages;

public partial class AddEvent : PopupPage
{
	public AddEvent(AddEventViewModel addEventViewModel)
	{
		InitializeComponent();
		BindingContext = addEventViewModel;
	}
}