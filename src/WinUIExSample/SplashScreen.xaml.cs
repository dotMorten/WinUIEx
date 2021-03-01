using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUIExSample
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SplashScreen : WinUIEx.SplashScreen
    {
        public SplashScreen(Window window) : base(window)
        {
            this.InitializeComponent();
        }

        public SplashScreen(Type window) : base(window)
        {
            this.InitializeComponent();
        }

        protected override async Task OnLoading()
        {
            status.Text = "Loading 20%...";
            progress.Value += 20;
            await Task.Delay(1000);
            status.Text = "Loading 40%...";
            progress.Value += 20;
            await Task.Delay(1000);
            status.Text = "Loading 60%...";
            progress.Value += 20;
            await Task.Delay(1000);
            status.Text = "Loading 80%...";
            progress.Value += 20;
            await Task.Delay(1000);
            status.Text = "Finishing up...";
            progress.Value += 20;
            await Task.Delay(1000);
        }
    }
}
