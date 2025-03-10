using EventManager.ViewModels.Popups;
using Mopups.Pages;

namespace EventManager.Views.Popups;

public partial class SyncData : PopupPage
{
	public SyncData(SyncDataViewModel syncDataViewModel)
	{
		InitializeComponent();
		BindingContext = syncDataViewModel;
	}
}