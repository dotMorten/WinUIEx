using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace WinUIEx
{
    internal class NumberBoxAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
    {
        public NumberBoxAutomationPeer(NumberBox numberBox) : base(numberBox)
        {
        }

        protected override object GetPatternCore(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.RangeValue)
            {
                return this;
            }

            return base.GetPatternCore(patternInterface);
        }
        protected override string GetClassNameCore()
        {
            return nameof(NumberBox);
        }
        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            if (string.IsNullOrEmpty(name))
            {
                if (Owner is NumberBox numberBox)
                {
                    name = TryGetStringRepresentationFromObject(numberBox.Header);
                }
            }

            return name;
        }

        private string TryGetStringRepresentationFromObject(object? obj)
        {
            if (obj is not null)
            {
                if (obj is string str)
                {
                    return str;
                }
                return obj.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        private NumberBox NumberBox => (NumberBox)Owner;

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Spinner;

        public bool IsReadOnly => throw new NotImplementedException();

        public double LargeChange => NumberBox.LargeChange;

        public double Maximum => NumberBox.Maximum;

        public double Minimum => NumberBox.Minimum;

        public double SmallChange => NumberBox.SmallChange;

        public double Value => NumberBox.Value;

        public void SetValue(double value) => NumberBox.Value = value;

        public void RaiseValueChangedEvent(double oldValue, double newValue)
        {
            base.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty,
                PropertyValue.CreateDouble(oldValue),
                PropertyValue.CreateDouble(newValue));
        }
    }
}
