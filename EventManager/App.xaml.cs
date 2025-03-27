namespace EventManager
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Mzc2NzExM0AzMjM4MmUzMDJlMzBvZ2Fpcyt3RzJXSHFmeTFIajNrVlhQUXBrdE11UXlFaEhRcHlCUWgzaTVzPQ==");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}