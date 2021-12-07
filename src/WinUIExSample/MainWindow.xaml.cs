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

namespace WinUIExSample
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        private readonly Queue<string> windowEvents = new Queue<string>();

#if EXPERIMENTAL
        private readonly WindowMessageMonitor monitor;
#endif
        public MainWindow()
        {
            this.InitializeComponent();
            this.PresenterChanged += (s, e) => Log("PresenterChanged");
            this.PositionChanged += (s, e) => Log("PositionChanged");
            this.SetTitleBarBackgroundColors(Microsoft.UI.Colors.CornflowerBlue);
#if EXPERIMENTAL
            monitor = new WindowMessageMonitor(this);
            monitor.WindowMessageRecieved += Monitor_WindowMessageRecieved;
#endif
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

#if EXPERIMENTAL
        private void Monitor_WindowMessageRecieved(object sender, WindowMessageEventArgs e)
        {
            Log($"{e.MessageType}: w={e.WParam}, l={e.LParam}");
        }
#endif

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

        private TrayIcon tray;

        private void ToggleTrayIcon_Click(object sender, RoutedEventArgs e)
        {
            if (tray is null)
            {
                var icon = Icon.FromFile("Images/WindowIcon.ico");
                tray = new TrayIcon(icon);
                tray.TrayIconLeftMouseDown += (s, e) => this.BringToFront();
            }
            else
            {
                tray.Dispose();
                tray = null;
            }
        }

        private void MinimizeTrayIcon_Click(object sender, RoutedEventArgs e)
        {
            tray?.Dispose();
            var icon = Icon.FromFile("Images/WindowIcon.ico");
            tray = new TrayIcon(icon);
            tray.TrayIconLeftMouseDown += (s, e) =>
            {
                this.Show();
                tray.Dispose();
                tray = null;
            };
            this.Hide();
        }

        private async void BringToFront_Click(object sender, RoutedEventArgs e)
        {
            await Task.Delay(2000);
            this.BringToFront();
        }

        private void CustomTitleBar_Toggled(object sender, RoutedEventArgs e)
        {
            if (((ToggleSwitch)sender).IsOn)
            {
                TitleBar = new TextBlock() { Text = Title, FontSize = 24, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red) };
            }
            else
            {
                TitleBar = null;
            }
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
    }
}
