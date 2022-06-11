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
        private Windows.UI.Color _tintColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);

        /// <summary>
        /// Gets or sets the color tint for the backdrop material.
        /// </summary>
        /// <value>The color tint for the background material.</value>
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.MicaController.TintColor" />
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.TintColor" />
        public Windows.UI.Color TintColor
        {
            get => _tintColor;
            set
            {
                if (_tintColor != value)
                {
                    _tintColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private Windows.UI.Color _fallbackColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);

        /// <summary>
        /// Gets or sets the solid color to use when system conditions prevent rendering the backdrop material.
        /// </summary>
        /// <remarks>
        /// The backdrop material is replaced with a solid color when one of the fallback conditions is met, such as entering battery saver mode.
        /// </remarks>
        /// <value>The solid color to use when system conditions prevent rendering the backdrop material.</value>
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.MicaController.FallbackColor" />
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.FallbackColor" />
        public Windows.UI.Color FallbackColor
        {
            get => _fallbackColor;
            set
            {
                if (_fallbackColor != value)
                {
                    _fallbackColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _tintOpacity = double.NaN;

        /// <summary>
        /// Gets or sets the degree of opacity of the color tint.
        /// </summary>
        /// <value>The degree of opacity of the color tint.</value>
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.MicaController.TintOpacity" />
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.TintOpacity" />
        public double TintOpacity
        {
            get => _tintOpacity;
            set
            {
                if (_tintOpacity != value)
                {
                    if (value < 0 || value > 1)
                        throw new ArgumentOutOfRangeException(nameof(value));
                    _tintOpacity = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _luminosityOpacity = double.NaN;

        /// <summary>
        /// Gets or sets the degree of opacity of the color's luminosity.
        /// </summary>
        /// <value>The degree of opacity of the color's luminosity.</value>
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.MicaController.LuminosityOpacity" />
        /// <seealso cref="Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.LuminosityOpacity" />
        public double LuminosityOpacity
        {
            get => _luminosityOpacity;
            set
            {
                if (_luminosityOpacity != value)
                {
                    if (value < 0 || value > 1)
                        throw new ArgumentOutOfRangeException(nameof(value));
                    _luminosityOpacity = value;
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
