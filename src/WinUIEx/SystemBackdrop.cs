﻿using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinUIEx
{
    /// <summary>
    /// The backdrop configuration for applying a backdrop.
    /// </summary>
    /// <seealso cref="WindowEx.Backdrop"/>
    /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.MicaController" />
    /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController" />
    public abstract class SystemBackdrop
    { 
        private Windows.UI.Color _darkTintColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);

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
                    _darkTintColor = value;
                    NotifyDirty();
                }
            }
        }
        private Windows.UI.Color _lightTintColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);

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
                    _lightTintColor = value;
                    NotifyDirty();
                }
            }
        }

        private Windows.UI.Color _darkFallbackColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);

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
                    _darkFallbackColor = value;
                    NotifyDirty();
                }
            }
        }

        private Windows.UI.Color _lightFallbackColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);

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
                    _lightFallbackColor = value;
                    NotifyDirty();
                }
            }
        }

        private double _darkTintOpacity = double.NaN;

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

        private double _lightTintOpacity = double.NaN;

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

        private double _darkLuminosityOpacity = double.NaN;

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

        private double _lightLuminosityOpacity = double.NaN;

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

        private protected static bool IsEmpty(Windows.UI.Color c) => c.A == 0 && c.R == 0 && c.G == 0 && c.B == 0;

        /// <summary>
        /// Gets a proper indicating whether the system supports this backdrop.
        /// </summary>
        public abstract bool IsSupported { get; }
    }

    /// <summary>
    /// Defines the Mica System Backdrop settings to apply to the window.
    /// </summary>
    /// <seealso cref="MicaController"/>
    public class MicaSystemBackdrop : SystemBackdrop
    {
        private MicaKind _kind;

        /// <summary>
        /// Gets or sets the Mica backdrop kind.
        /// </summary>
        /// <seealso cref="MicaController.Kind"/>
        public MicaKind Kind
        {
            get => _kind;
            set { _kind = value; NotifyDirty(); }
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
                if (!double.IsNaN(isDark ? DarkTintOpacity : LightTintOpacity))
                    mica.TintOpacity = (float)(isDark ? DarkTintOpacity : LightTintOpacity);
                if (!double.IsNaN(isDark ? DarkLuminosityOpacity : LightLuminosityOpacity))
                    mica.LuminosityOpacity = (float)(isDark ? DarkLuminosityOpacity : LightLuminosityOpacity);
                if (!IsEmpty(isDark ? DarkTintColor : LightTintColor))
                    mica.TintColor = isDark ? DarkTintColor : LightTintColor;
                if (!IsEmpty(isDark ? DarkFallbackColor : LightFallbackColor))
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
    public class AcrylicSystemBackdrop : SystemBackdrop
    {
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
                if (!double.IsNaN(isDark ? DarkTintOpacity : LightTintOpacity))
                    acrylic.TintOpacity = (float)(isDark ? DarkTintOpacity : LightTintOpacity);
                if (!double.IsNaN(isDark ? DarkLuminosityOpacity : LightLuminosityOpacity))
                    acrylic.LuminosityOpacity = (float)(isDark ? DarkLuminosityOpacity : LightLuminosityOpacity);
                if (!IsEmpty(isDark ? DarkTintColor : LightTintColor))
                    acrylic.TintColor = isDark ? DarkTintColor : LightTintColor;
                if (!IsEmpty(isDark ? DarkFallbackColor : LightFallbackColor))
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
