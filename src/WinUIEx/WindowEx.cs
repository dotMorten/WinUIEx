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
    [ContentProperty(Name = "WindowContent")]
    public class WindowEx : Window
    {
        private readonly Grid titleBarArea;
        private readonly Image iconArea;
        private readonly ContentControl titleBarContainer;
        private readonly ContentControl windowArea;
        
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

        private FrameworkElement WindowRoot;

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


        private bool _IsMinimizeButtonVisible;
        
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

        public bool IsAlwaysOnTop
        {
            get => _IsAlwaysOnTop;
            set
            {
                _IsAlwaysOnTop = value;
                WindowExtensions.SetAlwaysOnTop(this, value);
            }
        }

        public ImageSource Icon
        {
            get => iconArea.Source;
            set => iconArea.Source = value;
        }

        private UIElement _titleBarContent;

        public UIElement TitleBar
        {
            get => _titleBarContent;
            set
            {
                _titleBarContent = value;
                titleBarContainer.Content = value;
            }
        }

        public object WindowContent
        {
            get { return windowArea.Content; }
            set { windowArea.Content = value; }
        }

        public bool BringToFront() => WindowExtensions.SetForegroundWindow(this);


        private double _width = 1024;

        public double Width
        {
            get { return _width; }
            set { _width = value; }
        }
        private double _height = 786;

        public double Height
        {
            get { return _height; }
            set { _height = value; }
        }

    }
}
