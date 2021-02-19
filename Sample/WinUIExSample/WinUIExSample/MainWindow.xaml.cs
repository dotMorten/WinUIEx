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
using Windows.UI.Notifications;
using WinUIEx;

namespace WinUIExSample
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.SetAppIcon("Images/WindowIcon.ico");
        }

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
                tray = new TrayIcon();
                tray.SetIcon(icon);
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
            tray = new TrayIcon();
            tray.SetIcon(icon);
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
    }
}
