using System;
using System.Collections.Generic;
using System.Text;

namespace WinUIEx.TestTools.MSTest
{
    public static class UnitTestClient
    {
        public static void Run(Microsoft.UI.Xaml.Window window)
        {
            TestHost.Window = window;
            Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.CreateDefaultUI();
            Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(Environment.CommandLine);
        }
    }
}
