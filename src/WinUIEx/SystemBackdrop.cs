using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Win32.Graphics.Direct3D11;

namespace WinUIEx
{
    /// <summary>
    /// The backdrop configuration for applying a backdrop.
    /// </summary>
    /// <seealso cref="WindowEx.Backdrop"/>
    /// <seealso cref="MicaSystemBackdrop"/>
    /// <seealso cref="AcrylicSystemBackdrop"/>
    /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.MicaController" />
    /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController" />
    public abstract class SystemBackdrop
    {
        private protected bool _isDarkTintOverridden = false;
        private protected bool _isLightTintOverridden = false;
        private protected bool _isDarkFallbackOverridden = false;
        private protected bool _isLightFallbackOverridden = false;
        private readonly protected static Windows.UI.Color _defaultDarkFallback = Windows.UI.Color.FromArgb(0xff, 0x20, 0x20, 0x20);
        private readonly protected static Windows.UI.Color _defaultLightFallback = Windows.UI.Color.FromArgb(0xff, 0xf3, 0xf3, 0xf3);
        private protected Windows.UI.Color _darkTintColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);

        /// <summary>
        /// Resets any customized properties to their system defaults and reverts to automatic light/dark theme handling.
        /// </summary>
        public virtual void ResetProperties()
        {
            _isLightFallbackOverridden = false;
            _isDarkFallbackOverridden = false;
            _isDarkTintOverridden = false;
            
            _isLightTintOverridden = false;
        }

        /// <summary>
        /// Gets or sets the color tint for the backdrop material.
        /// </summary>
        /// <value>The color tint for the background material.</value>
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.MicaController.TintColor" />
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.TintColor" />
        public Windows.UI.Color DarkTintColor
        {
            get => _darkTintColor;
            set
            {
                if (_darkTintColor != value)
                {
                    _isDarkTintOverridden = true;
                    _darkTintColor = value;
                    NotifyDirty();
                }
            }
        }
        private protected Windows.UI.Color _lightTintColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);

        /// <summary>
        /// Gets or sets the color tint for the backdrop material.
        /// </summary>
        /// <value>The color tint for the background material.</value>
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.MicaController.TintColor" />
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.TintColor" />
        public Windows.UI.Color LightTintColor
        {
            get => _lightTintColor;
            set
            {
                if (_lightTintColor != value)
                {
                    _isLightTintOverridden = true;
                    _lightTintColor = value;
                    NotifyDirty();
                }
            }
        }

        private protected Windows.UI.Color _darkFallbackColor = _defaultDarkFallback;

        /// <summary>
        /// Gets or sets the solid color to use when system conditions prevent rendering the backdrop material.
        /// </summary>
        /// <remarks>
        /// The backdrop material is replaced with a solid color when one of the fallback conditions is met, such as entering battery saver mode.
        /// </remarks>
        /// <value>The solid color to use when system conditions prevent rendering the backdrop material.</value>
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.MicaController.FallbackColor" />
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.FallbackColor" />
        public Windows.UI.Color DarkFallbackColor
        {
            get => _darkFallbackColor;
            set
            {
                if (_darkFallbackColor != value)
                {
                    _isDarkFallbackOverridden = true;
                    _darkFallbackColor = value;
                    NotifyDirty();
                }
            }
        }

        private protected Windows.UI.Color _lightFallbackColor = _defaultLightFallback;

        /// <summary>
        /// Gets or sets the solid color to use when system conditions prevent rendering the backdrop material.
        /// </summary>
        /// <remarks>
        /// The backdrop material is replaced with a solid color when one of the fallback conditions is met, such as entering battery saver mode.
        /// </remarks>
        /// <value>The solid color to use when system conditions prevent rendering the backdrop material.</value>
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.MicaController.FallbackColor" />
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.FallbackColor" />
        public Windows.UI.Color LightFallbackColor
        {
            get => _lightFallbackColor;
            set
            {
                if (_lightFallbackColor != value)
                {
                    _isLightFallbackOverridden = true;
                    _lightFallbackColor = value;
                    NotifyDirty();
                }
            }
        }

        private double _darkTintOpacity = 0;

        /// <summary>
        /// Gets or sets the degree of opacity of the color tint.
        /// </summary>
        /// <value>The degree of opacity of the color tint.</value>
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.MicaController.TintOpacity" />
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.TintOpacity" />
        public double DarkTintOpacity
        {
            get => _darkTintOpacity;
            set
            {
                if (_darkTintOpacity != value)
                {
                    if (value < 0 || value > 1)
                        throw new ArgumentOutOfRangeException(nameof(value));
                    _darkTintOpacity = value;
                    NotifyDirty();
                }
            }
        }

        private double _lightTintOpacity = 0;

        /// <summary>
        /// Gets or sets the degree of opacity of the color tint.
        /// </summary>
        /// <value>The degree of opacity of the color tint.</value>
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.MicaController.TintOpacity" />
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.TintOpacity" />
        public double LightTintOpacity
        {
            get => _lightTintOpacity;
            set
            {
                if (_lightTintOpacity != value)
                {
                    if (value < 0 || value > 1)
                        throw new ArgumentOutOfRangeException(nameof(value));
                    _lightTintOpacity = value;
                    NotifyDirty();
                }
            }
        }

        private double _darkLuminosityOpacity = 1;

        /// <summary>
        /// Gets or sets the degree of opacity of the color's luminosity.
        /// </summary>
        /// <value>The degree of opacity of the color's luminosity.</value>
        /// <seealso cref="MicaController.LuminosityOpacity" />
        /// <seealso cref="DesktopAcrylicController.LuminosityOpacity" />
        public double DarkLuminosityOpacity
        {
            get => _darkLuminosityOpacity;
            set
            {
                if (_darkLuminosityOpacity != value)
                {
                    if (value < 0 || value > 1)
                        throw new ArgumentOutOfRangeException(nameof(value));
                    _darkLuminosityOpacity = value;
                    NotifyDirty();
                }
            }
        }

        private double _lightLuminosityOpacity = 1;

        /// <summary>
        /// Gets or sets the degree of opacity of the color's luminosity.
        /// </summary>
        /// <value>The degree of opacity of the color's luminosity.</value>
        /// <seealso cref="MicaController.LuminosityOpacity" />
        /// <seealso cref="DesktopAcrylicController.LuminosityOpacity" />
        public double LightLuminosityOpacity
        {
            get => _lightLuminosityOpacity;
            set
            {
                if (_lightLuminosityOpacity != value)
                {
                    if (value < 0 || value > 1)
                        throw new ArgumentOutOfRangeException(nameof(value));
                    _lightLuminosityOpacity = value;
                    NotifyDirty();
                }
            }
        }

        /// <summary>
        /// Notifies the backdrops that properties have changed and the controller needs updating.
        /// </summary>
        /// <seealso cref="UpdateController(ISystemBackdropController, SystemBackdropTheme)"/>
        protected void NotifyDirty()
        {
            IsDirty?.Invoke(this, EventArgs.Empty);
        }

        internal event EventHandler? IsDirty;

        /// <summary>
        /// Creates the backdrop controller.
        /// </summary>
        /// <returns></returns>
        protected internal abstract ISystemBackdropController CreateController();

        /// <summary>
        /// Updates the properties of the backdrop controller.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="theme"></param>
        protected internal abstract void UpdateController(ISystemBackdropController controller, SystemBackdropTheme theme);

        /// <summary>
        /// Applies the controller to the system backdrop.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="target"></param>
        /// <param name="configuration"></param>
        /// <remarks>
        /// This usually is a matter of calling <c>AddSystemBackdropTarget</c> and <c>SetSystemBackdropConfiguration</c> on the controller.
        /// Example:
        /// <code lang="cs">
        /// controller.AddSystemBackdropTarget(target);
        /// controller.SetSystemBackdropConfiguration(configuration);
        /// </code>
        /// </remarks>
        /// <seealso cref="MicaController.AddSystemBackdropTarget(ICompositionSupportsSystemBackdrop)"/>
        /// <seealso cref="DesktopAcrylicController.AddSystemBackdropTarget(ICompositionSupportsSystemBackdrop)"/>
        /// <seealso cref="MicaController.SetSystemBackdropConfiguration(SystemBackdropConfiguration)"/>
        /// <seealso cref="DesktopAcrylicController.SetSystemBackdropConfiguration(SystemBackdropConfiguration)"/>
        protected internal abstract void ApplyController(
            ISystemBackdropController controller, Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop target, SystemBackdropConfiguration configuration);

        /// <summary>
        /// Gets a proper indicating whether the system supports this backdrop.
        /// </summary>
        public abstract bool IsSupported { get; }
    }

    /// <summary>
    /// Defines the Mica System Backdrop settings to apply to the window.
    /// Note: requires Windows 11 and up - Windows 10 will use fallback colors.
    /// </summary>
    /// <seealso cref="AcrylicSystemBackdrop"/>
    public class MicaSystemBackdrop : SystemBackdrop
    {
        private readonly static Windows.UI.Color _defaultDarkTint = Windows.UI.Color.FromArgb(0xff, 0x20, 0x20, 0x20);
        private readonly static Windows.UI.Color _defaultLightTint = Windows.UI.Color.FromArgb(0xff, 0xf3, 0xf3, 0xf3);
        private readonly static Windows.UI.Color _altDarkTint = Windows.UI.Color.FromArgb(0xff, 0x20, 0x20, 0x20); //TODO: Verify
        private readonly static Windows.UI.Color _altLightTint = Windows.UI.Color.FromArgb(0xff, 0xda, 0xda, 0xda);
        private readonly static Windows.UI.Color _altDarkFallback = Windows.UI.Color.FromArgb(0xff, 0x20, 0x20, 0x20); //TODO: Verify
        private readonly static Windows.UI.Color _altLightFallback = Windows.UI.Color.FromArgb(0xff, 0xe8, 0xe8, 0xe8);

        /// <summary>
        /// Initializes a new instance of the <see cref="MicaSystemBackdrop"/> class.
        /// </summary>
        public MicaSystemBackdrop()
        {
            LightTintColor = _defaultLightTint;
            LightTintOpacity = 0.5;
            DarkTintColor = _defaultDarkTint;
            DarkTintOpacity = 0.5;
            _isDarkTintOverridden = false;
            _isLightTintOverridden = false;
        }
        private MicaKind _kind;

        /// <summary>
        /// Gets or sets the Mica backdrop kind.
        /// </summary>
        /// <remarks>
        /// Setting the Kind property will change the Tint and Fallback colors to new defaults. Note that if you
        /// explicitly set the Tint and Fallback colors, changing the Kind will have no effect. This matches the
        /// MicaController behavior in the Windows App SDK.
        /// </remarks>
        /// <seealso cref="MicaController.Kind"/>
        public MicaKind Kind
        {
            get => _kind;
            set {
                _kind = value;
                 if(!_isDarkTintOverridden)
                     _darkTintColor = _kind == MicaKind.Base ? _defaultDarkTint : _altDarkTint;
                if (!_isLightTintOverridden)
                    _lightTintColor = _kind == MicaKind.Base ? _defaultLightTint : _altLightTint;
                if (!_isDarkFallbackOverridden)
                    _darkFallbackColor = _kind == MicaKind.Base ? _defaultDarkFallback : _altDarkFallback;
                if (!_isLightFallbackOverridden)
                    _lightFallbackColor = _kind == MicaKind.Base ? _defaultLightFallback : _altLightFallback;
                NotifyDirty(); }
        }

        /// <inheritdoc/>
        public override bool IsSupported => MicaController.IsSupported();

        /// <inheritdoc/>
        protected internal override ISystemBackdropController CreateController() => new MicaController();
        
        /// <inheritdoc/>
        protected internal override void UpdateController(ISystemBackdropController controller, SystemBackdropTheme theme)
        {
            if (controller is MicaController mica)
            {
                bool isDark = theme == SystemBackdropTheme.Dark;
                mica.Kind = Kind;
                mica.TintOpacity = (float)(isDark ? DarkTintOpacity : LightTintOpacity);
                mica.LuminosityOpacity = (float)(isDark ? DarkLuminosityOpacity : LightLuminosityOpacity);
                mica.TintColor = isDark ? DarkTintColor : LightTintColor;
                mica.FallbackColor = isDark ? DarkFallbackColor : LightFallbackColor;
            }
        }

        /// <inheritdoc/>
        protected internal override void ApplyController(ISystemBackdropController controller, ICompositionSupportsSystemBackdrop target, SystemBackdropConfiguration configuration)
        {
            if (controller is MicaController mica)
            {
                mica.AddSystemBackdropTarget(target);
                mica.SetSystemBackdropConfiguration(configuration);
            }
        }
    }

    /// <summary>
    /// Defines the Acrylic System Backdrop settings to apply to the window.
    /// </summary>
    /// <seealso cref="DesktopAcrylicController"/>
    /// <seealso cref="MicaSystemBackdrop"/>
    public class AcrylicSystemBackdrop : SystemBackdrop
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AcrylicSystemBackdrop"/> class.
        /// </summary>
        public AcrylicSystemBackdrop()
        {
            LightTintColor = Windows.UI.Color.FromArgb(0xff, 0xd3, 0xd3, 0xd3);
            LightFallbackColor = Windows.UI.Color.FromArgb(0xff, 0xd3, 0xd3, 0xd3);
            LightLuminosityOpacity = .64;
            DarkTintColor = Windows.UI.Color.FromArgb(0xff, 0x40, 0x40, 0x40);
            DarkFallbackColor = Windows.UI.Color.FromArgb(0xff, 0x40, 0x40, 0x40);
            DarkLuminosityOpacity = .64;
        }

        /// <inheritdoc/>
        public override bool IsSupported => DesktopAcrylicController.IsSupported();

        /// <inheritdoc/>
        protected internal override ISystemBackdropController CreateController() => new DesktopAcrylicController();

        /// <inheritdoc/>
        protected internal override void UpdateController(ISystemBackdropController controller, SystemBackdropTheme theme)
        {
            if (controller is DesktopAcrylicController acrylic)
            {
                bool isDark = theme == SystemBackdropTheme.Dark;
                acrylic.TintOpacity = (float)(isDark ? DarkTintOpacity : LightTintOpacity);
                acrylic.LuminosityOpacity = (float)(isDark ? DarkLuminosityOpacity : LightLuminosityOpacity);
                acrylic.TintColor = isDark ? DarkTintColor : LightTintColor;
                acrylic.FallbackColor = isDark ? DarkFallbackColor : LightFallbackColor;
            }
        }

        /// <inheritdoc/>
        protected internal override void ApplyController(ISystemBackdropController controller, ICompositionSupportsSystemBackdrop target, SystemBackdropConfiguration configuration)
        {
            if (controller is DesktopAcrylicController acrylic)
            {
                acrylic.AddSystemBackdropTarget(target);
                acrylic.SetSystemBackdropConfiguration(configuration);
            }
        }
    }
}
