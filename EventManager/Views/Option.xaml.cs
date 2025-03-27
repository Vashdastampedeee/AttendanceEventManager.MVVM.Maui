using EventManager.ViewModels;

namespace EventManager.Views;

public partial class Option : ContentPage
{
	public Option(OptionViewModel optionViewModel)
	{
		InitializeComponent();
		BindingContext = optionViewModel;
	}
}