using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Sdk;

namespace WinUIEx
{
    /// <summary>
    /// A custom WinUI Window with more convenience methods
    /// </summary>
    [ContentProperty(Name = "WindowContent")]
    public class WindowEx : Window
    {
        private readonly Grid titleBarArea;
        private readonly Image iconArea;
        private readonly ContentControl titleBarContainer;
        private readonly ContentControl windowArea;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        public WindowEx()
        {
            var rootContent = new Grid();
            rootContent.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto), MinHeight = 35 });
            rootContent.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            
            titleBarArea = new Grid();
            titleBarArea.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            titleBarArea.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            rootContent.Children.Add(titleBarArea);

            iconArea = new Image() { VerticalAlignment = VerticalAlignment.Center };
            titleBarArea.Children.Add(iconArea);
            titleBarContainer = new ContentControl() { VerticalAlignment = VerticalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
            Grid.SetColumn(titleBarContainer, 1);
            titleBarArea.Children.Add(titleBarContainer);
            
            windowArea = new ContentControl();
            Grid.SetRow(windowArea, 1);
            rootContent.Children.Add(windowArea);

            this.Content = rootContent;
            SetTitleBar(titleBarArea);
            ExtendsContentIntoTitleBar = true;
            rootContent.Loaded += RootLoaded;
            //this.Activated += WindowEx_Activated;
        }

        private bool isInitialized;
        private void WindowEx_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
                return;
            if(!isInitialized)
            {
                isInitialized = true;
                var clientAreaPresenter = VisualTreeHelper.GetParent(Content) as ContentPresenter;
                WindowRoot = VisualTreeHelper.GetParent(clientAreaPresenter) as Grid;
                SetVisibility("MinimizeButton", IsMinimizeButtonVisible);
                SetVisibility("MaximizeButton", IsMaximizeButtonVisible);
                SetVisibility("CloseButton", IsCloseButtonVisible);
                if (IsAlwaysOnTop)
                    IsAlwaysOnTop = true;
            }
        }

        private FrameworkElement? WindowRoot;

        private void SetVisibility(string name, bool visible)
        {
            var element = WindowRoot?.FindName(name) as FrameworkElement;
            if (element != null)
                element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RootLoaded(object sender, RoutedEventArgs e)
        {
            
            var clientAreaPresenter = VisualTreeHelper.GetParent(Content) as ContentPresenter;
            WindowRoot = VisualTreeHelper.GetParent(clientAreaPresenter) as Grid;
            SetVisibility("MinimizeButton", IsMinimizeButtonVisible);
            SetVisibility("MaximizeButton", IsMaximizeButtonVisible);
            SetVisibility("CloseButton", IsCloseButtonVisible);
            if (IsAlwaysOnTop)
                IsAlwaysOnTop = true;
        }

        /// <summary>
        /// Brings the window to the front
        /// </summary>
        /// <returns></returns>
        public bool BringToFront() => WindowExtensions.SetForegroundWindow(this);


        private bool _IsMinimizeButtonVisible;
        
        /// <summary>
        /// Gets or sets a value indicating whether the minimimze button is visible
        /// </summary>
        public bool IsMinimizeButtonVisible
        {
            get => _IsMinimizeButtonVisible;
            set
            {
                _IsMinimizeButtonVisible = value;
                SetVisibility("MinimizeButton", value);
            }
        }

        private bool _IsMaximizeButtonVisible;

        /// <summary>
        /// Gets or sets a value indicating whether the maximimze button is visible
        /// </summary>
        public bool IsMaximizeButtonVisible
        {
            get => _IsMaximizeButtonVisible;
            set
            {
                _IsMaximizeButtonVisible = value;
                SetVisibility("MaximizeButton", value);
            }
        }


        private bool _IsCloseButtonVisible;

        /// <summary>
        /// Gets or sets a value indicating whether the close button is visible
        /// </summary>
        public bool IsCloseButtonVisible
        {
            get => _IsCloseButtonVisible;
            set
            {
                _IsCloseButtonVisible = value;
                SetVisibility("CloseButton", value);
            }
        }

        private bool _IsAlwaysOnTop;

        /// <summary>
        /// Gets or sets a value indicating whether the window is displayed on top of other windows
        /// </summary>
        public bool IsAlwaysOnTop
        {
            get => _IsAlwaysOnTop;
            set
            {
                _IsAlwaysOnTop = value;
                WindowExtensions.SetAlwaysOnTop(this, value);
            }
        }

        /// <summary>
        /// Gets or sets the image icon for the title bar
        /// </summary>
        public ImageSource? Icon
        {
            get => iconArea.Source;
            set => iconArea.Source = value;
        }

        private UIElement? _titleBarContent;

        /// <summary>
        /// Gets or sets the title bar content
        /// </summary>
        public UIElement? TitleBar
        {
            get => _titleBarContent;
            set
            {
                _titleBarContent = value;
                titleBarContainer.Content = value;
            }
        }

        /// <summary>
        /// Gets or sets the Window content
        /// </summary>
        public object? WindowContent
        {
            get { return windowArea.Content; }
            set { windowArea.Content = value; }
        }


        private double _width = 1024;

        /// <summary>
        /// Gets or sets the width of the window
        /// </summary>
        public double Width
        {
            get { return _width; }
            set { _width = value; }
        }

        private double _height = 786;

        /// <summary>
        /// Gets or sets the height of the window
        /// </summary>
        public double Height
        {
            get { return _height; }
            set { _height = value; }
        }
    }
}
