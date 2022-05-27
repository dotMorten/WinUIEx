using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;
using WinUIEx;
using WinUIEx.Messaging;

namespace WinUIExSample
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        private readonly Queue<string> windowEvents = new Queue<string>();
        private readonly WindowMessageMonitor monitor;

        public MainWindow()
        {
            this.InitializeComponent();
            this.PresenterChanged += (s, e) => Log("PresenterChanged");
            this.PositionChanged += (s, e) => Log("PositionChanged");
            this.SetTitleBarBackgroundColors(Microsoft.UI.Colors.CornflowerBlue);

            monitor = new WindowMessageMonitor(this);
            var monitors = MonitorInfo.GetDisplayMonitors();
            foreach (var monitor in monitors.Reverse())
                Log("  - " + monitor.ToString());
            Log($"{monitors.Count} monitors detected");
            if (!Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
                backdropSelector.SelectedIndex = 0; // Backdrops doesn't work on Windows 10.
        }

        private void Log(string message)
        {
            if (!DispatcherQueue.HasThreadAccess)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    Log(message);
                });
                return;
            }
            windowEvents.Enqueue(message);
            if (windowEvents.Count > 100)
                windowEvents.Dequeue();
            WindowEventLog.Text = string.Join('\n', windowEvents.Reverse());
        }

        private void Center_Click(object sender, RoutedEventArgs e) => this.CenterOnScreen();

        private void MaximizeWindow_Click(object sender, RoutedEventArgs e) => this.Maximize();

        private void RestoreWindow_Click(object sender, RoutedEventArgs e) => this.Restore();

        private async void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Minimize();
            await Task.Delay(2000);
            this.Restore();
        }

        private async void HideWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            await Task.Delay(2000);
            this.Restore();
        }

        private async void BringToFront_Click(object sender, RoutedEventArgs e)
        {
            await Task.Delay(2000);
            this.BringToFront();
        }

        private void Presenter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = ((ComboBox)sender).SelectedIndex;
            if (index == 0)
                PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped;
            else if (index == 1)
                PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.CompactOverlay;
            else if (index == 2)
                PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen;
        }

        private async void ShowDialog_Click(object sender, RoutedEventArgs e)
        {
            var commands = new List<Windows.UI.Popups.IUICommand>();
            commands.Add(new Windows.UI.Popups.UICommand("OK"));
            commands.Add(new Windows.UI.Popups.UICommand("Maybe"));
            commands.Add(new Windows.UI.Popups.UICommand("Cancel"));
            var result = await ShowMessageDialogAsync("This is a simple message dialog", commands, cancelCommandIndex: 2, title: "Dialog title");
            Log("You clicked: " + result.Label);
        }

        private void WMMessages_Toggled(object sender, RoutedEventArgs e)
        {
            if (((ToggleSwitch)sender).IsOn)
                monitor.WindowMessageReceived += WindowMessageReceived;
            else
                monitor.WindowMessageReceived -= WindowMessageReceived;
        }


        private void WindowMessageReceived(object sender, WindowMessageEventArgs e)
        {
            Log(e.Message.ToString());
        }

        private void Backdrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch(((ComboBox)sender).SelectedIndex)
            {
                case 1: this.Backdrop = Backdrop.Acrylic; break;
                case 2: this.Backdrop = Backdrop.Mica; break;
                default: this.Backdrop = Backdrop.Default; break;
            }
        }
    }
}
