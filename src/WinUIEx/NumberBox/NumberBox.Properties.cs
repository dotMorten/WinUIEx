using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Globalization.NumberFormatting;

namespace WinUIEx
{
    public sealed partial class NumberBox : Control
    {
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set
            {
                // When using two way bindings to Value using x:Bind, we could end up with a stack overflow because
                // nan != nan. However in this case, we are using nan as a value to represent value not set (cleared)
                // and that can happen quite often. We can avoid the stack overflow by breaking the cycle here. This is possible
                // for x:Bind since the generated code goes through this property setter. This is not the case for Binding
                // unfortunately. x:Bind is recommended over Binding anyway due to its perf and debuggability benefits.
                if (!double.IsNaN(value) || !double.IsNaN(Value))
                {
                    SetValue(ValueProperty, value);
                }
            }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumberBox), new PropertyMetadata(double.NaN, (s, e) => ((NumberBox)s).OnValuePropertyChanged(e)));

        private void OnValuePropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            // This handler may change Value; don't send extra events in that case.
            if (!m_valueUpdating)
            {
                double oldValue = (double)args.OldValue;

                m_valueUpdating = true;
                try
                {

                    CoerceValue();

                    double newValue = (double)Value;
                    if (newValue != oldValue && !(double.IsNaN(newValue) && double.IsNaN(oldValue)))
                    {
                        // Fire ValueChanged event
                        ValueChanged?.Invoke(this, new NumberBoxValueChangedEventArgs(oldValue, newValue));

                        // Fire value property change for UIA
                        if (FrameworkElementAutomationPeer.FromElement(this) is NumberBoxAutomationPeer peer)
                        {
                            peer.RaiseValueChangedEvent(oldValue, newValue);
                        }
                    }

                    UpdateTextToValue();
                    UpdateSpinButtonEnabled();
                }
                finally
                {
                    m_valueUpdating = false;
                }
            }
        }

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NumberBox), new PropertyMetadata(double.MinValue, (s, e) => ((NumberBox)s).OnMinimumPropertyChanged(e)));

        private void OnMinimumPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            CoerceMaximum();
            CoerceValue();

            UpdateSpinButtonEnabled();
            ReevaluateForwardedUIAProperties();
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NumberBox), new PropertyMetadata(double.MaxValue, (s, e) => ((NumberBox)s).OnMaximumPropertyChanged(e)));

        private void OnMaximumPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            CoerceMinimum();
            CoerceValue();

            UpdateSpinButtonEnabled();
            ReevaluateForwardedUIAProperties();
        }

        public double SmallChange
        {
            get { return (double)GetValue(SmallChangeProperty); }
            set { SetValue(SmallChangeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SmallChange.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(NumberBox), new PropertyMetadata(1d, (s, e) => ((NumberBox)s).OnSmallChangePropertyChanged(e)));

        private void OnSmallChangePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateSpinButtonEnabled();
        }

        public double LargeChange
        {
            get { return (double)GetValue(LargeChangeProperty); }
            set { SetValue(LargeChangeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LargeChange.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LargeChangeProperty =
            DependencyProperty.Register(nameof(LargeChange), typeof(double), typeof(NumberBox), new PropertyMetadata(10d));



        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(NumberBox), new PropertyMetadata(string.Empty, (s, e) => ((NumberBox)s).OnTextPropertyChanged(e)));

        private void OnTextPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!m_textUpdating)
            {
                UpdateValueToText();
            }
        }

        public object? Header
        {
            get { return (object)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Header.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(object), typeof(NumberBox), new PropertyMetadata(null, (s, e) => ((NumberBox)s).OnHeaderPropertyChanged(e)));

        private void OnHeaderPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateHeaderPresenterState();
        }

        public DataTemplate? HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(NumberBox), new PropertyMetadata(null, (s, e) => ((NumberBox)s).OnHeaderTemplatePropertyChanged(e)));

        private void OnHeaderTemplatePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateHeaderPresenterState();
        }



        public InputScope InputScope
        {
            get { return (InputScope)GetValue(InputScopeProperty); }
            set { SetValue(InputScopeProperty, value); }
        }

        /// <summary>Identifies the <see cref="InputScope"/> dependency property.</summary>
        public static readonly DependencyProperty InputScopeProperty =
            DependencyProperty.Register(nameof(InputScope), typeof(InputScope), typeof(NumberBox), new PropertyMetadata(null));



        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PlaceholderText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(NumberBox), new PropertyMetadata(string.Empty));

        public FlyoutBase? SelectionFlyout   
        {
            get { return (FlyoutBase)GetValue(SelectionFlyoutProperty); }
            set { SetValue(SelectionFlyoutProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectionFlyoutProperty =
            DependencyProperty.Register(nameof(SelectionFlyout), typeof(int), typeof(NumberBox), new PropertyMetadata(null));

        public SolidColorBrush? SelectionHighlightColor
        {
            get { return (SolidColorBrush)GetValue(SelectionHighlightColorProperty); }
            set { SetValue(SelectionHighlightColorProperty, value); }
        }

        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        /// <summary>Identifies the <see cref="TextAlignment"/> dependency property.</summary>
        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(NumberBox), new PropertyMetadata(TextAlignment.Left));

        public static readonly DependencyProperty SelectionHighlightColorProperty =
            DependencyProperty.Register(nameof(SelectionHighlightColor), typeof(SolidColorBrush), typeof(NumberBox), new PropertyMetadata(null));

        public TextReadingOrder TextReadingOrder
        {
            get { return (TextReadingOrder)GetValue(TextReadingOrderProperty); }
            set { SetValue(TextReadingOrderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextReadingOrder.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextReadingOrderProperty =
            DependencyProperty.Register(nameof(TextReadingOrder), typeof(TextReadingOrder), typeof(NumberBox), new PropertyMetadata(TextReadingOrder.Default));

        public bool PreventKeyboardDisplayOnProgrammaticFocus
        {
            get { return (bool)GetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty); }
            set { SetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty, value); }
        }

        /// <summary>Identifies the <see cref="PreventKeyboardDisplayOnProgrammaticFocus"/> dependency property.</summary>
        public static readonly DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty =
            DependencyProperty.Register(nameof(PreventKeyboardDisplayOnProgrammaticFocus), typeof(bool), typeof(NumberBox), new PropertyMetadata(false));


        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(NumberBox), new PropertyMetadata(string.Empty));


        public NumberBoxValidationMode ValidationMode
        {
            get { return (NumberBoxValidationMode)GetValue(ValidationModeProperty); }
            set { SetValue(ValidationModeProperty, value); }
        }

        /// <summary>Identifies the <see cref="ValidationMode"/> dependency property.</summary>
        public static readonly DependencyProperty ValidationModeProperty =
            DependencyProperty.Register(nameof(ValidationMode), typeof(NumberBoxValidationMode), typeof(NumberBox), new PropertyMetadata(NumberBoxValidationMode.InvalidInputOverwritten, (s, e) => ((NumberBox)s).OnValidationModePropertyChanged(e)));

        private void OnValidationModePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            ValidateInput();
            UpdateSpinButtonEnabled();
        }

        public NumberBoxSpinButtonPlacementMode SpinButtonPlacementMode
        {
            get { return (NumberBoxSpinButtonPlacementMode)GetValue(SpinButtonPlacementModeProperty); }
            set { SetValue(SpinButtonPlacementModeProperty, value); }
        }

        /// <summary>Identifies the <see cref="SpinButtonPlacementMode"/> dependency property.</summary>
        public static readonly DependencyProperty SpinButtonPlacementModeProperty =
            DependencyProperty.Register(nameof(SpinButtonPlacementMode), typeof(NumberBoxSpinButtonPlacementMode), typeof(NumberBox), new PropertyMetadata(NumberBoxSpinButtonPlacementMode.Hidden, (s, e) => ((NumberBox)s).OnSpinButtonPlacementModePropertyChanged(e)));

        private void OnSpinButtonPlacementModePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateSpinButtonPlacement();
        }

        public bool IsWrapEnabled
        {
            get { return (bool)GetValue(IsWrapEnabledProperty); }
            set { SetValue(IsWrapEnabledProperty, value); }
        }

        /// <summary>Identifies the <see cref="IsWrapEnabled"/> dependency property.</summary>
        public static readonly DependencyProperty IsWrapEnabledProperty =
            DependencyProperty.Register(nameof(IsWrapEnabled), typeof(bool), typeof(NumberBox), new PropertyMetadata(false, (s, e) => ((NumberBox)s).OnIsWrapEnabledPropertyChanged(e)));

        private void OnIsWrapEnabledPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateSpinButtonEnabled();
        }

        public bool AcceptsExpression
        {
            get { return (bool)GetValue(AcceptsExpressionProperty); }
            set { SetValue(AcceptsExpressionProperty, value); }
        }

        /// <summary>Identifies the <see cref="AcceptsExpression"/> dependency property.</summary>
        public static readonly DependencyProperty AcceptsExpressionProperty =
            DependencyProperty.Register(nameof(AcceptsExpression), typeof(bool), typeof(NumberBox), new PropertyMetadata(false));

        public INumberFormatter2? NumberFormatter
        {
            get { return (INumberFormatter2)GetValue(NumberFormatterProperty); }
            set { ValidateNumberFormatter(value); SetValue(NumberFormatterProperty, value); }
        }

        /// <summary>Identifies the <see cref="NumberFormatter"/> dependency property.</summary>
        public static readonly DependencyProperty NumberFormatterProperty =
            DependencyProperty.Register(nameof(NumberFormatter), typeof(INumberFormatter2), typeof(NumberBox), new PropertyMetadata(null, (s,e) => ((NumberBox)s).OnNumberFormatterChanged()));

        private void OnNumberFormatterChanged()
        {
            // Update text with new formatting
            UpdateTextToValue();
        }

        private void ValidateNumberFormatter(INumberFormatter2? value)
        {
            // NumberFormatter also needs to be an INumberParser
            if (value is not null && value is not INumberParser)
            {
                throw new ArgumentException("Formatter must implement INumberParser");
            }
        }

        public event Windows.Foundation.TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>? ValueChanged;

    }
}
