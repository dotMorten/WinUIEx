using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUIEx.TestTools
{
    /// <summary>
    /// Test host helper class
    /// </summary>
    public static class TestHost
    {
        /// <summary>
        /// Gets or sets the current window for the test. This property will be set by the generated test code.
        /// </summary>
        public static Microsoft.UI.Xaml.Window Window { get; set; }
    }
}
