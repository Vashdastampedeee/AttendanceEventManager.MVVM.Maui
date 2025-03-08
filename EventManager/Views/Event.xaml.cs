using EventManager.ViewModels;

namespace EventManager.Views;

public partial class Event : ContentPage
{
	public Event(EventViewModel eventViewModel)
	{
		InitializeComponent();
		BindingContext = eventViewModel;
	}
}