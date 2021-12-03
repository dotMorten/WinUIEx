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
#if EXPERIMENTAL
        private readonly WindowMessageMonitor monitor;
        private readonly Queue<string> windowEvents = new Queue<string>();
#endif
        public MainWindow()
        {
            this.InitializeComponent();

#if EXPERIMENTAL
            monitor = new WindowMessageMonitor(this);
            monitor.WindowMessageRecieved += Monitor_WindowMessageRecieved;
#endif
        }

#if EXPERIMENTAL
        private void Monitor_WindowMessageRecieved(object sender, WindowMessageEventArgs e)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                windowEvents.Enqueue($"{e.MessageType}: w={e.WParam}, l={e.LParam}");
                if (windowEvents.Count > 100)
                    windowEvents.Dequeue();
                WindowEventLog.Text = string.Join('\n', windowEvents.Reverse());
            });
        }
#endif

        private void Center_Click(object sender, RoutedEventArgs e) => this.CenterOnScreen();

        private void MaximizeWindow_Click(object sender, RoutedEventArgs e) => this.MaximizeWindow();

        private void RestoreWindow_Click(object sender, RoutedEventArgs e) => this.RestoreWindow();

        private async void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.MinimizeWindow();
            await Task.Delay(2000);
            this.RestoreWindow();
        }

        private async void HideWindow_Click(object sender, RoutedEventArgs e)
        {
            this.HideWindow();
            await Task.Delay(2000);
            this.RestoreWindow();
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
                this.ShowWindow();
                tray.Dispose();
                tray = null;
            };
            this.HideWindow();
        }

        private async void BringToFront_Click(object sender, RoutedEventArgs e)
        {
            await Task.Delay(2000);
            this.BringToFront();
        }

        private void CustomTitleBar_Toggled(object sender, RoutedEventArgs e)
        {
            if(((ToggleSwitch)sender).IsOn)
            {
                TitleBar = new TextBlock() { Text = Title, FontSize = 24, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red) };
            }
            else
            {
                TitleBar = null;
            }

        }
        /*
        private void Fullscreen_Toggled(object sender, RoutedEventArgs e)
        {
            
            // Disabled until this is fixed:
            if (((ToggleSwitch)sender).IsOn)
            {
                bool succcess = AppWindow.TrySetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
            }
            else
            {
                bool succcess = AppWindow.TrySetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);
            }
     }*/
    }
}
