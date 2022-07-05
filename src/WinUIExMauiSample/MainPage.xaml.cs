namespace WinUIExMauiSample
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnFullScreenClicked(object sender, EventArgs e)
        {
#if WINDOWS
            var manager = WinUIEx.WindowManager.Get(this.Window.Handler.PlatformView as Microsoft.UI.Xaml.Window);
            if (manager.PresenterKind == Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped)
                manager.PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen;
            else
                manager.PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped;
#endif
        }
    }
}