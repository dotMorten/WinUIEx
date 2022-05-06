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

            await VerifyCS.VerifyAnalyzerAsync(test, new Microsoft.CodeAnalysis.Testing.DiagnosticResult("WinUIEX1", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning));
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
            if (AppWindowTitleBar.IsCustomizationSupported()) 
            {
                appWindow.TitleBar.ButtonBackgroundColor = color;
                appWindow.TitleBar.BackgroundColor = color;
                appWindow.TitleBar.ButtonInactiveBackgroundColor = color;
                appWindow.TitleBar.ButtonPressedBackgroundColor = color;
                appWindow.TitleBar.InactiveBackgroundColor = color;
            }
        }
    }
}";
            
            await VerifyCS.VerifyAnalyzerAsync(test);
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
                new Microsoft.CodeAnalysis.Testing.DiagnosticResult("WinUIEX1", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning),
                new Microsoft.CodeAnalysis.Testing.DiagnosticResult("WinUIEX1", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning),
                new Microsoft.CodeAnalysis.Testing.DiagnosticResult("WinUIEX1", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning),
                new Microsoft.CodeAnalysis.Testing.DiagnosticResult("WinUIEX1", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning),
                new Microsoft.CodeAnalysis.Testing.DiagnosticResult("WinUIEX1", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                );
        }

        /*
        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod3()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class {|#0:TypeName|}
        {   
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";

            var expected = VerifyCS.Diagnostic("WinUIExAnalyzers").WithLocation(0).WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }*/
    }
}
