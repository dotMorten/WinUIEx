using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUIEx
{
    /// <summary>
    /// Provides properties and methods to customize media transport controls.
    /// </summary>
    public sealed class MediaTransportControlsHelper
    {
        public static int? GetDropoutOrder(DependencyObject obj)
        {
            return (int?)obj.GetValue(DropoutOrderProperty);
        }

        public static void SetDropoutOrder(DependencyObject obj, int? value)
        {
            obj.SetValue(DropoutOrderProperty, value);
        }

        public static readonly DependencyProperty DropoutOrderProperty =
            DependencyProperty.RegisterAttached("DropoutOrder", typeof(int?), typeof(MediaTransportControlsHelper), new PropertyMetadata(0));
    }
}
