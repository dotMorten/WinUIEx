using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUIEx;

/// <summary>
/// Class used to forward the IconElement property to the template.
/// </summary>
public class TitleBarTemplateSettings : DependencyObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TitleBar"/> class.
    /// </summary>
    public TitleBarTemplateSettings()
    {
    }

    /// <summary>
    /// Gets or sets the IconElement property
    /// </summary>
    public IconElement? IconElement
    {
        get { return (IconElement?)GetValue(IconElementProperty); }
        set { SetValue(IconElementProperty, value); }
    }

    /// <summary>Identifies the <see cref="IconElement"/> dependency property.</summary>
    public static readonly DependencyProperty IconElementProperty =
    DependencyProperty.Register("IconElement", typeof(IconElement), typeof(TitleBarTemplateSettings), new PropertyMetadata(null));
}
