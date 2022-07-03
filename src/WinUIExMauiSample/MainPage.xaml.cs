namespace WinUIExMauiSample
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            Navigation.PushModalAsync(new Page());
            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
}