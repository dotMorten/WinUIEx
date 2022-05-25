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
using Windows.Storage;
using WinUIEx.Messaging;

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
        private readonly WindowMessageMonitor mon;
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
#if EXPERIMENTAL
            titleBarContainer.SizeChanged += (s,e) => UpdateDragRectangles();
#endif

            windowArea = new ContentControl()
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            Grid.SetRow(windowArea, 1);
            rootContent.Children.Add(windowArea);

            this.Content = rootContent;
            rootContent.Loaded += RootContent_Loaded;
            AppWindow.Changed += AppWindow_Changed;
            mon = new WindowMessageMonitor(this);
            mon.WindowMessageReceived += OnWindowMessage;
            this.Closed += WindowEx_Closed;
            this.Activated += WindowEx_Activated;
        }

        private unsafe void OnWindowMessage(object? sender, Messaging.WindowMessageEventArgs e)
        {
            switch(e.MessageType)
            {
                case WindowsMessages.WM_GETMINMAXINFO:
                    {
                        // Restrict min-size
                        MINMAXINFO* rect2 = (MINMAXINFO*)e.Message.LParam;
                        var currentDpi = this.GetDpiForWindow();
                        rect2->ptMinTrackSize.x = (int)(Math.Max(MinWidth * (currentDpi / 96f), rect2->ptMinTrackSize.x));
                        rect2->ptMinTrackSize.y = (int)(Math.Max(MinHeight * (currentDpi / 96f), rect2->ptMinTrackSize.y));
                    }
                    break;
                case WindowsMessages.WM_DPICHANGED:
                    {
                        // Resize to account for DPI change
                        var suggestedRect = (Windows.Win32.Foundation.RECT*)e.Message.LParam;
                        bool result = Windows.Win32.PInvoke.SetWindowPos(new Windows.Win32.Foundation.HWND(this.GetWindowHandle()), new Windows.Win32.Foundation.HWND(), suggestedRect->left, suggestedRect->top,
                            suggestedRect->right - suggestedRect->left, suggestedRect->bottom - suggestedRect->top, Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOZORDER | Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
                        break;
                    }
            }
        }

        private struct MINMAXINFO
        {
#pragma warning disable CS0649
            public Windows.Win32.Foundation.POINT ptReserved;
            public Windows.Win32.Foundation.POINT ptMaxSize;
            public Windows.Win32.Foundation.POINT ptMaxPosition;
            public Windows.Win32.Foundation.POINT ptMinTrackSize;
            public Windows.Win32.Foundation.POINT ptMaxTrackSize;
#pragma warning restore CS0649
        }

        private void AppWindow_Changed(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
        {
            if (args.DidPositionChange)
                PositionChanged?.Invoke(this, EventArgs.Empty);
            if(args.DidSizeChange)
            {
#if EXPERIMENTAL
                UpdateDragRectangles();
#endif
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
            var dialog = this.CreateMessageDialog(content, title);
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
        /// Gets or sets the title bar content 
        /// </summary>
        /// <value>The title bar content.</value>
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

#if EXPERIMENTAL
        private void UpdateDragRectangles()
        {
            if (base.ExtendsContentIntoTitleBar)
            {
                var ri = AppWindow.TitleBar.RightInset;
                var li = AppWindow.TitleBar.LeftInset;
                var _width = AppWindow.Size.Width;
                var _height = AppWindow.Size.Height;
                List<Windows.Foundation.Rect> bounds = new List<Windows.Foundation.Rect>();
                if (TitleBar is not null)
                {
                    var transform = TitleBar.TransformToVisual((UIElement)Content);
                    foreach (var elm in GetInteractiveUIElement(TitleBar))
                    {
                        if (elm.ActualSize.X > 0 && elm.ActualSize.Y > 0)
                        {
                            var bound = transform.TransformBounds(new Windows.Foundation.Rect(elm.ActualOffset.X, elm.ActualOffset.Y, elm.ActualSize.X, elm.ActualSize.Y));
                            bounds.Add(bound);
                        }
                    }
                }
                double start = 0;
                var height = TitleBar?.ActualSize.Y ?? 0;
                if (height == 0) return;
                List<Windows.Graphics.RectInt32> rects = new List<Windows.Graphics.RectInt32>(1);
                foreach (var bound in bounds)
                {
                    if (bound.X > start)
                    {
                        var w = bound.Width;
                        if (w + bound.X > _width)
                            w = _width - bound.X;
                        if (w > 0)
                        {
                            rects.Add(new Windows.Graphics.RectInt32((int)start, 0, (int)(bound.X - start), (int)height));
                            start = bound.X + w;
                        }
                    }
                }
                if(start < _width)
                    rects.Add(new Windows.Graphics.RectInt32((int)start, 0, (int)(_width - start), (int)height));
                AppWindow.TitleBar.SetDragRectangles(rects.ToArray());
            }

        }
        private static IEnumerable<FrameworkElement> GetInteractiveUIElement(UIElement element)
        {
            if (element is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    foreach (var ce in GetInteractiveUIElement(child))
                        yield return ce;
                }
            }
            else if (element is FrameworkElement fe && fe.IsHitTestVisible)
            {
                if (element is Microsoft.UI.Xaml.Controls.Primitives.ButtonBase || element is TextBox)
                    yield return fe;
            }
        }
#endif

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

        /// <summary>
        /// Gets or sets the width of the window
        /// </summary>
        public double Width
        {
            get { return AppWindow.Size.Width / (this.GetDpiForWindow() / 96d); }
            set
            {
                this.SetWindowSize(value, Height);
            }
        }

        /// <summary>
        /// Gets or sets the height of the window
        /// </summary>
        public double Height
        {
            get { return AppWindow.Size.Height / (this.GetDpiForWindow() / 96d); }
            set
            {
                this.SetWindowSize(Width, value);
            }
        }

        private double _minWidth = 136;

        /// <summary>
        /// Gets or sets the minimum width of this window
        /// </summary>
        /// <remarks>A window is currently set to a minimum of 139 pixels.</remarks>
        public double MinWidth
        {
            get => _minWidth; 
            set
            {
                _minWidth = value;
                if (Width < value)
                    Width = value;
            }
        }

        private double _minHeight = 39;

        /// <summary>
        /// Gets or sets the minimum height of this window
        /// </summary>
        /// <remarks>A window is currently set to a minimum of 39 pixels.</remarks>
        public double MinHeight
        {
            get => _minHeight;
            set {
                _minHeight = value;
                if (Height < value)
                    Height = value;
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

        public WindowPersistence Persistence { get; set; }

        private void WindowEx_Activated(object sender, WindowActivatedEventArgs args)
        {
            LoadPersistance();
        }

        private void WindowEx_Closed(object sender, WindowEventArgs args)
        {
            if(Persistence != null)
            {
                // Store monitor info - we won't restore on original screen if original monitor layout has changed
                using var data = new System.IO.MemoryStream();
                using var sw = new System.IO.BinaryWriter(data);
                var monitors = MonitorInfo.GetDisplayMonitors();
                sw.Write(monitors.Count);
                foreach (var monitor in monitors)
                {
                    sw.Write(monitor.Name);
                    sw.Write(monitor.RectMonitor.Left);
                    sw.Write(monitor.RectMonitor.Top);
                    sw.Write(monitor.RectMonitor.Right);
                    sw.Write(monitor.RectMonitor.Bottom);
                }
                sw.Write(this.Width);
                sw.Write(this.Height);
                sw.Write(AppWindow.Position.X);
                sw.Write(AppWindow.Position.Y);
                sw.Flush();
                var winuiExSettings = ApplicationData.Current?.LocalSettings?.CreateContainer("WinUIEx", ApplicationDataCreateDisposition.Always);
                if (winuiExSettings != null)
                    winuiExSettings.Values[$"WindowPersistance_{Persistence.PersistanceId}"] = Convert.ToBase64String(data.ToArray());
            }
        }
        private bool isLoaded = false;
        private void LoadPersistance()
        {
            if (Persistence != null && !isLoaded)
            {
                try
                {
                    byte[]? data = null;
                    var winuiExSettings = ApplicationData.Current?.LocalSettings?.CreateContainer("WinUIEx", ApplicationDataCreateDisposition.Existing);
                    if (winuiExSettings is not null && winuiExSettings.Values.ContainsKey($"WindowPersistance_{Persistence.PersistanceId}"))
                    {
                        var base64 = winuiExSettings.Values[$"WindowPersistance_{Persistence.PersistanceId}"] as string;
                        data = Convert.FromBase64String(base64);
                    }
                    if (data is null)
                        return;
                    System.IO.BinaryReader br = new System.IO.BinaryReader(new System.IO.MemoryStream(data));
                    int monitorCount = br.ReadInt32();
                    for (int i = 0; i < monitorCount; i++)
                    {
                        string name = br.ReadString();
                        double left = br.ReadDouble();
                        double top = br.ReadDouble();
                        double right = br.ReadDouble();
                        double bottom = br.ReadDouble();
                    }
                    double width = br.ReadDouble();
                    double height = br.ReadDouble();
                    int x = br.ReadInt32();
                    int y = br.ReadInt32();

                    this.MoveAndResize(x, y, width, height);
                    isLoaded = true;
                }
                catch { }
            }
        }
    }
    public class WindowPersistence
    {
        public WindowPersistence(string persistanceId)
        {
            //var settings = ApplicationData.Current.LocalSettings;
            if (string.IsNullOrEmpty(persistanceId ?? throw new ArgumentNullException(nameof(persistanceId))))
                throw new ArgumentException("persistanceId cannot be empty", nameof(persistanceId));
            PersistanceId = persistanceId;
            
        }
        public string PersistanceId { get; }
        public bool RestoreSize { get; set; } = true;
        public bool RestoreLocation { get; set; } = true;
        public bool PersistMonitor { get; set; } = true;
        
    }
}
