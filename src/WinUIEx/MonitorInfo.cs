using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Foundation;
using System.Collections.Generic;
using Windows.Win32;
using System;

namespace WinUIEx
{
    /// <summary>
    /// Contains information about a display monitor.
    /// </summary>
    public class MonitorInfo
    {
        /// <summary>
        /// Gets the display monitors (including invisible pseudo-monitors associated with the mirroring drivers).
        /// </summary>
        /// <returns>A list of display monitors</returns>
        public unsafe static IList<MonitorInfo> GetDisplayMonitors()
        {
            
            int monitorCount = PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CMONITORS);
            List<MonitorInfo> list = new List<MonitorInfo>(monitorCount);
            var cbhandle = GCHandle.Alloc(list);
            var ptr = GCHandle.ToIntPtr(cbhandle);
            
            LPARAM data = new LPARAM(ptr);
            bool ok = PInvoke.EnumDisplayMonitors(new HDC(0), (RECT?)null, &MonitorEnumProc, data);
            cbhandle.Free();
            if (!ok)
                Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());
            return list;
        }

        [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
        private unsafe static BOOL MonitorEnumProc(HMONITOR hMonitor, HDC hdcMonitor, RECT* lprcMonitor, LPARAM dwData)
        {
            var handle = GCHandle.FromIntPtr(dwData.Value);
            if(!lprcMonitor->IsEmpty && handle.IsAllocated && handle.Target is List<MonitorInfo> list)
                list.Add(new MonitorInfo(hMonitor, *lprcMonitor));
            return new BOOL(1);
        }

        private readonly HMONITOR _monitor;

        internal unsafe MonitorInfo(HMONITOR monitor, RECT rect)
        {
            RectMonitor =
                new Rect(new Point(rect.left, rect.top),
                new Point(rect.right, rect.bottom));
            _monitor = monitor;
            var info = new MONITORINFOEXW() { monitorInfo = new MONITORINFO() { cbSize = (uint)sizeof(MONITORINFOEXW) } };
            GetMonitorInfo(monitor, ref info);
            RectWork =
                new Rect(new Point(info.monitorInfo.rcWork.left, info.monitorInfo.rcWork.top),
                new Point(info.monitorInfo.rcWork.right, info.monitorInfo.rcWork.bottom));
            Name = new string(info.szDevice.AsSpan()).Replace("\0", "").Trim();
        }

        /// <summary>
        /// Gets the name of the display.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the display monitor rectangle, expressed in virtual-screen coordinates.
        /// </summary>
        /// <remarks>
        /// <note>If the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.</note>
        /// </remarks>
        public Rect RectMonitor { get; }

        /// <summary>
        /// Gets the work area rectangle of the display monitor, expressed in virtual-screen coordinates.
        /// </summary>
        /// <remarks>
        /// <note>If the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.</note>
        /// </remarks>
        public Rect RectWork { get; }

        /// <inheritdoc />
        public override string ToString() => $"{Name} {RectMonitor.Width}x{RectMonitor.Height}";

        private static unsafe bool GetMonitorInfo(HMONITOR hMonitor, ref MONITORINFOEXW lpmi)
        {
            fixed (MONITORINFOEXW* lpmiLocal = &lpmi)

            {
                bool __result = GetMonitorInfo(hMonitor, lpmiLocal);
                return __result;
            }
        }
        [DllImport("User32", ExactSpelling = true, EntryPoint = "GetMonitorInfoW")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)] private static extern unsafe bool GetMonitorInfo(HMONITOR hMonitor, MONITORINFOEXW* lpmi);
    }
}