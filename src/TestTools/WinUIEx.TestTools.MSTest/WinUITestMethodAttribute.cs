using System;
using System.Collections.Generic;
using System.Text;

namespace WinUIEx.TestTools.MSTest
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class WinUITestMethodAttribute : Attribute
    {
        public WinUITestMethodAttribute()
        {
        }
    }
}
