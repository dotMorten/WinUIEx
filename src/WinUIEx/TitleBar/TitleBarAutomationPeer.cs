using Microsoft.UI.Xaml.Automation.Peers;

namespace WinUIEx;

/// <summary>
/// Automation peer for the <see cref="TitleBar"/> control.
/// </summary>
[System.Obsolete("Use Windows App SDK's TitleBar control instead.")]
public partial class TitleBarAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TitleBar"/> class.
    /// </summary>
    /// <param name="owner">TitleBar owner</param>
    public TitleBarAutomationPeer(TitleBar owner) : base(owner)
    {
    }

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return base.GetAutomationControlTypeCore();
    }

    /// <inheritdoc />
    protected override string GetClassNameCore()
    {
        return nameof(TitleBar);
    }

    /// <inheritdoc />
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        if (string.IsNullOrEmpty(name))
        {
            if (Owner is TitleBar titleBar)
            {
                name = titleBar.Name;
            }
        }
        return name;
    }
}
