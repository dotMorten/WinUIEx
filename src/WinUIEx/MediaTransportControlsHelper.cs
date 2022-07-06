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
        /// <summary>
        /// Gets the DropoutOrder attached property value
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int? GetDropoutOrder(DependencyObject obj)
        {
            return (int?)obj.GetValue(DropoutOrderProperty);
        }

        /// <summary>
        /// Sets the DropoutOrder attached property value
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public static void SetDropoutOrder(DependencyObject obj, int? value)
        {
            obj.SetValue(DropoutOrderProperty, value);
        }

        /// <summary>
        /// Identifies the DropoutOrder attached dependency property.
        /// </summary>
        public static readonly DependencyProperty DropoutOrderProperty =
            DependencyProperty.RegisterAttached("DropoutOrder", typeof(int?), typeof(MediaTransportControlsHelper), new PropertyMetadata(0));
    }
}
