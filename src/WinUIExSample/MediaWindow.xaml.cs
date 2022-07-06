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
    public sealed partial class MediaWindow : WindowEx
    {
        public MediaWindow()
        {
            this.InitializeComponent();
            BuildPropertyPanel(player);
            BuildPropertyPanel(player.TransportControls);
        }

        private void BuildPropertyPanel(object element)
        {
            var props = element.GetType().GetProperties();
            foreach (var prop in props.Where(p => p.CanWrite && p.CanRead).OrderBy(p => p.Name))
            {
                if (prop.DeclaringType != element.GetType())
                    continue;
                if (prop.PropertyType == typeof(bool))
                {
                    ToggleSwitch ts = new ToggleSwitch();
                    ts.Header = prop.Name;
                    ts.IsOn = (bool)prop.GetValue(element);
                    ts.Toggled += (s, e) =>
                    {
                        prop.SetValue(element, ts.IsOn);
                    };
                    propPanel.Children.Add(ts);
                }
                else if(prop.PropertyType.IsEnum)
                {
                    ComboBox cb = new ComboBox { Header = prop.Name };
                    var value = prop.GetValue(element);
                    foreach (var v in Enum.GetValues(prop.PropertyType))
                        cb.Items.Add(new ComboBoxItem() { IsSelected = v.Equals(value), Content = v.ToString(), Tag = v });
                    cb.SelectionChanged += (s, e) => prop.SetValue(element, (cb.SelectedItem as ComboBoxItem).Tag);
                    propPanel.Children.Add(cb);
                }
            }
        }

        private void LoadSourceButton_Click(object sender, RoutedEventArgs e)
        {
            player.Source = new Uri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4");
        }

        private void LoadInvalidSourceButton_Click(object sender, RoutedEventArgs e)
        {
            player.Source = new Uri("https://this-wont-work.com/errormedia.mp4");
        }

        private void ShowTransportControls_Click(object sender, RoutedEventArgs e) => player.TransportControls.Show();

        private void HideTransportControls_Click(object sender, RoutedEventArgs e) => player.TransportControls.Hide();

    }
}
