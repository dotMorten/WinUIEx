using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace WinUIEx
{
    /// <summary>
    /// A custom WinUI Window with more convenience methods
    /// </summary>
    [ContentProperty(Name = "WindowContent")]
    public partial class WindowEx : Window
    {
        private readonly Grid titleBarArea;
        private readonly Image iconArea;
        private readonly ContentControl titleBarContainer;
        private readonly ContentControl windowArea;
        private readonly Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter;
        private readonly WindowManager _manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowEx"/> class.
        /// </summary>
        public WindowEx()
        {
            _manager = new WindowManager(this);
            overlappedPresenter = Microsoft.UI.Windowing.OverlappedPresenter.Create();
            AppWindow.SetPresenter(overlappedPresenter);

            _manager.PresenterChanged += (s, e) => { OnPresenterChanged(Presenter); PresenterChanged?.Invoke(this, e); };
            _manager.PositionChanged += (s, e) => { OnPositionChanged(e); PositionChanged?.Invoke(this, e); };
            _manager.ZOrderChanged += (s, e) => { OnZOrderChanged(e); ZOrderChanged?.Invoke(this, e); };
            SizeChanged += (s, e) => { OnSizeChanged(e); };
            
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

            windowArea = new ContentControl()
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            Grid.SetRow(windowArea, 1);
            rootContent.Children.Add(windowArea);

            this.Content = rootContent;
            //rootContent.ActualThemeChanged += (s, e) => OnThemeChanged(rootContent.ActualTheme);
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
        public async Task<Windows.UI.Popups.IUICommand> ShowMessageDialogAsync(string content, IList<Windows.UI.Popups.IUICommand>? commands, uint defaultCommandIndex = 0, uint cancelCommandIndex = 1, string title = "")
        {
            if (commands != null && commands.Count > 3)
                throw new InvalidOperationException("A maximum of 3 commands can be specified");

            Windows.UI.Popups.IUICommand defaultCommand = new Windows.UI.Popups.UICommand("OK");
            Windows.UI.Popups.IUICommand? secondaryCommand = null;
            Windows.UI.Popups.IUICommand? cancelCommand = null;
            if (commands != null)
            {
                defaultCommand = commands.Count > defaultCommandIndex ? commands[(int)defaultCommandIndex] : commands.FirstOrDefault() ?? defaultCommand;
                cancelCommand = commands.Count > cancelCommandIndex ? commands[(int)cancelCommandIndex] : null;
                secondaryCommand = commands.Where(c => c != defaultCommand && c != cancelCommand).FirstOrDefault();
            }
            var dialog = new ContentDialog() { XamlRoot = Content.XamlRoot };
            dialog.Content = new TextBlock() { Text = content };
            dialog.Title = title;
            dialog.PrimaryButtonText = defaultCommand.Label;
            if (secondaryCommand != null)
            {
                dialog.SecondaryButtonText = secondaryCommand.Label;
            }
            if (cancelCommand != null)
            {
                dialog.CloseButtonText = cancelCommand.Label;
            }
            var result = await dialog.ShowAsync();
            switch (result)
            {
                case ContentDialogResult.Primary:
                    return defaultCommand;
                case ContentDialogResult.Secondary:
                    return secondaryCommand!;
                case ContentDialogResult.None:
                default:
                    return cancelCommand ?? new Windows.UI.Popups.UICommand();
            }
        }

        /// <summary>
        /// Gets a reference to the AppWindow for the app
        /// </summary>
        public Microsoft.UI.Windowing.AppWindow AppWindow => _manager.AppWindow;

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
        /// Gets or sets the title bar content 
        /// </summary>
        /// <value>The title bar content.</value>
        [Obsolete("TitleBar has been obsoleted, and will be removed in a future release. Use the SetTitleBar method and ExtendsContentIntoTitleBar property instead.")]
        public UIElement? TitleBar
        {
            get { return titleBarContainer.Content as UIElement; }
            set
            {
                if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
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

        /// <summary>
        /// Gets or sets a unique ID used for saving and restoring window size and position
        /// across sessions.
        /// </summary>
        /// <remarks>
        /// The ID must be set before the window activates. The window size and position
        /// will only be restored if the monitor layout hasn't changed between application settings.
        /// The property uses ApplicationData storage, and therefore is currently only functional for
        /// packaged applications.
        /// </remarks>
        public string? PersistenceId
        {
            get => _manager.PersistenceId;
            set => _manager.PersistenceId = value;
        }

        /// <summary>
        /// Gets or sets the Window content 
        /// </summary>
        /// <value>The window content.</value>
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
        /// Gets or sets a value indicating whether this window is always on top.
        /// </summary>
        public bool IsAlwaysOnTop
        {
            get => overlappedPresenter.IsAlwaysOnTop;
            set => overlappedPresenter.IsAlwaysOnTop = value;
        }

        /// <summary>
        /// Gets the presenter for the current window
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

        /// <summary>
        /// Gets or sets the width of the window
        /// </summary>
        public double Width
        {
            get => _manager.Width;
            set => _manager.Width = value;
        }

        /// <summary>
        /// Gets or sets the height of the window
        /// </summary>
        public double Height
        {
            get => _manager.Height;
            set => _manager.Height = value;
        }

        /// <summary>
        /// Gets or sets the minimum width of this window
        /// </summary>
        /// <remarks>A window is currently set to a minimum of 139 pixels.</remarks>
        public double MinWidth
        {
            get => _manager.MinWidth;
            set => _manager.MinWidth = value;
        }

        /// <summary>
        /// Gets or sets the minimum height of this window
        /// </summary>
        /// <remarks>A window is currently set to a minimum of 39 pixels.</remarks>
        public double MinHeight
        {
            get => _manager.MinHeight;
            set => _manager.MinHeight = value;
        }

        /// <summary>
        /// Gets or sets the system backdrop for the window.
        /// </summary>
        /// <seealso cref="MicaSystemBackdrop"/>
        /// <seealso cref="AcrylicSystemBackdrop"/>
        public SystemBackdrop Backdrop
        {
            get => _manager.Backdrop;
            set => _manager.Backdrop = value;
        }

        #region Window events and corresponding virtual methods

        /// <summary>
        /// Raised if the window position changes.
        /// </summary>
        /// <seealso cref="Microsoft.UI.Windowing.AppWindow.Position"/>
        public event EventHandler<Windows.Graphics.PointInt32>? PositionChanged;

        /// <summary>
        /// Called when the window position changed.
        /// </summary>
        /// <param name="position">The current position of the window in screen coordinates.</param>
        /// <seealso cref="Microsoft.UI.Windowing.AppWindow.Position"/>
        protected virtual void OnPositionChanged(Windows.Graphics.PointInt32 position)
        {
        }

        /// <summary>
        /// Raised if the presenter for the window changed.
        /// </summary>
        /// <seealso cref="Presenter"/>
        /// <seealso cref="PresenterKind"/>
        public event EventHandler<Microsoft.UI.Windowing.AppWindowPresenter>? PresenterChanged;

        /// <summary>
        /// Called when the presenter for the window changed.
        /// </summary>
        /// <param name="newPresenter">The new presenter.</param>
        /// <seealso cref="Presenter"/>
        /// <seealso cref="PresenterKind"/>
        /// <seealso cref="Microsoft.UI.Windowing.AppWindow.Presenter"/>
        protected virtual void OnPresenterChanged(Microsoft.UI.Windowing.AppWindowPresenter newPresenter)
        {
        }

        /// <summary>
        /// Raised if the Z order of the window changed.
        /// </summary>
        public event EventHandler<ZOrderInfo>? ZOrderChanged;

        /// <summary>
        /// Called when the Z order of the window changed.
        /// </summary>
        /// <param name="info">Object describing the current new ZOrder of the window</param>
        protected virtual void OnZOrderChanged(ZOrderInfo info)
        {
        }

        private void OnSizeChanged(WindowSizeChangedEventArgs e)
        {
            var dpi = this.GetDpiForWindow();
            var result = OnSizeChanged(new Windows.Foundation.Size(e.Size.Width / dpi * 96, e.Size.Height / dpi * 96));
            if (result)
                e.Handled = true;
        }

        /// <summary>
        /// Called when the size of the window changes.
        /// </summary>
        /// <param name="newSize">The new size of the window in device independent units.</param>
        /// <returns>True if the resize event should be marked handled.</returns>
        /// <remarks>
        /// While this event is equivalent to the <see cref="Window.SizeChanged"/> event,
        /// the units provided here are in device independent units and not screen pixels.
        /// </remarks>
        protected virtual bool OnSizeChanged(Windows.Foundation.Size newSize) => false;

/*
        /// <summary>
        /// Called when the actual theme changes
        /// </summary>
        /// <param name="theme">The new theme</param>
        /// <seealso cref="FrameworkElement.ActualTheme"/>
        /// <seealso cref="ActualTheme"/>
        protected virtual void OnThemeChanged(ElementTheme theme)
        {            
        }

        /// <summary>
        /// The actual theme for the window
        /// </summary>
        /// <seealso cref="OnThemeChanged(ElementTheme)"/>
        public ElementTheme ActualTheme => windowArea.ActualTheme;
*/
        #endregion Window events and corresponding virtual methods
    }
}
