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
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed partial class TransparentWindow : Window
    {
        public TransparentWindow()
        {
            this.InitializeComponent();
            var manager = WindowManager.Get(this);
            manager.PersistenceId = "TransparentWindow";
            manager.Width = 600;
            manager.Height = 400;
            this.ExtendsContentIntoTitleBar = true;
            var currentStyle = this.GetWindowStyle();
            var extendedStyle = this.GetExtendedWindowStyle();
            this.ToggleExtendedWindowStyle(false, ExtendedWindowStyle.WindowEdge);
            //this.SetExtendedWindowStyle(ExtendedWindowStyle.Transparent);
            var extendedStyleAfter = this.GetExtendedWindowStyle(); // This is still WindowEdge?!?
        }
    }
}
