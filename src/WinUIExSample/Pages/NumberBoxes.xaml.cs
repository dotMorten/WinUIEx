using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization.NumberFormatting;
using Windows.Web.Http;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUIExSample.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NumberBoxes : Page
    {
        public NumberBoxes()
        {
            this.InitializeComponent();
        }
        public WindowEx MainWindow => ((App)Application.Current).MainWindow;

        public NumberBoxVM VM { get; } = new NumberBoxVM();

        public static string GetTypeName(INumberFormatter2 type)
        {
            return type.GetType().Name;
        }
    }
    public partial class StringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var type = value.GetType();
            if (type.IsEnum)
                return value.ToString();
            return type.Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public partial class NumberBoxVM : ObservableObject
    {
        public NumberBoxVM()
        {
            Formatter = Formatters[0];
        }

        [ObservableProperty]
        public partial double Value { get; set; } = double.NaN;
        [ObservableProperty]
        public partial decimal DecimalValue { get; set; }
        [ObservableProperty]
        public partial int? NullableIntValue { get; set; }


        [ObservableProperty]
        public partial bool AcceptsExpressions { get; set; }
        
        [ObservableProperty]
        public partial bool IsWrapEnabled { get; set; }

        [ObservableProperty]
        public partial double Minimum { get; set; } = double.MinValue;

        partial void OnMinimumChanged(double value)
        {
            if (double.IsNaN(value))
                Minimum = double.MinValue;
            OnPropertyChanged(nameof(MinimumDecimal));
            OnPropertyChanged(nameof(MinimumInt32));
        }

        public decimal MinimumDecimal => Minimum == double.MinValue ? decimal.MinValue : (decimal)Minimum;

        public int MinimumInt32 => Minimum == double.MinValue ? Int32.MinValue : (Int32)Minimum;

        [ObservableProperty]
        public partial double Maximum { get; set; } = double.MaxValue;

        partial void OnMaximumChanged(double value)
        {
            if (double.IsNaN(value))
                Maximum = double.MaxValue;
            OnPropertyChanged(nameof(MaximumDecimal));
            OnPropertyChanged(nameof(MaximumInt32));
        }

        public decimal MaximumDecimal => Maximum == double.MaxValue ? decimal.MaxValue : (decimal)Maximum;

        public int MaximumInt32 => Maximum == double.MaxValue ? Int32.MaxValue : (Int32)Maximum;

        [ObservableProperty]
        public partial INumberFormatter2 Formatter { get; set; }

        public INumberFormatter2[] Formatters { get; set; } =
        {
            new DecimalFormatter() { FractionDigits = 0, IntegerDigits = 1 },
            new PercentFormatter(),
            new CurrencyFormatter(new RegionInfo(CultureInfo.CurrentCulture.LCID).ISOCurrencySymbol) { IntegerDigits = 1, FractionDigits = 2, SignificantDigits = 2, IsGrouped = true },
            new PermilleFormatter(),
        };

        public NumberBoxSpinButtonPlacementMode[] SpinButtonPlacementModes =
        {
            NumberBoxSpinButtonPlacementMode.Hidden,
            NumberBoxSpinButtonPlacementMode.Inline,
            NumberBoxSpinButtonPlacementMode.Compact
        };

        [ObservableProperty]
        public partial NumberBoxSpinButtonPlacementMode SpinButtonPlacementMode { get; set; } = NumberBoxSpinButtonPlacementMode.Inline;

        public NumberBoxValidationMode[] ValidationModes =
        {
            NumberBoxValidationMode.Disabled,
            NumberBoxValidationMode.InvalidInputOverwritten,
        };

        [ObservableProperty]
        public partial NumberBoxValidationMode ValidationMode { get; set; } = NumberBoxValidationMode.InvalidInputOverwritten;

        public List<TextAlignment> TextAlignments { get; } =
        [
            TextAlignment.Center,
            TextAlignment.Left,
            TextAlignment.Right,
            TextAlignment.Justify,
            TextAlignment.DetectFromContent,
        ];

        [ObservableProperty]
        public partial TextAlignment TextAlignment { get; set; } = TextAlignment.Left;
    }
}
