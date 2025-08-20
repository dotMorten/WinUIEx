using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using Windows.Globalization.NumberFormatting;
using Windows.System;

namespace WinUIEx
{
    public sealed partial class NumberBox : Control
    {

        const string c_numberBoxHeaderName = "HeaderContentPresenter";
        const string c_numberBoxDownButtonName = "DownSpinButton";
        const string c_numberBoxUpButtonName = "UpSpinButton";
        const string c_numberBoxTextBoxName = "InputBox";
        const string c_numberBoxPopupName = "UpDownPopup";
        const string c_numberBoxPopupDownButtonName = "PopupDownSpinButton";
        const string c_numberBoxPopupUpButtonName = "PopupUpSpinButton";
        const string c_numberBoxPopupContentRootName = "PopupContentRoot";
        const double c_popupShadowDepth = 16.0;
        const string c_numberBoxPopupShadowDepthName = "NumberBoxPopupShadowDepth";

        private TextBox? m_textBox;
        private Popup? m_popup;
        private ContentPresenter? m_headerPresenter;
        private bool m_valueUpdating = false;
        private bool m_textUpdating = false;

        private SignificantDigitsNumberRounder m_displayRounder = new SignificantDigitsNumberRounder();

        public NumberBox()
        {
            DefaultStyleKey = typeof(NumberBox);

            NumberFormatter = GetRegionalSettingsAwareDecimalFormatter();

            PointerWheelChanged += OnNumberBoxScroll;

            GotFocus += OnNumberBoxGotFocus;
            LostFocus += OnNumberBoxLostFocus;

            SetDefaultInputScope();

            RegisterPropertyChangedCallback(AutomationProperties.NameProperty, OnAutomationPropertiesNamePropertyChanged);
            RegisterPropertyChangedCallback(AutomationProperties.LabeledByProperty, OnAutomationPropertiesLabeledByPropertyChanged);
        }

        private void SetDefaultInputScope()
        {
            // Sets the default value of the InputScope property.
            // Note that InputScope is a class that cannot be set to a default value within the IDL.
            var inputScopeName = new InputScopeName(InputScopeNameValue.Number);
            var inputScope = new InputScope();
            inputScope.Names.Add(inputScopeName);
            SetValue(InputScopeProperty, inputScope);
        }

        // This was largely copied from Calculator's GetRegionalSettingsAwareDecimalFormatter()
        private DecimalFormatter GetRegionalSettingsAwareDecimalFormatter()
        {
            DecimalFormatter? formatter = null;

            var currentLocale = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            if (currentLocale.Length > 0)
            {
                List<string> languageList = new List<string>();
                languageList.Add(currentLocale);
                formatter = new DecimalFormatter(languageList, Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion);
            }

            if (formatter is null)
            {
                formatter = new DecimalFormatter();
            }

            formatter.IntegerDigits = 1;
            formatter.FractionDigits = 0;

            return formatter;
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new NumberBoxAutomationPeer(this);

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild(c_numberBoxDownButtonName) is RepeatButton spinDown)
            {
                spinDown.Click += OnSpinDownClick;
                if (string.IsNullOrEmpty(AutomationProperties.GetName(spinDown)))
                {
                    AutomationProperties.SetName(spinDown, ResourceAccessor.GetLocalizedStringResource("NumberBoxDownSpinButtonName"));
                }
            }
            if (GetTemplateChild(c_numberBoxUpButtonName) is RepeatButton upDown)
            {
                upDown.Click += OnSpinUpClick;
                if (string.IsNullOrEmpty(AutomationProperties.GetName(upDown)))
                {
                    AutomationProperties.SetName(upDown, ResourceAccessor.GetLocalizedStringResource("NumberBoxUpSpinButtonName"));
                }
            }
            UpdateHeaderPresenterState();
            m_textBox = GetTemplateChild(c_numberBoxTextBoxName) as TextBox;
            if (m_textBox is not null)
            {
                m_textBox.PreviewKeyDown += OnTextBoxKeyDown;
                m_textBox.KeyUp += OnTextBoxKeyUp;
                m_textBox.Loaded += OnTextBoxLoaded;
            }
            m_popup = GetTemplateChild(c_numberBoxPopupName) as Popup;
            if (GetTemplateChild(c_numberBoxPopupContentRootName) is UIElement popupRoot)
            {
                if (popupRoot.Shadow is null)
                {
                    popupRoot.Shadow = new ThemeShadow();
                    var translation = popupRoot.Translation;
                    if (FindInApplicationResources(c_numberBoxPopupShadowDepthName, c_popupShadowDepth) is double value)
                        popupRoot.Translation = new(translation.X, translation.Y, (float)value);
                }
            }

            if (GetTemplateChild(c_numberBoxPopupDownButtonName) is RepeatButton popupSpinDown)
            {
                popupSpinDown.Click += OnSpinDownClick;
            }

            if (GetTemplateChild(c_numberBoxPopupUpButtonName) is RepeatButton popupSpinUp)
    {
                popupSpinUp.Click += OnSpinUpClick;
            }

            IsEnabledChanged += OnIsEnabledChanged;

            m_displayRounder.SignificantDigits = 10;

            UpdateSpinButtonPlacement();
            UpdateSpinButtonEnabled();

            UpdateVisualStateForIsEnabledChange();

            ReevaluateForwardedUIAProperties();

            if (ReadLocalValue(ValueProperty) == DependencyProperty.UnsetValue
                && ReadLocalValue(TextProperty) != DependencyProperty.UnsetValue)
            {
                // If Text has been set, but Value hasn't, update Value based on Text.
                UpdateValueToText();
            }
            else
            {
                UpdateTextToValue();
            }
        }

        object FindInApplicationResources(string resource, object defaultValue)
        {
            return FindResource(resource, Application.Current.Resources, defaultValue);
        }

        object FindResource(object resource, ResourceDictionary resources, object defaultValue)
        {
            if (resources.TryGetValue(resource, out object value))
                return value;
            return defaultValue;
        }

        private void OnTextBoxLoaded(object sender, RoutedEventArgs e)
        {
            // Updating again once TextBox is loaded so its visual states are set properly.
            UpdateSpinButtonPlacement();
        }

        void UpdateValueToText()
        {
            if (m_textBox is not null)
            {
                m_textBox.Text = Text;
                ValidateInput();
            }
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateVisualStateForIsEnabledChange();
        }


        private void OnAutomationPropertiesNamePropertyChanged(DependencyObject _, DependencyProperty __)
        {
            ReevaluateForwardedUIAProperties();
        }

        private void OnAutomationPropertiesLabeledByPropertyChanged(DependencyObject _, DependencyProperty __)
        {
            ReevaluateForwardedUIAProperties();
        }

        void ReevaluateForwardedUIAProperties()
        {
            if (m_textBox is TextBox textBox)
            {
                // UIA Name
                var name = AutomationProperties.GetName(this);
                var minimum = Minimum == double.MinValue ?
                    string.Empty : " " + ResourceAccessor.GetLocalizedStringResource("NumberBoxMinimumValueStatus") + Minimum.ToString();
                ;
                var maximum = Maximum == double.MaxValue ?
                    string.Empty :
                    " " + ResourceAccessor.GetLocalizedStringResource("NumberBoxMaximumValueStatus") + Maximum.ToString();
                ;

                if (!string.IsNullOrEmpty(name))
                {
                    // AutomationProperties.Name is a non empty string, we will use that value.
                    AutomationProperties.SetName(textBox, name + minimum + maximum);
                }
                else
                {
                    if (Header is string headerAsString)
                    {
                        // Header is a string, we can use that as our UIA name.
                        AutomationProperties.SetName(textBox, headerAsString + minimum + maximum);
                    }
                }

                // UIA LabeledBy
                var labeledBy = AutomationProperties.GetLabeledBy(this);
                if (labeledBy is not null)
                {
                    AutomationProperties.SetLabeledBy(textBox, labeledBy);
                }
            }
        }

        private void UpdateVisualStateForIsEnabledChange()
        {
            VisualStateManager.GoToState(this, IsEnabled ? "Norma" : "Disabled", false);
        }

        void OnNumberBoxGotFocus(object sender, RoutedEventArgs args)
        {
            // When the control receives focus, select the text
            m_textBox?.SelectAll();

            if (SpinButtonPlacementMode == NumberBoxSpinButtonPlacementMode.Compact && m_popup is not null)
            {
                m_popup.IsOpen = true;
            }
        }

        void OnNumberBoxLostFocus(object sender, RoutedEventArgs args)
        {
            ValidateInput();
            if (m_popup is not null)
                m_popup.IsOpen = false;
        }

        void CoerceMinimum()
        {
            var max = Maximum;
            if (Minimum > max)
            {
                Minimum = max;
            }
        }

        void CoerceMaximum()
        {
            var min = Minimum;
            if (Maximum < min)
            {
                Maximum = min;
            }
        }

        void CoerceValue()
        {
            // Validate that the value is in bounds
            var value = Value;
            if (!double.IsNaN(value) && !IsInBounds(value) && ValidationMode == NumberBoxValidationMode.InvalidInputOverwritten)
            {
                // Coerce value to be within range
                var max = Maximum;
                if (value > max)
                {
                    Value = max;
                }
                else
                {
                    Value = Minimum;
                }
            }
        }

        void ValidateInput()
        {
            // Validate the content of the inner textbox
            if (m_textBox is TextBox textBox)
            {
                var text = textBox.Text.Trim();

                // Handles empty TextBox case, set text to current value
                if (string.IsNullOrEmpty(text))
                {
                    Value = double.NaN;
                }
                else
                {
                    // Setting NumberFormatter to something that isn't an INumberParser will throw an exception, so this should be safe
                    var numberParser = (INumberParser)NumberFormatter;

                    double? value = AcceptsExpression
                        ? NumberBoxParser.Compute(text, numberParser)
                        : numberParser.ParseDouble(text);

                    if (!value.HasValue)
                    {
                        if (ValidationMode == NumberBoxValidationMode.InvalidInputOverwritten)
                        {
                            // Override text to current value
                            UpdateTextToValue();
                        }
                    }
                    else
                    {
                        if (value.Value == Value)
                        {
                            // Even if the value hasn't changed, we still want to update the text (e.g. Value is 3, user types 1 + 2, we want to replace the text with 3)
                            UpdateTextToValue();
                        }
                        else
                        {
                            Value = value.Value;
                        }
                    }
                }
            }
        }
        private void OnSpinDownClick(object sender, RoutedEventArgs e)
        {
            StepValue(-SmallChange);
        }

        private void OnSpinUpClick(object sender, RoutedEventArgs e)
        {
            StepValue(SmallChange);
        }

        private void OnTextBoxKeyDown(object sender, KeyRoutedEventArgs args)
        {
            // Handle these on key down so that we get repeat behavior.
            switch (args.OriginalKey)
            {
                case VirtualKey.Up:
                    StepValue(SmallChange);
                    args.Handled = true;
                    break;

                case VirtualKey.Down:
                    StepValue(-SmallChange);
                    args.Handled = true;
                    break;

                case VirtualKey.PageUp:
                    StepValue(LargeChange);
                    args.Handled = true;
                    break;

                case VirtualKey.PageDown:
                    StepValue(-LargeChange);
                    args.Handled = true;
                    break;
            }
        }


        private void OnTextBoxKeyUp(object sender, KeyRoutedEventArgs args)
        {
            switch (args.OriginalKey)
            {
                case VirtualKey.Enter:
                case VirtualKey.GamepadA:
                    ValidateInput();
                    args.Handled = true;
                    break;

                case VirtualKey.Escape:
                case VirtualKey.GamepadB:
                    UpdateTextToValue();
                    args.Handled = true;
                    break;
            }
        }

        private void OnNumberBoxScroll(object sender, PointerRoutedEventArgs args)
        {
            if (m_textBox is TextBox textBox)
            {
                if (textBox.FocusState != FocusState.Unfocused)
                {
                    var delta = args.GetCurrentPoint(this).Properties.MouseWheelDelta;
                    if (delta > 0)
                    {
                        StepValue(SmallChange);
                    }
                    else if (delta < 0)
                    {
                        StepValue(-SmallChange);
                    }
                    // Only set as handled when we actually changed our state.
                    args.Handled = true;
                }
            }
        }

        private void StepValue(double change)
        {
            // Before adjusting the value, validate the contents of the textbox so we don't override it.
            ValidateInput();

            var newVal = Value;
            if (!double.IsNaN(newVal))
            {
                newVal += change;

                if (IsWrapEnabled)
                {
                    var max = Maximum;
                    var min = Minimum;

                    if (newVal > max)
                    {
                        newVal = min;
                    }
                    else if (newVal < min)
                    {
                        newVal = max;
                    }
                }

                Value = newVal;

                // We don't want the caret to move to the front of the text for example when using the up/down arrows
                // to change the numberbox value.
                MoveCaretToTextEnd();
            }
        }

        // Updates TextBox.Text with the formatted Value
        void UpdateTextToValue()
        {
            if (m_textBox is TextBox textBox)
            {
                string newText = "";

                var value = Value;
                if (!double.IsNaN(value))
                {
                    // Rounding the value here will prevent displaying digits caused by floating point imprecision.
                    var roundedValue = m_displayRounder.RoundDouble(value);
                    newText = NumberFormatter.FormatDouble(roundedValue);
                }

                textBox.Text = newText;

                m_textUpdating = true;
                try
                {
                    Text = newText;
                }
                finally {
                    m_textUpdating = false;
                }
            }
        }

        void UpdateSpinButtonPlacement()
        {
            var spinButtonMode = SpinButtonPlacementMode;
            var state = "SpinButtonsCollapsed";

            if (spinButtonMode == NumberBoxSpinButtonPlacementMode.Inline)
            {
                state = "SpinButtonsVisible";
            }
            else if (spinButtonMode == NumberBoxSpinButtonPlacementMode.Compact)
            {
                state = "SpinButtonsPopup";
            }

            VisualStateManager.GoToState(this, state, false);

            if (m_textBox is TextBox textbox)
            {
                VisualStateManager.GoToState(textbox, state, false);
            }
        }

        void UpdateSpinButtonEnabled()
        {
            var value = Value;
            bool isUpButtonEnabled = false;
            bool isDownButtonEnabled = false;

            if (!double.IsNaN(value))
            {
                if (IsWrapEnabled || ValidationMode != NumberBoxValidationMode.InvalidInputOverwritten)
                {
                    // If wrapping is enabled, or invalid values are allowed, then the buttons should be enabled
                    isUpButtonEnabled = true;
                    isDownButtonEnabled = true;
                }
                else
                {
                    if (value < Maximum)
                    {
                        isUpButtonEnabled = true;
                    }
                    if (value > Minimum)
                    {
                        isDownButtonEnabled = true;
                    }
                }
            }

            VisualStateManager.GoToState(this, isUpButtonEnabled ? "UpSpinButtonEnabled" : "UpSpinButtonDisabled", false);
            VisualStateManager.GoToState(this, isDownButtonEnabled ? "DownSpinButtonEnabled" : "DownSpinButtonDisabled", false);
        }

        bool IsInBounds(double value)
        {
            return (value >= Minimum && value <= Maximum);
        }

        void UpdateHeaderPresenterState()
        {
            bool shouldShowHeader = false;

            // Load header presenter as late as possible

            // To enable lightweight styling, collapse header presenter if there is no header specified
            if (Header is object header)
            {
                // Check if header is string or not
                if (header is string headerAsString)
                {
                    if (!string.IsNullOrEmpty(headerAsString))
                    {
                        // Header is not empty string
                        shouldShowHeader = true;
                    }
                }
                else
                {
                    // Header is not a string, so let's show header presenter
                    shouldShowHeader = true;
                    // When our header isn't a string, we use the NumberBox's UIA name for the textbox's UIA name.
                    if (m_textBox is TextBox textBox)
                    {
                        AutomationProperties.SetName(textBox, AutomationProperties.GetName(this));
                    }
                }
            }
            if (HeaderTemplate is not null)
            {
                shouldShowHeader = true;
            }

            if (shouldShowHeader && m_headerPresenter is null)
            {
                if (GetTemplateChild(c_numberBoxHeaderName) is ContentPresenter headerPresenter)
                {
                    // Set presenter to enable lightweight styling of the headers margin
                    m_headerPresenter = headerPresenter;
                }
            }

            if (m_headerPresenter is not null)
            {
                m_headerPresenter.Visibility = shouldShowHeader ? Visibility.Visible : Visibility.Collapsed;
            }

            ReevaluateForwardedUIAProperties();
        }

        void MoveCaretToTextEnd()
        {
            if (m_textBox is TextBox textBox)
            {
                // This places the caret at the end of the text.
                textBox.Select(textBox.Text.Length, 0);
            }
        }


    }

    public sealed class NumberBoxValueChangedEventArgs : EventArgs
    {
        internal NumberBoxValueChangedEventArgs(double oldValue, double newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public double OldValue { get; }
        public double NewValue { get; }
    }

}
