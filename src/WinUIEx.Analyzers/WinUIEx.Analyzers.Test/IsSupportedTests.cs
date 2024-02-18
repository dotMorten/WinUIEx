using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace WinUIEx.Analyzers.Test
{
    [TestClass]
    public class IsSupportedTests : BaseAnalyzersUnitTest<WinUIEx.Analyzers.PlatformAnalyzer, WinUIEx.Analyzers.WinUIExAnalyzersCodeFixProvider>
    {
        [TestMethod]
        public async Task TitleBar_BackgroundColor_NotGuarded()
        {
            var testCode = @"
    using Microsoft.UI.Xaml;
    namespace ConsoleApplication1
    {
        class MyClass : Window
        {    
            public void MethodName()
            {
                {|#0:AppWindow.TitleBar.BackgroundColor|} = Microsoft.UI.Colors.Transparent;
            }
        }
    }";
            var expected = Diagnostic("WinUIEx2001").WithLocation(0).WithArguments("BackgroundColor", "AppWindowTitleBar.IsCustomizationSupported()");
            await VerifyAnalyzerAsync(testCode, expected);
        }

        [TestMethod]
        public async Task TitleBar_ResetToDefault_NotGuarded()
        {
            var testCode = @"
    using Microsoft.UI.Xaml;
    namespace ConsoleApplication1
    {
        class MyClass : Window
        {    
            public void MethodName()
            {
                {|#0:AppWindow.TitleBar.ResetToDefault()|};;
            }
        }
    }";
            var expected = Diagnostic("WinUIEx2001").WithLocation(0).WithArguments("ResetToDefault", "AppWindowTitleBar.IsCustomizationSupported()");
            await VerifyAnalyzerAsync(testCode, expected);
        }

        [TestMethod]
        public async Task TitleBar_BackgroundColor_Guarded()
        {
            var testCode = @"
    using Microsoft.UI.Xaml;
    namespace ConsoleApplication1
    {
        class MyClass : Window
        {    
            public void MethodName()
            {
                if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
                    AppWindow.TitleBar.BackgroundColor = Microsoft.UI.Colors.Transparent;
            }
        }
    }";
            await VerifyAnalyzerAsync(testCode);
        }

        [TestMethod]
        public async Task TitleBar_CodeBlock_Guarded()
        {
            var testCode = @"
    using Microsoft.UI.Xaml;
    namespace ConsoleApplication1
    {
        class MyClass : Window
        {    
            public void MethodName()
            {
                if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported()) {
                    AppWindow.TitleBar.BackgroundColor = Microsoft.UI.Colors.Transparent;
                    AppWindow.TitleBar.SetDragRectangles(new Windows.Graphics.RectInt32[] { new Windows.Graphics.RectInt32() { Width = 5, Height = 5, X = 0, Y = 0 } });
                }
            }
        }
    }";
            await VerifyAnalyzerAsync(testCode);
        }

        [TestMethod]
        public async Task TitleBar_SupportedOSPlatformAttribute_OnMethod()
        {
            var testCode = @"
    using Microsoft.UI.Xaml;
    namespace ConsoleApplication1
    {
        class MyClass : Window
        {    
            [System.Runtime.Versioning.SupportedOSPlatform(""windows11.0.0.0"")]
            public void MethodName()
            {
                AppWindow.TitleBar.BackgroundColor = Microsoft.UI.Colors.Transparent;
            }
        }
    }";
            await VerifyAnalyzerAsync(testCode);
        }

        [TestMethod]
        public async Task TitleBar_SupportedOSPlatformAttribute_OnClass()
        {
            var testCode = @"
    using Microsoft.UI.Xaml;
    namespace ConsoleApplication1
    {
        [System.Runtime.Versioning.SupportedOSPlatform(""windows11.0.0.0"")]
        class MyClass : Window
        {    
            public void MethodName()
            {
                AppWindow.TitleBar.BackgroundColor = Microsoft.UI.Colors.Transparent;
            }
        }
    }";
            await VerifyAnalyzerAsync(testCode);
        }

        [TestMethod]
        public async Task TitleBar_PlatformCheckShouldNotWarn()
        {
            var testCode = @"
    using Microsoft.UI.Xaml;
    namespace ConsoleApplication1
    {
        class MyClass : Window
        {    
            public void MethodName()
            {
                if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported()) { }
            }
        }
    }";
            await VerifyAnalyzerAsync(testCode);
        }
    }
}
