using System.Collections.Generic;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace WinUIEx;

/// <summary>
/// TitleBar control.
/// </summary>
[System.Obsolete("Use Windows App SDK's TitleBar control instead.")]
public partial class TitleBar : Control
{
    private double m_compactModeThresholdWidth = 0.0;
    const string s_leftPaddingColumnName = "LeftPaddingColumn";
    const string s_rightPaddingColumnName = "RightPaddingColumn";
    const string s_layoutRootPartName = "PART_LayoutRoot";
    const string s_backButtonPartName = "PART_BackButton";
    const string s_paneToggleButtonPartName = "PART_PaneToggleButton";
    const string s_headerContentPresenterPartName = "PART_HeaderContentPresenter";
    const string s_contentPresenterGridPartName = "PART_ContentPresenterGrid";
    const string s_contentPresenterPartName = "PART_ContentPresenter";
    const string s_footerPresenterPartName = "PART_FooterContentPresenter";
    const string s_compactVisualStateName = "Compact";
    const string s_expandedVisualStateName = "Expanded";
    const string s_compactHeightVisualStateName = "CompactHeight";
    const string s_expandedHeightVisualStateName = "ExpandedHeight";
    const string s_defaultSpacingVisualStateName = "DefaultSpacing";
    const string s_negativeInsetVisualStateName = "NegativeInsetSpacing";
    const string s_iconVisibleVisualStateName = "IconVisible";
    const string s_iconCollapsedVisualStateName = "IconCollapsed";
    const string s_iconDeactivatedVisualStateName = "IconDeactivated";
    const string s_backButtonVisibleVisualStateName = "BackButtonVisible";
    const string s_backButtonCollapsedVisualStateName = "BackButtonCollapsed";
    const string s_backButtonDeactivatedVisualStateName = "BackButtonDeactivated";
    const string s_paneToggleButtonVisibleVisualStateName = "PaneToggleButtonVisible";
    const string s_paneToggleButtonCollapsedVisualStateName = "PaneToggleButtonCollapsed";
    const string s_paneToggleButtonDeactivatedVisualStateName = "PaneToggleButtonDeactivated";
    const string s_titleTextVisibleVisualStateName = "TitleTextVisible";
    const string s_titleTextCollapsedVisualStateName = "TitleTextCollapsed";
    const string s_titleTextDeactivatedVisualStateName = "TitleTextDeactivated";
    const string s_subtitleTextVisibleVisualStateName = "SubtitleTextVisible";
    const string s_subtitleTextCollapsedVisualStateName = "SubtitleTextCollapsed";
    const string s_subtitleTextDeactivatedVisualStateName = "SubtitleTextDeactivated";
    const string s_headerVisibleVisualStateName = "HeaderVisible";
    const string s_headerCollapsedVisualStateName = "HeaderCollapsed";
    const string s_headerDeactivatedVisualStateName = "HeaderDeactivated";
    const string s_contentVisibleVisualStateName = "ContentVisible";
    const string s_contentCollapsedVisualStateName = "ContentCollapsed";
    const string s_contentDeactivatedVisualStateName = "ContentDeactivated";
    const string s_footerVisibleVisualStateName = "FooterVisible";
    const string s_footerCollapsedVisualStateName = "FooterCollapsed";
    const string s_footerDeactivatedVisualStateName = "FooterDeactivated";
    const string s_titleBarCaptionButtonForegroundColorName = "TitleBarCaptionButtonForegroundColor";
    const string s_titleBarCaptionButtonBackgroundColorName = "TitleBarCaptionButtonBackgroundColor";
    const string s_titleBarCaptionButtonHoverForegroundColorName = "TitleBarCaptionButtonHoverForegroundColor";
    const string s_titleBarCaptionButtonHoverBackgroundColorName = "TitleBarCaptionButtonHoverBackgroundColor";
    const string s_titleBarCaptionButtonPressedForegroundColorName = "TitleBarCaptionButtonPressedForegroundColor";
    const string s_titleBarCaptionButtonPressedBackgroundColorName = "TitleBarCaptionButtonPressedBackgroundColor";
    const string s_titleBarCaptionButtonInactiveForegroundColorName = "TitleBarCaptionButtonInactiveForegroundColor";

    private ColumnDefinition? m_leftPaddingColumn;
    private ColumnDefinition? m_rightPaddingColumn;
    private Button? m_backButton;
    private Button? m_paneToggleButton;
    private Grid? m_contentAreaGrid;
    private FrameworkElement? m_headerArea;
    private FrameworkElement? m_contentArea;
    private FrameworkElement? m_footerArea;
    private InputActivationListener? m_inputActivationListener;

    /// <summary>
    /// Initializes a new instance of the <see cref="TitleBar"/> class.
    /// </summary>
    public TitleBar()
    {
        SetValue(TemplateSettingsProperty, new TitleBarTemplateSettings());
        this.DefaultStyleKey = typeof(TitleBar);
        this.SizeChanged += OnSizeChanged;
        this.LayoutUpdated += OnLayoutUpdated;
        ActualThemeChanged += (s, e) => UpdateTheme();
    }

    /// <summary>
    /// Finalizer
    /// </summary>
    ~TitleBar()
    {
        if (m_inputActivationListener != null)
            m_inputActivationListener.InputActivationChanged -= OnInputActivationChanged;
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        m_leftPaddingColumn = GetTemplateChild(s_leftPaddingColumnName) as ColumnDefinition;
        m_rightPaddingColumn = GetTemplateChild(s_rightPaddingColumnName) as ColumnDefinition;
        if (XamlRoot?.ContentIslandEnvironment is not null)
        {
            var appWindowId = XamlRoot.ContentIslandEnvironment.AppWindowId;
            if (appWindowId.Value != 0)
            {
                m_inputActivationListener = Microsoft.UI.Input.InputActivationListener.GetForWindowId(appWindowId);
                m_inputActivationListener.InputActivationChanged += OnInputActivationChanged;
            }
        }
        UpdateHeight();
        UpdatePadding();
        UpdateIcon();
        UpdateBackButton();
        UpdatePaneToggleButton();
        UpdateTheme();
        UpdateTitle();
        UpdateSubtitle();
        UpdateHeader();
        UpdateContent();
        UpdateFooter();
        UpdateInteractableElementsList();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if(Content is not null)
        {
            if(m_contentArea is not null && m_contentAreaGrid is not null)
            {
                if(m_compactModeThresholdWidth != 0 && m_contentArea.DesiredSize.Width > m_contentAreaGrid.ActualWidth)
                {
                    m_compactModeThresholdWidth = e.NewSize.Width;
                }
                else if(e.NewSize.Width >= m_compactModeThresholdWidth)
                {
                    m_compactModeThresholdWidth = 0;
                    VisualStateManager.GoToState(this, s_expandedVisualStateName, false);
                    UpdateTitle();
                    UpdateSubtitle();
                }
            }
        }
        UpdateDragRegion();
    }

    private void OnLayoutUpdated(object? sender, object e)
    {
        UpdateDragRegion();
    }

    private void OnInputActivationChanged(InputActivationListener sender, InputActivationListenerActivationChangedEventArgs args)
    {
        bool isDeactivate = sender.State == InputActivationState.Deactivated;
        if (IsBackButtonVisible && IsBackEnabled)
        {
            VisualStateManager.GoToState(this, isDeactivate ? s_backButtonDeactivatedVisualStateName : s_backButtonVisibleVisualStateName, false);
        }

        if (IsPaneToggleButtonVisible)
        {
            VisualStateManager.GoToState(this, isDeactivate ? s_paneToggleButtonDeactivatedVisualStateName : s_paneToggleButtonVisibleVisualStateName, false);
        }

        if (IconSource is not null)
        {
            VisualStateManager.GoToState(this, isDeactivate ? s_iconDeactivatedVisualStateName : s_iconVisibleVisualStateName, false);
        }

        if (!string.IsNullOrEmpty(Title))
        {
            VisualStateManager.GoToState(this, isDeactivate ? s_titleTextDeactivatedVisualStateName : s_titleTextVisibleVisualStateName, false);
        }

        if (!string.IsNullOrEmpty(Subtitle))
        {
            VisualStateManager.GoToState(this, isDeactivate ? s_subtitleTextDeactivatedVisualStateName : s_subtitleTextVisibleVisualStateName, false);
        }

        if (Header is not null)
        {
            VisualStateManager.GoToState(this, isDeactivate ? s_headerDeactivatedVisualStateName : s_headerVisibleVisualStateName, false);
        }

        if (Content is not null)
        {
            VisualStateManager.GoToState(this, isDeactivate ? s_contentDeactivatedVisualStateName : s_contentVisibleVisualStateName, false);
        }

        if (Footer is not null)
        {
            VisualStateManager.GoToState(this, isDeactivate ? s_footerDeactivatedVisualStateName : s_footerVisibleVisualStateName, false);
        }
    }

    private void OnBackButtonClick(object sender, RoutedEventArgs e)
    {
        BackRequested?.Invoke(this, e);
    }

    private void OnPaneToggleButtonClick(object sender, RoutedEventArgs e)
    {
        PaneToggleRequested?.Invoke(this, e);
    }

    private void UpdateIcon()
    {
        if ((IconSource is not null))
        {
            TemplateSettings.IconElement = MakeIconElementFrom(IconSource);
            VisualStateManager.GoToState(this, s_iconVisibleVisualStateName, false);
        }
        else
        {
            TemplateSettings.IconElement = null;
            VisualStateManager.GoToState(this, s_iconCollapsedVisualStateName, false);
        }
    }

    private IconElement? MakeIconElementFrom(IconSource iconSource)
    {
        if (iconSource is FontIconSource fontIconSource)
        {
            FontIcon fontIcon = new FontIcon();

            fontIcon.Glyph = fontIconSource.Glyph;
            fontIcon.FontSize = fontIconSource.FontSize;
            if (fontIconSource.Foreground != null)
            {
                fontIcon.Foreground = fontIconSource.Foreground;
            }

            if (fontIconSource.FontFamily != null)
            {
                fontIcon.FontFamily = fontIconSource.FontFamily;
            }

            fontIcon.FontWeight = fontIconSource.FontWeight;
            fontIcon.FontStyle = fontIconSource.FontStyle;
            fontIcon.IsTextScaleFactorEnabled = fontIconSource.IsTextScaleFactorEnabled;
            fontIcon.MirroredWhenRightToLeft = fontIconSource.MirroredWhenRightToLeft;

            return fontIcon;
        }
        else if (iconSource is SymbolIconSource symbolIconSource)
        {
            SymbolIcon symbolIcon = new SymbolIcon();
            symbolIcon.Symbol = symbolIconSource.Symbol;
            if (symbolIconSource.Foreground != null)
            {
                symbolIcon.Foreground = symbolIconSource.Foreground;
            }
            return symbolIcon;
        }
        // Note: this check must be done before BitmapIconSource
        // since ImageIconSource uses BitmapIconSource as a composable interface,
        // so a ImageIconSource will also register as a BitmapIconSource.
        else if (iconSource is ImageIconSource imageIconSource)
        {
            ImageIcon imageIcon = new ImageIcon();
            if (imageIconSource.ImageSource != null)
            {
                imageIcon.Source = imageIconSource.ImageSource;
            }
            if (imageIconSource.Foreground != null)
            {
                imageIcon.Foreground = imageIconSource.Foreground;
            }
            return imageIcon;
        }
        else if (iconSource is BitmapIconSource bitmapIconSource)
        {
            BitmapIcon bitmapIcon = new BitmapIcon();

            if (bitmapIconSource.UriSource != null)
            {
                bitmapIcon.UriSource = bitmapIconSource.UriSource;
            }

            bitmapIcon.ShowAsMonochrome = bitmapIconSource.ShowAsMonochrome;

            if (bitmapIconSource.Foreground != null)
            {
                bitmapIcon.Foreground = bitmapIconSource.Foreground;
            }

            return bitmapIcon;
        }
        // Note: this check must be done before PathIconSource
        // since AnimatedIconSource uses PathIconSource as a composable interface,
        // so a AnimatedIconSource will also register as a PathIconSource.
        else if (iconSource is AnimatedIconSource animatedIconSource)
        {
            AnimatedIcon animatedIcon = new AnimatedIcon();
            if (animatedIconSource.Source != null)
            {
                animatedIcon.Source = animatedIconSource.Source;
            }
            if (animatedIconSource.FallbackIconSource != null)
            {
                animatedIcon.FallbackIconSource = animatedIconSource.FallbackIconSource;
            }
            if (animatedIconSource.Foreground != null)
            {
                animatedIcon.Foreground = animatedIconSource.Foreground;
            }
            return animatedIcon;
        }
        else if (iconSource is PathIconSource pathIconSource)
        {
            PathIcon pathIcon = new PathIcon();

            if (pathIconSource.Data != null)
            {
                pathIcon.Data = pathIconSource.Data;
            }
            if (pathIconSource.Foreground != null)
            {
                pathIcon.Foreground = pathIconSource.Foreground;
            }
            return pathIcon;
        }

        return null;
    }

    private void UpdateBackButton()
    {
        if (IsBackButtonVisible)
        {
            if (m_backButton is null)
            {
                LoadBackButton();
            }

            VisualStateManager.GoToState(this, s_backButtonVisibleVisualStateName, false);
        }
        else
        {
            VisualStateManager.GoToState(this, s_backButtonCollapsedVisualStateName, false);
        }

        UpdateInteractableElementsList();
        UpdateHeaderSpacing();
    }

    private void UpdatePaneToggleButton() 
    {
        if (IsPaneToggleButtonVisible)
        {
            if (m_paneToggleButton is null)
            {
                LoadPaneToggleButton();
            }

            VisualStateManager.GoToState(this, s_paneToggleButtonVisibleVisualStateName, false);
        }
        else
        {
            VisualStateManager.GoToState(this, s_paneToggleButtonCollapsedVisualStateName, false);
        }

        UpdateInteractableElementsList();
        UpdateHeaderSpacing();
    }

    private void UpdateHeight()
    {
        VisualStateManager.GoToState(this, (Content is null && Header is null && Footer is null) ? s_compactHeightVisualStateName : s_expandedHeightVisualStateName, false);
    }

    private void UpdatePadding()
    {
        if (XamlRoot?.ContentIslandEnvironment is not null)
        {
            var appWindowId = XamlRoot.ContentIslandEnvironment.AppWindowId;
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(appWindowId);

            if (appWindow.TitleBar is not null)
            {
                // TODO 50724421: Bind to appTitleBar Left and Right inset changed event.
                if (m_leftPaddingColumn is not null)
                {
                    m_leftPaddingColumn.Width = new GridLength(appWindow.TitleBar.LeftInset);
                }

                if (m_rightPaddingColumn is not null)
                {
                    m_rightPaddingColumn.Width = new GridLength(appWindow.TitleBar.RightInset);
                }
            }
        }
    }

    private object? ResourceLookup(string key)
    {
        // TODO: This method is generally not working since WinUI doesn't allow us to look up resources defined in Generic.xaml
        return ResourceLookup(key, this, ActualTheme) ?? ResourceLookup(key, Application.Current.Resources, ActualTheme) ?? null;
    }
    private static object? ResourceLookup(string key, FrameworkElement control, ElementTheme theme)
    {
        var resource = ResourceLookup(key, control.Resources, theme);
        if (resource is not null)
            return resource;
        if(control.Parent is FrameworkElement parent)
        {
            return ResourceLookup(key, parent, theme);
        }
        return null;
    }
    private static object? ResourceLookup(string key, ResourceDictionary dictionary, ElementTheme theme)
    {
        if (dictionary.ContainsKey(key))
            return dictionary[key];
        if(dictionary.ThemeDictionaries.TryGetValue(theme.ToString(), out var themeDictionary) && themeDictionary is ResourceDictionary d && d.ContainsKey(key))
            return d[key];
        foreach (var mdictionary in dictionary.MergedDictionaries)
        {
            var resource = ResourceLookup(key, mdictionary, theme);
            if (resource is not null)
                return resource;
        }
        return null;
    }

    private void UpdateTheme()
    {
        if (XamlRoot?.ContentIslandEnvironment is not null)
        {
            var appWindowId = XamlRoot.ContentIslandEnvironment.AppWindowId;
            var appTitleBar = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(appWindowId)?.TitleBar;
            // AppWindow TitleBar's caption buttons does not update colors with theme change.
            // We need to set them here.
            
            if (appTitleBar is not null)
            {
                // Define fallback colors because WinUI doesn't give us full resource lookup support, so the lookups generally fail
                var theme = this.ActualTheme;
                Windows.UI.Color defaultForeground = ActualTheme == ElementTheme.Dark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Black;
                Windows.UI.Color defaultHoverBackground = ActualTheme == ElementTheme.Dark ? Windows.UI.Color.FromArgb(0x0f,0xff,0xff,0xff) : Windows.UI.Color.FromArgb(0x09, 0x00, 0x00, 0x00);
                Windows.UI.Color defaultPressedBackground = ActualTheme == ElementTheme.Dark ? Windows.UI.Color.FromArgb(0x0a, 0xff, 0xff, 0xff) : Windows.UI.Color.FromArgb(0x06, 0x00, 0x00, 0x00);
                
                // Rest colors.
                var buttonForegroundColor = (Windows.UI.Color?)ResourceLookup(s_titleBarCaptionButtonForegroundColorName);
                appTitleBar.ButtonForegroundColor = buttonForegroundColor ?? defaultForeground;

                var buttonBackgroundColor = (Windows.UI.Color?)ResourceLookup(s_titleBarCaptionButtonBackgroundColorName);
                appTitleBar.ButtonBackgroundColor = buttonBackgroundColor ?? Microsoft.UI.Colors.Transparent;
                appTitleBar.ButtonInactiveBackgroundColor = buttonBackgroundColor ?? Microsoft.UI.Colors.Transparent;

                // Hover colors.
                var buttonHoverForegroundColor = (Windows.UI.Color?)ResourceLookup(s_titleBarCaptionButtonHoverForegroundColorName);
                appTitleBar.ButtonHoverForegroundColor = buttonHoverForegroundColor ?? defaultForeground;

                var buttonHoverBackgroundColor = (Windows.UI.Color?)ResourceLookup(s_titleBarCaptionButtonHoverBackgroundColorName);
                appTitleBar.ButtonHoverBackgroundColor = buttonHoverBackgroundColor ?? defaultHoverBackground;

                // Pressed colors.
                var buttonPressedForegroundColor = (Windows.UI.Color?)ResourceLookup(s_titleBarCaptionButtonPressedForegroundColorName);
                appTitleBar.ButtonPressedForegroundColor = buttonPressedForegroundColor ?? defaultForeground;

                var buttonPressedBackgroundColor = (Windows.UI.Color?)ResourceLookup(s_titleBarCaptionButtonPressedBackgroundColorName);
                appTitleBar.ButtonPressedBackgroundColor = buttonPressedBackgroundColor ?? defaultPressedBackground;
                
                // Inactive foreground.
                var buttonInactiveForegroundColor = (Windows.UI.Color?)ResourceLookup(s_titleBarCaptionButtonInactiveForegroundColorName);
                appTitleBar.ButtonInactiveForegroundColor = buttonInactiveForegroundColor ?? (ActualTheme == ElementTheme.Dark ? Windows.UI.Color.FromArgb(255, 0x71, 0x71, 0x71) : Windows.UI.Color.FromArgb(255, 0x9b, 0x9b, 0x9b));
            }
        }
    }

    private void UpdateTitle()
    {
        if (string.IsNullOrEmpty(Title))
        {
            VisualStateManager.GoToState(this, s_titleTextCollapsedVisualStateName, false);
        }
        else
        {
            VisualStateManager.GoToState(this, s_titleTextVisibleVisualStateName, false);
        }
    }

    private void UpdateSubtitle()
    {
        if (string.IsNullOrEmpty(Subtitle))
        {
            VisualStateManager.GoToState(this, s_subtitleTextCollapsedVisualStateName, false);
        }
        else
        {
            VisualStateManager.GoToState(this, s_subtitleTextVisibleVisualStateName, false);
        }
    }

    private void UpdateHeader() 
    {
        if (Header is null)
        {
            VisualStateManager.GoToState(this, s_headerCollapsedVisualStateName, false);
        }
        else
        {
            if (m_headerArea is null)
            {
                m_headerArea = GetTemplateChild(s_headerContentPresenterPartName) as FrameworkElement;
            }
            VisualStateManager.GoToState(this, s_headerVisibleVisualStateName, false);
        }

        UpdateHeight();
        UpdateInteractableElementsList();
    }
    private void UpdateContent() 
    {
        if (Content is null)
        {
            VisualStateManager.GoToState(this, s_contentCollapsedVisualStateName, false);
        }
        else
        {
            if (m_contentArea is null)
            {
                m_contentAreaGrid = GetTemplateChild(s_contentPresenterGridPartName) as Grid;
                m_contentArea = GetTemplateChild(s_contentPresenterPartName) as FrameworkElement;
            }

            VisualStateManager.GoToState(this, s_contentVisibleVisualStateName, false);
        }

        UpdateHeight();
        UpdateInteractableElementsList();
    }
    private void UpdateFooter()
    {
        if (Footer is null)
        {
            VisualStateManager.GoToState(this, s_footerCollapsedVisualStateName, false);
        }
        else
        {
            if (m_footerArea is null)
            {
                m_footerArea = GetTemplateChild(s_footerPresenterPartName) as FrameworkElement;
            }
            VisualStateManager.GoToState(this, s_footerVisibleVisualStateName, false);
        }

        UpdateHeight();
        UpdateInteractableElementsList();
    }

    private readonly List<FrameworkElement> m_interactableElementsList = new List<FrameworkElement>();

    private void UpdateDragRegion()
    {
        if (XamlRoot?.ContentIslandEnvironment is not null)
        {
            var appWindowId = XamlRoot.ContentIslandEnvironment.AppWindowId;
            var nonClientPointerSource = InputNonClientPointerSource.GetForWindowId(appWindowId);

            if (m_interactableElementsList.Count > 0)
            {
                List<Windows.Graphics.RectInt32> passthroughRects = new List<Windows.Graphics.RectInt32>();

                // Get rects for each interactable element in TitleBar.
                foreach (var frameworkElement in m_interactableElementsList)
                {
                    var transformBounds = frameworkElement.TransformToVisual(null);
                    var width = frameworkElement.ActualWidth;
                    var height = frameworkElement.ActualHeight;
                    var bounds = transformBounds.TransformBounds(new Windows.Foundation.Rect(0.0f, 0.0f, width, height));

                    if (bounds.X < 0 || bounds.Y < 0)
                    {
                        continue;
                    }

                    var scale = XamlRoot.RasterizationScale;
                    var transparentRect = new Windows.Graphics.RectInt32(
                    (int)(bounds.X * scale),
                    (int)(bounds.Y * scale),
                    (int)(bounds.Width * scale),
                    (int)(bounds.Height * scale));

                    passthroughRects.Add(transparentRect);
                }

                // Set list of rects as passthrough regions for the non-client area.
                nonClientPointerSource.SetRegionRects(NonClientRegionKind.Passthrough, [.. passthroughRects]);
            }
            else
            {
                // There is no interactable areas. Clear previous passthrough rects.
                nonClientPointerSource.ClearRegionRects(NonClientRegionKind.Passthrough);
            }
        }
    }

    private void UpdateInteractableElementsList()
    {
        m_interactableElementsList.Clear();

        if (IsBackButtonVisible && IsBackEnabled && m_backButton is not null)
        {
            m_interactableElementsList.Add(m_backButton);
        }

        if (IsPaneToggleButtonVisible && m_paneToggleButton is not null)
        {
            m_interactableElementsList.Add(m_paneToggleButton);
        }

        if (Header is not null && m_headerArea is not null)
        {
            m_interactableElementsList.Add(m_headerArea);
        }

        if (Content is not null && m_contentArea is not null)
        {
            m_interactableElementsList.Add(m_contentArea);
        }
        

        if (Footer is not null && m_footerArea is not null)
        {
            m_interactableElementsList.Add(m_footerArea);
        }
    }
    private void UpdateHeaderSpacing()
    {
        VisualStateManager.GoToState(this, IsBackButtonVisible == IsPaneToggleButtonVisible ? s_defaultSpacingVisualStateName : s_negativeInsetVisualStateName, false);
    }
 
    private void LoadBackButton()
    {
        m_backButton = GetTemplateChild(s_backButtonPartName) as Button;

        if (m_backButton is not null)
        {
            m_backButton.Click += OnBackButtonClick;
            // Do localization for the back button
            if (string.IsNullOrEmpty(AutomationProperties.GetName(m_backButton)))
            {
                AutomationProperties.SetName(m_backButton, "Back");
            }

            // Setup the tooltip for the back button
            var tooltip = new ToolTip();
            tooltip.Content = "Back";
            ToolTipService.SetToolTip(m_backButton, tooltip);
        }
    }

    private void LoadPaneToggleButton()
    {
        m_paneToggleButton = GetTemplateChild(s_paneToggleButtonPartName) as Button;

        if (m_paneToggleButton is not null)
        {
            m_paneToggleButton.Click += OnPaneToggleButtonClick;

            // Do localization for paneToggleButton
            if (string.IsNullOrEmpty(AutomationProperties.GetName(m_paneToggleButton)))
            {
                AutomationProperties.SetName(m_paneToggleButton, "Toggle Navigation");
            }

            // Setup the tooltip for the paneToggleButton
            var tooltip = new ToolTip();
            tooltip.Content = AutomationProperties.GetName(m_paneToggleButton);
            ToolTipService.SetToolTip(m_paneToggleButton, tooltip);
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new TitleBarAutomationPeer(this);

    /// <summary>
    /// Gets or sets the Icon for the titlebar
    /// </summary>
    public IconSource IconSource
    {
        get { return (IconSource)GetValue(IconSourceProperty); }
        set { SetValue(IconSourceProperty, value); }
    }

    /// <summary>Identifies the <see cref="IconSource"/> dependency property.</summary>
    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.Register("IconSource", typeof(IconSource), typeof(TitleBar), new PropertyMetadata(null, (s, e) => ((TitleBar)s).UpdateIcon()));

    /// <summary>
    /// Gets or sets the Header content for the titlebar
    /// </summary>
    public object? Header
    {
        get { return (object)GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register("Header", typeof(object), typeof(TitleBar), new PropertyMetadata(null, (s, e) => ((TitleBar)s).UpdateHeader()));

    /// <summary>
    /// Gets or sets the Window title
    /// </summary>
    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(TitleBar), new PropertyMetadata(string.Empty, (s, e) => ((TitleBar)s).UpdateTitle()));

    /// <summary>
    /// Gets or sets the Subtitle for the titlebar
    /// </summary>
    public string Subtitle
    {
        get { return (string)GetValue(SubtitleProperty); }
        set { SetValue(SubtitleProperty, value); }
    }

    /// <summary>Identifies the <see cref="Subtitle"/> dependency property.</summary>
    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register("Subtitle", typeof(string), typeof(TitleBar), new PropertyMetadata(string.Empty, (s, e) => ((TitleBar)s).UpdateSubtitle()));

    /// <summary>
    /// Gets or sets the content for the titlebar
    /// </summary>
    public object? Content
    {
        get { return (object?)GetValue(ContentProperty); }
        set { SetValue(ContentProperty, value); }
    }

    /// <summary>Identifies the <see cref="Content"/> dependency property.</summary>
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register("Content", typeof(object), typeof(TitleBar), new PropertyMetadata(null, (s, e) => ((TitleBar)s).UpdateContent()));

    /// <summary>
    /// Gets or sets the footer of the titlebar
    /// </summary>
    public object? Footer
    {
        get { return (object?)GetValue(FooterProperty); }
        set { SetValue(FooterProperty, value); }
    }

    /// <summary>Identifies the <see cref="Footer"/> dependency property.</summary>
    public static readonly DependencyProperty FooterProperty =
        DependencyProperty.Register("Footer", typeof(object), typeof(TitleBar), new PropertyMetadata(null, (s, e) => ((TitleBar)s).UpdateFooter()));

    /// <summary>
    /// Gets or sets a value indicating whether the back button is visible
    /// </summary>
    public bool IsBackButtonVisible
    {
        get { return (bool)GetValue(IsBackButtonVisibleProperty); }
        set { SetValue(IsBackButtonVisibleProperty, value); }
    }

    /// <summary>Identifies the <see cref="IsBackButtonVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsBackButtonVisibleProperty =
        DependencyProperty.Register("IsBackButtonVisible", typeof(bool), typeof(TitleBar), new PropertyMetadata(false, (s,e) => ((TitleBar)s).UpdateBackButton()));

    /// <summary>
    /// Gets or sets a value indicating whether the back button is enabled
    /// </summary>
    public bool IsBackEnabled
    {
        get { return (bool)GetValue(IsBackEnabledProperty); }
        set { SetValue(IsBackEnabledProperty, value); }
    }

    /// <summary>Identifies the <see cref="IsBackEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsBackEnabledProperty =
        DependencyProperty.Register("IsBackEnabled", typeof(bool), typeof(TitleBar), new PropertyMetadata(true, (s, e) => ((TitleBar)s).UpdateInteractableElementsList()));

    /// <summary>
    /// Gets or sets a value indicating whether the pane toggle button is visible
    /// </summary>
    public bool IsPaneToggleButtonVisible
    {
        get { return (bool)GetValue(IsPaneToggleButtonVisibleProperty); }
        set { SetValue(IsPaneToggleButtonVisibleProperty, value); }
    }

    /// <summary>Identifies the <see cref="IsPaneToggleButtonVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsPaneToggleButtonVisibleProperty =
        DependencyProperty.Register("IsPaneToggleButtonVisible", typeof(bool), typeof(TitleBar), new PropertyMetadata(false, (s, e) => ((TitleBar)s).UpdatePaneToggleButton()));

    /// <summary>
    /// Gets the template settings for the titlebar
    /// </summary>
    public TitleBarTemplateSettings TemplateSettings
    {
        get { return (TitleBarTemplateSettings)GetValue(TemplateSettingsProperty); }
    }

    /// <summary>Identifies the <see cref="TemplateSettings"/> dependency property.</summary>
    public static readonly DependencyProperty TemplateSettingsProperty =
        DependencyProperty.Register("TemplateSettings", typeof(TitleBarTemplateSettings), typeof(TitleBar), new PropertyMetadata(null));

    /// <summary>
    /// Raised when the back button is clicked
    /// </summary>
    public event Windows.Foundation.TypedEventHandler<TitleBar, object>? BackRequested;

    /// <summary>
    /// Raised when the Pane toggle button is clicked
    /// </summary>
    public event Windows.Foundation.TypedEventHandler<TitleBar, object>? PaneToggleRequested;
}
