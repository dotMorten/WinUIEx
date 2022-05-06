using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = WinUIEx.Analyzers.Test.CSharpCodeFixVerifier<
    WinUIEx.Analyzers.WinUIExAnalyzersAnalyzer,
    WinUIEx.Analyzers.WinUIExAnalyzersCodeFixProvider>;

namespace WinUIEx.Analyzers.Test
{
    [TestClass]
    public class WinUIExAnalyzersUnitTest
    {
        [TestMethod]
        public async Task TestNoCode()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
        [TestMethod]
        public async Task TestMicaController_AddSystemBackdropTarget_Guarded()
        {
            var test = @"
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT;

namespace App1
{
    internal class Class1
    {
        public void Test1(Window window, SystemBackdropConfiguration configuration)
        {
            var micaController = new MicaController();
            micaController.SetSystemBackdropConfiguration(configuration);
            if (MicaController.IsSupported())
            {
               micaController.AddSystemBackdropTarget(window.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
            }
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
        [TestMethod]
        public async Task TestMicaController_AddSystemBackdropTarget_Unguarded()
        {
            var test = @"
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT;

namespace App1
{
    internal class Class1
    {
        public void Test1(Window window, SystemBackdropConfiguration configuration)
        {
            var micaController = new MicaController();
            micaController.SetSystemBackdropConfiguration(configuration);
            micaController.AddSystemBackdropTarget(window.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
        }
    }
}";
            var expected = DiagnosticResult.CompilerWarning("WinUIEx01").WithSpan(16, 13, 16, 125).WithArguments("AddSystemBackdropTarget", "MicaController.IsSupported()");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestAppWindowTitleBar_Guarded()
        {
            var test = @"
using Microsoft.UI.Windowing;

namespace App1
{
    internal class Class1
    {
        public void Test2(AppWindow appWindow, Windows.UI.Color color)
        {
if(false) {
            if (AppWindowTitleBar.IsCustomizationSupported()) 
            {
                appWindow.TitleBar.ButtonBackgroundColor = color;
                appWindow.TitleBar.BackgroundColor = color;
                appWindow.TitleBar.ButtonInactiveBackgroundColor = color;
                appWindow.TitleBar.ButtonPressedBackgroundColor = color;
                appWindow.TitleBar.InactiveBackgroundColor = color;
            }}
        }
    }
}";
            
            await VerifyCS.VerifyAnalyzerAsync(test);
        }


        [System.Runtime.Versioning.SupportedOSPlatform("ios14.0")]
        private void Test()
        {

        }
        public void Test2()
        {
            if (System.OperatingSystem.IsIOSVersionAtLeast(14))
                Test();
        }

        [TestMethod]
        public async Task TestAppWindowTitleBar_Unguarded()
        {
            var test = @"
using Microsoft.UI.Windowing;

namespace App1
{
    internal class Class1
    {
        public void Test2(AppWindow appWindow, Windows.UI.Color color)
        {
            appWindow.TitleBar.ButtonBackgroundColor = color;
            appWindow.TitleBar.BackgroundColor = color;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = color;
            appWindow.TitleBar.ButtonPressedBackgroundColor = color;
            appWindow.TitleBar.InactiveBackgroundColor = color;
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                DiagnosticResult.CompilerWarning("WinUIEx01").WithSpan(10, 13, 10, 53).WithArguments("ButtonBackgroundColor", "AppWindowTitleBar.IsCustomizationSupported()"),
                DiagnosticResult.CompilerWarning("WinUIEx01").WithSpan(11, 13, 11, 47).WithArguments("BackgroundColor", "AppWindowTitleBar.IsCustomizationSupported()"),
                DiagnosticResult.CompilerWarning("WinUIEx01").WithSpan(12, 13, 12, 61).WithArguments("ButtonInactiveBackgroundColor", "AppWindowTitleBar.IsCustomizationSupported()"),
                DiagnosticResult.CompilerWarning("WinUIEx01").WithSpan(13, 13, 13, 60).WithArguments("ButtonPressedBackgroundColor", "AppWindowTitleBar.IsCustomizationSupported()"),
                DiagnosticResult.CompilerWarning("WinUIEx01").WithSpan(14, 13, 14, 55).WithArguments("InactiveBackgroundColor", "AppWindowTitleBar.IsCustomizationSupported()")
                );
        }
    }
}
