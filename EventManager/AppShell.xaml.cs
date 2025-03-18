using EventManager.Views.Modals;

namespace EventManager
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(TotalScannedData), typeof(TotalScannedData));
        }
    }
}
