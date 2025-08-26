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
    internal class NumberBoxAutomationPeer<T> : FrameworkElementAutomationPeer, IRangeValueProvider
        where T : struct, System.Numerics.INumber<T>, System.Numerics.IMinMaxValue<T>
    {
        public NumberBoxAutomationPeer(NumberBox<T> numberBox) : base(numberBox)
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
                if (Owner is NumberBox<T> numberBox)
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

        private NumberBox<T> NumberBox => (NumberBox<T>)Owner;

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Spinner;

        public bool IsReadOnly => throw new NotImplementedException();

        public double LargeChange => double.CreateTruncating(NumberBox.LargeChange);

        public double Maximum => double.CreateTruncating(NumberBox.Maximum);

        public double Minimum => double.CreateTruncating(NumberBox.Minimum);

        public double SmallChange => double.CreateTruncating(NumberBox.SmallChange);

        public double Value => NumberBox.Value.HasValue ? double.CreateTruncating(NumberBox.Value.Value) : double.NaN;

        public void SetValue(double value) => NumberBox.Value = T.CreateTruncating(value);

        public void RaiseValueChangedEvent(T? oldValue, T? newValue)
        {
            base.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValue.HasValue ? oldValue.Value : double.NaN, newValue.HasValue ? newValue.Value : double.NaN);
            //base.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty,
            //    PropertyValue.CreateDouble(oldValue),
            //    PropertyValue.CreateDouble(newValue));
        }
    }
}
