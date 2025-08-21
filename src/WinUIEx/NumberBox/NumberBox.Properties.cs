using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Globalization.NumberFormatting;

namespace WinUIEx
{
    public abstract partial class NumberBox<T>
    {
        /// <summary>
        /// Gets or sets the numeric value of a <see cref="NumberBox"/>.
        /// </summary>
        /// <value>The numeric value of a NumberBox.</value>
        public T Value
        {
            get { return (T)GetValue(ValueProperty); }
            set
            {
                // When using two way bindings to Value using x:Bind, we could end up with a stack overflow because
                // nan != nan. However in this case, we are using nan as a value to represent value not set (cleared)
                // and that can happen quite often. We can avoid the stack overflow by breaking the cycle here. This is possible
                // for x:Bind since the generated code goes through this property setter. This is not the case for Binding
                // unfortunately. x:Bind is recommended over Binding anyway due to its perf and debuggability benefits.
                if (!T.IsNaN(value) || !T.IsNaN(Value))
                {
                    SetValue(ValueProperty, value);
                }
            }
        }

        /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumberBox<T>), new PropertyMetadata(T.Zero, (s, e) => ((NumberBox<T>)s).OnValuePropertyChanged(e)));

        private void OnValuePropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            // This handler may change Value; don't send extra events in that case.
            if (!m_valueUpdating)
            {
                T oldValue = (T)args.OldValue;

                m_valueUpdating = true;
                try
                {

                    CoerceValue();

                    T newValue = (T)Value;
                    if (newValue != oldValue && !(T.IsNaN(newValue) && T.IsNaN(oldValue)))
                    {
                        // Fire ValueChanged event
                        ValueChanged?.Invoke(this, new NumberBoxValueChangedEventArgs<T>(oldValue, newValue));

                        // Fire value property change for UIA
                        if (FrameworkElementAutomationPeer.FromElement(this) is NumberBoxAutomationPeer<T> peer)
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

        /// <summary>
        /// Gets or sets the numerical minimum for <see cref="Value"/>.
        /// </summary>
        /// <value>The numerical minimum for <see cref="Value"/>.</value>
        public T Minimum
        {
            get { return (T)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(T), typeof(NumberBox<T>), new PropertyMetadata(T.MinValue, (s, e) => ((NumberBox<T>)s).OnMinimumPropertyChanged(e)));

        private void OnMinimumPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            CoerceMaximum();
            CoerceValue();

            UpdateSpinButtonEnabled();
            ReevaluateForwardedUIAProperties();
        }

        /// <summary>
        /// Gets or sets the numerical maximum for <see cref="Value"/>.
        /// </summary>
        /// <value>The numerical maximum for <see cref="Value"/>.</value>
        public T Maximum
        {
            get { return (T)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(T), typeof(NumberBox<T>), new PropertyMetadata(T.MaxValue, (s, e) => ((NumberBox<T>)s).OnMaximumPropertyChanged(e)));

        private void OnMaximumPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            CoerceMinimum();
            CoerceValue();

            UpdateSpinButtonEnabled();
            ReevaluateForwardedUIAProperties();
        }

        /// <summary>
        /// Gets or sets a number that is added to or subtracted from <see cref="Value"/> when a small change is made,
        /// such as with an arrow key or scrolling.
        /// </summary>
        /// <value>A number that is added to or subtracted from <see cref="Value"/> when a small change is made,
        /// such as with an arrow key or scrolling. The default is 1.</value>
        public T SmallChange
        {
            get { return (T)GetValue(SmallChangeProperty); }
            set { SetValue(SmallChangeProperty, value); }
        }

        /// <summary>Identifies the <see cref="SmallChange"/> dependency property.</summary>
        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(nameof(SmallChange), typeof(T), typeof(NumberBox<T>), new PropertyMetadata(T.One, (s, e) => ((NumberBox<T>)s).OnSmallChangePropertyChanged(e)));

        private void OnSmallChangePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateSpinButtonEnabled();
        }

        /// <summary>
        /// Gets or sets a number that is added to or subtracted from <see cref="Value"/> when a large change is made,
        /// such as with the PageUp and PageDown keys.
        /// </summary>
        /// <value>A number that is added to or subtracted from <see cref="Value"/> when a large change is made, such as
        /// with the PageUp and PageDown keys. The default is 10.</value>
        public T LargeChange
        {
            get { return (T)GetValue(LargeChangeProperty); }
            set { SetValue(LargeChangeProperty, value); }
        }

        /// <summary>Identifies the <see cref="LargeChange"/> dependency property.</summary>
        public static readonly DependencyProperty LargeChangeProperty =
            DependencyProperty.Register(nameof(LargeChange), typeof(T), typeof(NumberBox<T>), new PropertyMetadata(T.CreateSaturating<double>(10d)));

        /// <summary>
        /// Gets or sets the string type representation of the <see cref="Value"/> property.
        /// </summary>
        /// <value>The string type representation of the <see cref="Value"/> property.</value>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(NumberBox<T>), new PropertyMetadata(string.Empty, (s, e) => ((NumberBox<T>)s).OnTextPropertyChanged(e)));

        private void OnTextPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!m_textUpdating)
            {
                UpdateValueToText();
            }
        }

        /// <summary>
        /// Gets or sets the content for the control's header.
        /// </summary>
        /// <value>The content of the control's header. The default is <c>null</c>.</value>
        public object? Header
        {
            get { return (object)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(object), typeof(NumberBox<T>), new PropertyMetadata(null, (s, e) => ((NumberBox<T>)s).OnHeaderPropertyChanged(e)));

        private void OnHeaderPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateHeaderPresenterState();
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to display the content of the control's header.
        /// </summary>
        /// <value>The template that specifies the visualization of the header object. The default is <c>null</c>.</value>
        public DataTemplate? HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        /// <summary>Identifies the <see cref="HeaderTemplate"/> dependency property.</summary>
        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(NumberBox<T>), new PropertyMetadata(null, (s, e) => ((NumberBox<T>)s).OnHeaderTemplatePropertyChanged(e)));

        private void OnHeaderTemplatePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateHeaderPresenterState();
        }

        /// <summary>
        /// Gets or sets the InputScope for the NumberBox.
        /// </summary>
        public InputScope InputScope
        {
            get { return (InputScope)GetValue(InputScopeProperty); }
            set { SetValue(InputScopeProperty, value); }
        }

        /// <summary>Identifies the <see cref="InputScope"/> dependency property.</summary>
        public static readonly DependencyProperty InputScopeProperty =
            DependencyProperty.Register(nameof(InputScope), typeof(InputScope), typeof(NumberBox<T>), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the text that is displayed in the data entry field of the control until the
        /// value is changed by a user action or some other operation.
        /// </summary>
        /// <value>The text that is displayed in the data entry field of the control until the value is changed by a user action or some other operation.
        /// The default is an empty string.</value>
        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }

        /// <summary>Identifies the <see cref="PlaceholderText"/> dependency property.</summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(NumberBox<T>), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the flyout that is shown when text is selected, or <c>null</c> if no flyout is shown.
        /// </summary>
        /// <value>The flyout that is shown when text is selected, or null if no flyout is shown.
        /// The default is a <see cref="TextCommandBarFlyout"/></value>
        public FlyoutBase? SelectionFlyout   
        {
            get { return (FlyoutBase)GetValue(SelectionFlyoutProperty); }
            set { SetValue(SelectionFlyoutProperty, value); }
        }

        /// <summary>Identifies the <see cref="SelectionFlyout"/> dependency property.</summary>
        public static readonly DependencyProperty SelectionFlyoutProperty =
            DependencyProperty.Register(nameof(SelectionFlyout), typeof(int), typeof(NumberBox<T>), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the brush used to highlight the selected text.
        /// </summary>
        /// <value>The brush used to highlight the selected text. The practical default is a brush using the
        /// theme resource <c>TextSelectionHighlightThemeColor</c>.</value>
        public SolidColorBrush? SelectionHighlightColor
        {
            get { return (SolidColorBrush)GetValue(SelectionHighlightColorProperty); }
            set { SetValue(SelectionHighlightColorProperty, value); }
        }

        /// <summary>Identifies the <see cref="SelectionHighlightColor"/> dependency property.</summary>
        public static readonly DependencyProperty SelectionHighlightColorProperty =
            DependencyProperty.Register(nameof(SelectionHighlightColor), typeof(SolidColorBrush), typeof(NumberBox<T>), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the text alignment of the text in the control.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        /// <summary>Identifies the <see cref="TextAlignment"/> dependency property.</summary>
        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(NumberBox<T>), new PropertyMetadata(TextAlignment.Left));

        /// <summary>
        /// Gets or sets a value that indicates how the reading order is determined for the <see cref="NumberBox"/>.
        /// </summary>
        /// <value>A value of the enumeration that specifies how the reading order is determined for the NumberBox.</value>
        public TextReadingOrder TextReadingOrder
        {
            get { return (TextReadingOrder)GetValue(TextReadingOrderProperty); }
            set { SetValue(TextReadingOrderProperty, value); }
        }

        /// <summary>Identifies the <see cref="TextReadingOrder"/> dependency property.</summary>
        public static readonly DependencyProperty TextReadingOrderProperty =
            DependencyProperty.Register(nameof(TextReadingOrder), typeof(TextReadingOrder), typeof(NumberBox<T>), new PropertyMetadata(TextReadingOrder.Default));

        /// <summary>
        /// Gets or sets a value that indicates whether the on-screen keyboard is shown when the control receives focus programmatically.
        /// </summary>
        /// <remarks>true if the on-screen keyboard is shown when the control receives focus programmatically; otherwise, false. The default is false.</remarks>
        public bool PreventKeyboardDisplayOnProgrammaticFocus
        {
            get { return (bool)GetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty); }
            set { SetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty, value); }
        }

        /// <summary>Identifies the <see cref="PreventKeyboardDisplayOnProgrammaticFocus"/> dependency property.</summary>
        public static readonly DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty =
            DependencyProperty.Register(nameof(PreventKeyboardDisplayOnProgrammaticFocus), typeof(bool), typeof(NumberBox<T>), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets content that is shown below the control. The content should provide guidance 
        /// about the input expected by the control.
        /// </summary>
        /// <value>Content that is shown below the control. The default is null.</value>
        public object? Description
        {
            get { return (object)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(object), typeof(NumberBox<T>), new PropertyMetadata(null));


        /// <summary>
        /// Gets or sets a value that specifies the input validation behavior to invoke when invalid input is entered.
        /// </summary>
        /// <value>A value of the enumeration that specifies the input validation behavior to invoke when invalid input is entered. The default is <see cref="NumberBoxValidationMode.InvalidInputOverwritten"/>.</value>
        public NumberBoxValidationMode ValidationMode
        {
            get { return (NumberBoxValidationMode)GetValue(ValidationModeProperty); }
            set { SetValue(ValidationModeProperty, value); }
        }

        /// <summary>Identifies the <see cref="ValidationMode"/> dependency property.</summary>
        public static readonly DependencyProperty ValidationModeProperty =
            DependencyProperty.Register(nameof(ValidationMode), typeof(NumberBoxValidationMode), typeof(NumberBox<T>), new PropertyMetadata(NumberBoxValidationMode.InvalidInputOverwritten, (s, e) => ((NumberBox<T>)s).OnValidationModePropertyChanged(e)));

        private void OnValidationModePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            ValidateInput();
            UpdateSpinButtonEnabled();
        }

        /// <summary>
        /// Gets or sets a value that indicates the placement of buttons used to increment or decrement the <see cref="Value"/> property.
        /// </summary>
        /// <value>A value of the enumeration that specifies the placement of buttons used to increment or 
        /// decrement the <see cref="Value"/> property. The default is <see cref="NumberBoxSpinButtonPlacementMode.Hidden"/>.</value>
        public NumberBoxSpinButtonPlacementMode SpinButtonPlacementMode
        {
            get { return (NumberBoxSpinButtonPlacementMode)GetValue(SpinButtonPlacementModeProperty); }
            set { SetValue(SpinButtonPlacementModeProperty, value); }
        }

        /// <summary>Identifies the <see cref="SpinButtonPlacementMode"/> dependency property.</summary>
        public static readonly DependencyProperty SpinButtonPlacementModeProperty =
            DependencyProperty.Register(nameof(SpinButtonPlacementMode), typeof(NumberBoxSpinButtonPlacementMode), typeof(NumberBox<T>), new PropertyMetadata(NumberBoxSpinButtonPlacementMode.Hidden, (s, e) => ((NumberBox<T>)s).OnSpinButtonPlacementModePropertyChanged(e)));

        private void OnSpinButtonPlacementModePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateSpinButtonPlacement();
        }

        /// <summary>
        /// Gets or sets a value that indicates whether line breaking occurs when header text
        /// extends beyond the available width of the control.
        /// </summary>
        /// <value>true if line breaking occurs when header text extends beyond the available width
        /// of the control; otherwise, false. The default is false.</value>
        public bool IsWrapEnabled
        {
            get { return (bool)GetValue(IsWrapEnabledProperty); }
            set { SetValue(IsWrapEnabledProperty, value); }
        }

        /// <summary>Identifies the <see cref="IsWrapEnabled"/> dependency property.</summary>
        public static readonly DependencyProperty IsWrapEnabledProperty =
            DependencyProperty.Register(nameof(IsWrapEnabled), typeof(bool), typeof(NumberBox<T>), new PropertyMetadata(false, (s, e) => ((NumberBox<T>)s).OnIsWrapEnabledPropertyChanged(e)));

        private void OnIsWrapEnabledPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateSpinButtonEnabled();
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the control accepts and evaluates a
        /// basic formulaic expression entered as input.
        /// </summary>
        /// <value>true if the NumberBox accepts and evaluates a basic formulaic expression entered as input; otherwise, false. The default is false.</value>
        public bool AcceptsExpression
        {
            get { return (bool)GetValue(AcceptsExpressionProperty); }
            set { SetValue(AcceptsExpressionProperty, value); }
        }

        /// <summary>Identifies the <see cref="AcceptsExpression"/> dependency property.</summary>
        public static readonly DependencyProperty AcceptsExpressionProperty =
            DependencyProperty.Register(nameof(AcceptsExpression), typeof(bool), typeof(NumberBox<T>), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets the object used to specify the formatting of <see cref="Value"/>.
        /// </summary>
        /// <value>The object used to specify the formatting of Value.</value>
        public INumberFormatter2 NumberFormatter
        {
            get { return (INumberFormatter2)GetValue(NumberFormatterProperty); }
            set { ValidateNumberFormatter(value); SetValue(NumberFormatterProperty, value); }
        }

        /// <summary>Identifies the <see cref="NumberFormatter"/> dependency property.</summary>
        public static readonly DependencyProperty NumberFormatterProperty =
            DependencyProperty.Register(nameof(NumberFormatter), typeof(INumberFormatter2), typeof(NumberBox<T>), new PropertyMetadata(null, (s,e) => ((NumberBox<T>)s).OnNumberFormatterChanged()));

        private void OnNumberFormatterChanged()
        {
            // Update text with new formatting
            UpdateTextToValue();
        }

        private void ValidateNumberFormatter(INumberFormatter2? value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            // NumberFormatter also needs to be an INumberParser
            if (value is not INumberParser)
            {
                throw new ArgumentException("Formatter must implement INumberParser");
            }
        }

        /// <summary>
        /// Occurs after the user triggers evaluation of new input by pressing the Enter key, clicking a spin button, or by changing focus.
        /// </summary>
        public event Windows.Foundation.TypedEventHandler<NumberBox<T>, NumberBoxValueChangedEventArgs<T>>? ValueChanged;

    }
}
