using EventManager.ViewModels.Popups;
using Mopups.Pages;

namespace EventManager.Views.Popups;

public partial class EditEvent : PopupPage
{
	public EditEvent(EditEventViewModel editEventViewModel)
	{
		InitializeComponent();
		BindingContext = editEventViewModel;
	}
}