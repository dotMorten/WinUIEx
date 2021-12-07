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

        private readonly Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowEx"/> class.
        /// </summary>
        public WindowEx()
        {
            overlappedPresenter = Microsoft.UI.Windowing.OverlappedPresenter.Create();
            AppWindow.SetPresenter(overlappedPresenter);

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
            AppWindow.Changed += AppWindow_Changed;
            SizeChanged += Window_SizeChanged;
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
            if(args.DidPresenterChange)
            {
                PresenterChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Shows a message dialog
        /// </summary>
        /// <param name="content">The message displayed to the user.</param>
        /// <param name="title">The title to display on the dialog, if any.</param>
        /// <returns>An object that represents the asynchronous operation.</returns>
        public Task ShowMessageDialogAsync(string content, string title = "") => ShowMessageDialogAsync(content, null, title: title);

        /// <summary>
        /// Shows a message dialog
        /// </summary>
        /// <param name="content">The message displayed to the user.</param>
        /// <param name="commands">an array of commands that appear in the command bar of the message dialog. These commands makes the dialog actionable.</param>
        /// <param name="defaultCommandIndex">The index of the command you want to use as the default. This is the command that fires by default when users press the ENTER key.</param>
        /// <param name="cancelCommandIndex">The index of the command you want to use as the cancel command. This is the command that fires when users press the ESC key.</param>
        /// <param name="title">The title to display on the dialog, if any.</param>
        /// <returns>An object that represents the asynchronous operation.</returns>
        public Task<Windows.UI.Popups.IUICommand> ShowMessageDialogAsync(string content, IList<Windows.UI.Popups.IUICommand>? commands, uint defaultCommandIndex = 0, uint cancelCommandIndex = 1, string title = "")
        {
            var dialog = new Windows.UI.Popups.MessageDialog(content, title);
            WinRT.Interop.InitializeWithWindow.Initialize(dialog, this.GetWindowHandle());
            if (commands != null)
            {
                foreach (var command in commands)
                    dialog.Commands.Add(command);
                if (commands.Count > defaultCommandIndex)
                    dialog.DefaultCommandIndex = defaultCommandIndex;
                if (commands.Count > cancelCommandIndex && cancelCommandIndex != defaultCommandIndex)
                    dialog.CancelCommandIndex = cancelCommandIndex;
            }
            return dialog.ShowAsync().AsTask();
        }

        /// <summary>
        /// Gets a reference to the AppWindow for the app
        /// </summary>
        public Microsoft.UI.Windowing.AppWindow AppWindow => this.GetAppWindow();

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
                    SetTitleBar(titleBarArea);
                }
            }
        }

        private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            _width = args.Size.Width;
            _height = args.Size.Height;
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
                overlappedPresenter.SetBorderAndTitleBar(true /* Crash if you ever set this to false */, value);
            }
        }
                
        /// <summary>
        /// Gets or sets a value indicating whether the minimize button is visible
        /// </summary>
        public bool IsMinimizable
        {
            get => overlappedPresenter.IsMinimizable;
            set => overlappedPresenter.IsMinimizable = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the maximimze button is visible
        /// </summary>
        public bool IsMaximizable
        {
            get => overlappedPresenter.IsMaximizable;
            set => overlappedPresenter.IsMaximizable = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the window can be resized.
        /// </summary>
        public bool IsResizable
        {
            get => overlappedPresenter.IsResizable;
            set => overlappedPresenter.IsResizable = value;
        }

        /*
         * These are currently throwing
        /// <summary>
        /// Gets or sets a value indicating whether the window has a border or not.
        /// </summary>
        public bool HasBorder
        {
            get => overlappedPresenter.HasBorder;
            set => overlappedPresenter.SetBorderAndTitleBar(value, overlappedPresenter.HasTitleBar);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the window is modal or not.
        /// </summary>
        public bool IsModal
        {
            get => overlappedPresenter.IsModal;
            set => overlappedPresenter.IsModal = value;
        }*/

        /// <summary>
        /// Gets or sets a value indicating whether this window is shown in task switchers.
        /// </summary>
        public bool IsShownInSwitchers
        {
            get => AppWindow.IsShownInSwitchers;
            set => AppWindow.IsShownInSwitchers = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this window is shown in task switchers.
        /// </summary>
        public bool IsAlwaysOnTop
        {
            get => overlappedPresenter.IsAlwaysOnTop;
            set => overlappedPresenter.IsAlwaysOnTop = value;
        }

        /// <summary>
        /// Gets or sets the presenter for the current window
        /// </summary>
        /// <seealso cref="PresenterKind"/>
        /// <seealso cref="PresenterChanged"/>
        public Microsoft.UI.Windowing.AppWindowPresenter Presenter
        {
            get => AppWindow.Presenter;
        }

        /// <summary>
        /// Gets or sets the presenter kind for the current window
        /// </summary>
        /// <seealso cref="Presenter"/>
        /// <seealso cref="PresenterChanged"/>
        public Microsoft.UI.Windowing.AppWindowPresenterKind PresenterKind
        {
            get => AppWindow.Presenter.Kind;
            set {
                if (value is Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped)
                    AppWindow.SetPresenter(overlappedPresenter);
                else
                    AppWindow.SetPresenter(value);
            }
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
        /// Raised if the window position changes.
        /// </summary>
        public event EventHandler? PositionChanged;

        /// <summary>
        /// Raised if the presenter for the window changes.
        /// </summary>
        /// <seealso cref="Presenter"/>
        /// <seealso cref="PresenterKind"/>
        public event EventHandler? PresenterChanged;
    }
}
