namespace EventManager.Views.Popups;
using CommunityToolkit.Maui.Views;
using EventManager.ViewModels.Popups;

public partial class AddEvent : Popup
{
	public AddEvent(AddEventViewModel addEventViewModel)
	{
		InitializeComponent();
		BindingContext = addEventViewModel;
	}
}