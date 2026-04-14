using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace WinUIEx.Analyzers.Test
{
    [TestClass]
    public class WinUIExFrameNavigateAnalyzerTests : BaseAnalyzersUnitTest<WinUIEx.Analyzers.WinUIExFrameNavigateAnalyzer, WinUIEx.Analyzers.WinUIExAnalyzersCodeFixProvider>
    {
        [TestMethod]
        public async Task Frame_Navigate_With_NonPage_Type()
        {
            var testCode = @"
    using System;
    using Microsoft.UI.Xaml.Controls;
    namespace ConsoleApplication1
    {
        class MyViewModel { }

        class MyClass
        {
            public void MethodName(Frame frame)
            {
                frame.Navigate({|#0:typeof(MyViewModel)|});
            }
        }
    }";
            var expected = Diagnostic("WinUIEx1003").WithLocation(0).WithArguments("ConsoleApplication1.MyViewModel", "Microsoft.UI.Xaml.Controls.Page");
            await VerifyAnalyzerAsync(testCode, expected);
        }

        [TestMethod]
        public async Task Frame_Navigate_With_Page_Subclass()
        {
            var testCode = @"
    using System;
    using Microsoft.UI.Xaml.Controls;
    namespace ConsoleApplication1
    {
        class MyPage : Page { }

        class MyClass
        {
            public void MethodName(Frame frame)
            {
                frame.Navigate(typeof(MyPage));
            }
        }
    }";
            await VerifyAnalyzerAsync(testCode);
        }

        [TestMethod]
        public async Task Frame_Navigate_With_Page_Base_Type()
        {
            var testCode = @"
    using System;
    using Microsoft.UI.Xaml.Controls;
    namespace ConsoleApplication1
    {
        class MyClass
        {
            public void MethodName(Frame frame)
            {
                frame.Navigate(typeof(Page));
            }
        }
    }";
            await VerifyAnalyzerAsync(testCode);
        }
    }
}
