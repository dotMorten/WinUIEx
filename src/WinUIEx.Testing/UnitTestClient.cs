using System;
using System.Collections.Generic;
using System.Text;

namespace WinUIEx.Testing
{
    public static class UnitTestClient
    {
        public static void Run(Microsoft.UI.Xaml.Window window)
        {
            Window = window;
            Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.CreateDefaultUI();
            Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(Environment.CommandLine);
        }

        public static Microsoft.UI.Xaml.Window Window { get; private set; }
    }
}
