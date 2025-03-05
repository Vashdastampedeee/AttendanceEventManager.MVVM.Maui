using EventManager.ViewModels;

namespace EventManager.Views;

public partial class Index : ContentPage
{
	public Index(IndexViewModel indexViewModel)
	{
		InitializeComponent();
		BindingContext = indexViewModel;
	}
}