using EventManager.ViewModels.Modals;

namespace EventManager.Views.Modals;

public partial class TotalScannedData : ContentPage
{
	public TotalScannedData(TotalScannedDataViewModel totalScannedDataViewModel)
	{
		InitializeComponent();
		BindingContext = totalScannedDataViewModel;
	}
}