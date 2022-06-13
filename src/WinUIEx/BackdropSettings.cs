using Microsoft.UI.Xaml;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinUIEx
{
    /// <summary>
    /// The backdrop configuration for applying a backdrop
    /// </summary>
    /// <seealso cref="WindowEx.BackdropSettings"/>
    /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.MicaController" />
    /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController" />
    public class BackdropSettings : INotifyPropertyChanged
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
                }
            }
        }

        private double _darkLuminosityOpacity = double.NaN;

        /// <summary>
        /// Gets or sets the degree of opacity of the color's luminosity.
        /// </summary>
        /// <value>The degree of opacity of the color's luminosity.</value>
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.MicaController.LuminosityOpacity" />
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.LuminosityOpacity" />
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
                    OnPropertyChanged();
                }
            }
        }

        private double _lightLuminosityOpacity = double.NaN;

        /// <summary>
        /// Gets or sets the degree of opacity of the color's luminosity.
        /// </summary>
        /// <value>The degree of opacity of the color's luminosity.</value>
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.MicaController.LuminosityOpacity" />
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.LuminosityOpacity" />
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
                    OnPropertyChanged();
                }
            }
        }

        private Backdrop _kind;

        /// <summary>
        /// Gets or sets the kind of backdrop applied.
        /// </summary>
        public Backdrop Kind
        {
            get { return _kind; }
            set
            {
                _kind = value;
                OnPropertyChanged();
            }
        }

        event PropertyChangedEventHandler? _handler;
        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add => _handler += value;
            remove => _handler -= value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            _handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
