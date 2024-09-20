namespace WinUIExSample;
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LogWindow : WinUIEx.WindowEx
    {
        public LogWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            UpdateLog();
        }

        public MainWindow MainWindow => (MainWindow)((App)Application.Current).MainWindow;

        internal void UpdateLog()
        {
            var log = MainWindow.WindowEvents;
            if (log != null)
            {
                var multiline_text = string.Join('\n', log.Reverse());
                DispatcherQueue.TryEnqueue(() =>
                {
                    WindowEventLog.Text = multiline_text;
                });
            }
        }
    }
}
