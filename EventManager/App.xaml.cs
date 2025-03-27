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

        protected override void OnStart()
        {
            base.OnStart();
            ApplySavedTheme();
        }

        private void ApplySavedTheme()
        {
            int savedTheme = Preferences.Get("AppTheme", 0);

            UserAppTheme = savedTheme switch
            {
                1 => AppTheme.Light,
                2 => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }
    }
}