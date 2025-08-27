## `NumberBox<T>`

Port of WinUI's NumberBox control to support generic number types.

This control supports validation, increment stepping, and computing inline calculations of basic equations such as multiplication, division, addition, and subtraction.

Port of WinUI's `NumberBox` to `NumberBox<T>` to allow for any numeric datatype. Exposes two controls out of the box: `NumberBoxDecimal` and `NumberBoxInt32` but you can extend with your own by creating the class.

### Binding non-nullable values
The NumberBox<T>.Value is of type `T?` (nullable) to allow for null values when the user clears the text. If you bind to a non-nullable property, you must set `AllowNull="False"` on the control to prevent binding errors.

### Note on assigning decimal values:
To be able to assign Decimal values to the NumberBoxDecimal control, it can't be done in XAML (although x:Bind works). Support for decimal values is a limitation in the Windows App SDK and is supposed to be addressed in v1.8, but is currently not available in 1.8preview1 (see microsoft/WindowsAppSDK#5756)

## Usage

Register the WinUIEx xmlns namespace and add one of the specified controls :
```xml
 <ex:NumberBoxInt32 Header="NumberBoxInt32" AllowNull="True"
                AcceptsExpression="True" 
                Value="{x:Bind VM.IntValue, Mode=TwoWay}"
                AllowNulls="False"
                Minimum="-10"
                Maximum="10"
                IsWrapEnabled="True"
                Description="32bit Integer"
                PlaceholderText="Enter a whole number"  />

<ex:NumberBoxDecimal Header="NumberBoxDecimal" AllowNull="False"
                AcceptsExpression="False" 
                NumberFormatter="{x:Bind VM.Formatter, Mode=OneWay}"
                Value="{x:Bind VM.DecimalValue, Mode=TwoWay}"
                Description="Decimal"
                PlaceholderText="Enter number" />>
```
to
```xml
<winex:WindowEx xmlns:winex="using:WinUIEx" Width="1024" Height="768"  ...>
```

## Extending with your own number-type

First create a custom subclass of `NumberBox<T>`, for example for `float`:

```cs
public class NumberBoxFloat : NumberBox<float>
{
     public NumberBoxFloat() => DefaultStyleKey = typeof(NumberBoxFloat);
} 
```cs

Next duplicate the control template from the other NumberBox controls and update the target type to match the new type name.

