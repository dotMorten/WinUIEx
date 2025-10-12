using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUIExSample.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TrayIcons : Page
    {
        public TrayIcons()
        {
            this.InitializeComponent();
        }

        public MainWindow MainWindow => (MainWindow)((App)Application.Current).MainWindow!;

        private List<TrayIcon> icons = new List<TrayIcon>();

        private void CreateTray_Click(object sender, RoutedEventArgs e)
        {
            TrayIcon icon = new TrayIcon((uint)icons.Count,
                iconPath: iconSelector.SelectedIndex == 0 ? "Images/OKIcon.ico" : "Images/ErrorIcon.ico",
                tooltip: tooltip.Text);
            icon.IsVisible = true;

            icon.RightClick += (s, e) =>
                e.Flyout = new Flyout() { Content = new TextBlock() { Text = "You right-clicked!" } };
            icon.LeftClick += (s, e) =>
                e.Flyout = new Flyout() { Content = new TextBlock() { Text = "You left-clicked!" } };
            icon.LeftDoubleClick += (s, e) => MainWindow.Activate();

            icons.Add(icon);
        }

        private void UpdateTray_Click(object sender, RoutedEventArgs e)
        {
            foreach(var icon in icons)
            {
                icon.SetIcon(@"e:\GitHub\dotMorten\AnyStatus\src\Apps\Windows\AnyStatus.Apps.Windows\Resources\Icons\Tray\StatusOK.ico");
            }
        }

        private void ClearTray_Click(object sender, RoutedEventArgs e)
        {
            // Normally you would want to Dispose the icons, but here we just test that once GC is run,
            // the icons are removed from the tray
            // foreach(var icon in icons)
            // {
            //     icon.Dispose();
            // }
            icons.Clear();
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            foreach (var icon in icons)
            {
                icon.Tooltip = tooltip.Text;
            }
        }

        private void iconSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var icon in icons)
            {
                icon.SetIcon(iconSelector.SelectedIndex == 0 ? "Images/OKIcon.ico" : "Images/ErrorIcon.ico");
            }
        }
    }
}
