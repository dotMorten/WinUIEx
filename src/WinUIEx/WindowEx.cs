using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

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
        /// Initializes a new instance of the <see cref="WindowEx"/> class.
        /// </summary>
        public WindowEx()
        {
            AppWindow = WindowExtensions.GetAppWindowFromWindowHandle(this.GetWindowHandle());

            var rootContent = new Grid();
            rootContent.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto), MinHeight = 0 });
            rootContent.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            titleBarArea = new Grid() { Visibility = Visibility.Collapsed };
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
            AppWindow.Changed += AppWindow_Changed;
            var size = AppWindow.Size;
            _width = size.Width;
            _height = size.Height;
        }

        private void AppWindow_Changed(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
        {
            if (args.DidPositionChange)
                PositionChanged?.Invoke(this, EventArgs.Empty);
            if(args.DidSizeChange)
            {
                _width = sender.Size.Width;
                _height = sender.Size.Height;
            }
        }

        /// <summary>
        /// Gets a reference to the AppWindow for the app
        /// </summary>
        public Microsoft.UI.Windowing.AppWindow AppWindow { get; }

        /// <summary>
        /// Brings the window to the front
        /// </summary>
        /// <returns></returns>
        public bool BringToFront() => WindowExtensions.SetForegroundWindow(this);

        private Icon? _TaskBarIcon;

        /// <summary>
        /// Gets or sets the task bar icon.
        /// </summary>
        public Icon? TaskBarIcon
        {
            get { return _TaskBarIcon; }
            set {
                _TaskBarIcon = value;
                this.SetTaskBarIcon(value);
            }
        }

        /// <summary>
        /// Gets or sets the window title.
        /// </summary>
        public new string Title // Workaround for https://github.com/microsoft/microsoft-ui-xaml/issues/3689
        {
            get => base.Title;
            set => base.Title = value;
        }

        /// <summary>
        /// Gets or sets the Window content 
        /// /// </summary>
        public UIElement? TitleBar
        {
            get { return titleBarContainer.Content as UIElement; }
            set
            {
                AppWindow.TitleBar.ResetToDefault();
                titleBarContainer.Content = value;
                if (value is null)
                {
                    titleBarArea.Visibility = Visibility.Collapsed;
                    base.ExtendsContentIntoTitleBar = false;
                }
                else
                {
                    titleBarArea.Visibility = Visibility.Visible;
                    base.ExtendsContentIntoTitleBar = true;
                    SetTitleBar(TitleBar);
                }
            }
        }

        /// <summary>
        /// Gets or sets the Window content 
        /// /// </summary>
        public object? WindowContent
        {
            get { return windowArea.Content; }
            set { windowArea.Content = value; }
          
        }

        private bool _IsTitleBarVisible = true;

        /// <summary>
        /// Gets or sets a value indicating whether the default title bar is visible or not.
        /// </summary>
        public bool IsTitleBarVisible
        {
            get { return _IsTitleBarVisible; }
            set
            {
                _IsTitleBarVisible = value;
                this.SetHasTitleBar(value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the window has a frame or not.
        /// </summary>
        public bool IsFrameVisible
        {
            get => AppWindow.Configuration.HasFrame;
            set => this.SetHasFrame(value);
        }
                
        /// <summary>
        /// Gets or sets a value indicating whether the minimimze button is visible
        /// </summary>
        public bool IsMinimizable
        {
            get => AppWindow.Configuration.IsMinimizable;
            set => this.SetIsMinimizable(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the maximimze button is visible
        /// </summary>
        public bool IsMaximizable

        {
            get => AppWindow.Configuration.IsMaximizable;
            set => this.SetIsMaximizable(value);
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
                this.SetIsModal(!value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the window is displayed on top of other windows
        /// </summary>
        public bool IsAlwaysOnTop
        {
            get => AppWindow.Configuration.IsAlwaysOnTop;
            set => WindowExtensions.SetAlwaysOnTop(this, value);
        }

        private double _width;

        /// <summary>
        /// Gets or sets the width of the window
        /// </summary>
        public double Width
        {
            get { return _width; }
            set
            {
                _width = value; 
                this.SetWindowSize(_width, _height);
            }
        }

        private double _height;

        /// <summary>
        /// Gets or sets the height of the window
        /// </summary>
        public double Height
        {
            get { return _height; }
            set
            {
                _height = value;
                this.SetWindowSize(_width, _height);
            }
        }

        /// <summary>
        /// Raised if the window position changes
        /// </summary>
        public event EventHandler? PositionChanged;
    }
}
