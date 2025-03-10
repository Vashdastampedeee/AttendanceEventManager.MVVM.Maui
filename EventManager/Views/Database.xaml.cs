using EventManager.ViewModels;

namespace EventManager.Views;

public partial class Database : ContentPage
{
	public Database(DatabaseViewModel databaseViewModel)
	{
		InitializeComponent();
		BindingContext = databaseViewModel;
	}
}