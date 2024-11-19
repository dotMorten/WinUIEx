// Based on an implementation by Castorix: https://github.com/castorix/WinUI3_SplashScreen

//#define MEDIAPLAYER
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Xml.XPath;

#if MEDIAPLAYER
using MFPlay;
#endif
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinUIEx
{
    /// <summary>
    /// Simple and fast splash screen for display images during application startup.
    /// </summary>
    /// <remarks>
    /// Once your application window has launched/loaded, The splashscreen should be removed by either disposing this instance or calling <see cref="Hide(TimeSpan)"/>.
    /// </remarks>
    /// <seealso cref="SplashScreen"/>
    public sealed class SimpleSplashScreen : IDisposable
    {
        private const nint COLOR_BACKGROUND = 1;

        private DispatcherTimer? dTimer;
        private TimeSpan tsFadeoutDuration;
        private DateTime tsFadeoutEnd;
        Windows.Win32.Foundation.HWND hWndSplash = Windows.Win32.Foundation.HWND.Null;
        private Windows.Win32.Graphics.Gdi.HGDIOBJ hBitmap = Windows.Win32.Graphics.Gdi.HGDIOBJ.Null;
        private nuint initToken = 0;
#if MEDIAPLAYER
        private MFPlayer? mediaPlayer;
#endif

        /// <summary>
        /// Shows the splashscreen image specified in the application manifest.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static SimpleSplashScreen ShowDefaultSplashScreen()
        {
            var image = GetManifestSplashScreen();
            if (image is null)
                throw new InvalidOperationException("SplashScreen section not found in AppxManifest.xml");
            var manager = new Microsoft.Windows.ApplicationModel.Resources.ResourceManager();
            var context = manager.CreateResourceContext();

            uint dpi = 96;
            var monitor = PInvoke.MonitorFromPoint(new System.Drawing.Point(0, 0), Windows.Win32.Graphics.Gdi.MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
            
            if(monitor != IntPtr.Zero)
            {
                PInvoke.GetDpiForMonitor(monitor, Windows.Win32.UI.HiDpi.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out var dpiy);
                dpi = dpiX;
            }
            var scale = (int)(dpi / 96d * 100);
            if (scale == 0) scale = 100;
            context.QualifierValues["Scale"] = scale.ToString();
            var splashScreenImageResource = manager.MainResourceMap.TryGetValue("Files/" + image.Replace('\\','/'), context);
            if (splashScreenImageResource is not null && splashScreenImageResource.Kind == Microsoft.Windows.ApplicationModel.Resources.ResourceCandidateKind.FilePath)
            {
                return SimpleSplashScreen.ShowSplashScreenImage(splashScreenImageResource.ValueAsString);
            }
            throw new InvalidOperationException("Splash screen image not found in resources");
        }

        private static string? GetManifestSplashScreen()
        {
            var rootFolder = Windows.ApplicationModel.Package.Current?.InstalledLocation.Path ?? AppContext.BaseDirectory;
            var docPath = System.IO.Path.Combine(rootFolder, "AppxManifest.xml");
            if (!System.IO.File.Exists(docPath))
                throw new System.IO.FileNotFoundException("AppxManifest.xml not found");

            var doc = XDocument.Load(docPath, LoadOptions.None);
            var reader = doc.CreateReader();
            var namespaceManager = new System.Xml.XmlNamespaceManager(reader.NameTable);
            namespaceManager.AddNamespace("x", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
            namespaceManager.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10");

            var element = doc.Root?.XPathSelectElement("/x:Package/x:Applications/x:Application/uap:VisualElements/uap:SplashScreen", namespaceManager);
            return element?.Attribute(XName.Get("Image"))?.Value;
        }

        /// <summary>
        /// Shows a splash screen image at the center of the screen
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static SimpleSplashScreen ShowSplashScreenImage(string image)
        {
            var s = new SimpleSplashScreen();
            s.Initialize();
            var hBitmap = s.GetBitmap(image);
            s.DisplaySplash(Windows.Win32.Foundation.HWND.Null, hBitmap, null);
            return s;
        }
#if MEDIAPLAYER
        public static SimpleSplashScreen ShowSplashScreenVideo(string video)
        {
            var s = new SimpleSplashScreen();
            s.Initialize();
            s.DisplaySplash(Windows.Win32.Foundation.HWND.Null, Windows.Win32.Graphics.Gdi.HBITMAP.Null, video);
            return s;
        }
#endif
        private void Initialize()
        {
            var input = new Windows.Win32.Graphics.GdiPlus.GdiplusStartupInput()
            {
                GdiplusVersion = 1,
                SuppressBackgroundThread = false,
                SuppressExternalCodecs = false
            };
            var output = new Windows.Win32.Graphics.GdiPlus.GdiplusStartupOutput();
            var nStatus = PInvoke.GdiplusStartup(ref initToken, input, ref output);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            CleanUp();
            if (disposing)
            {
#if MEDIAPLAYER
                mediaPlayer.Dispose();
#endif
            }
        }

        private void CleanUp()
        {
            if (hWndSplash != IntPtr.Zero)
            {
                PInvoke.DestroyWindow(hWndSplash);
            }
            if (hBitmap != IntPtr.Zero)
            {
                PInvoke.DeleteObject(new Windows.Win32.Graphics.Gdi.HGDIOBJ(hBitmap));
                hBitmap = Windows.Win32.Graphics.Gdi.HGDIOBJ.Null;
            }
            PInvoke.GdiplusShutdown(initToken);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~SimpleSplashScreen() => Dispose(false);

        private unsafe void DisplaySplash(Windows.Win32.Foundation.HWND hWnd, Windows.Win32.Graphics.Gdi.HBITMAP bitmap, string? sVideo)
        {
            Windows.Win32.UI.WindowsAndMessaging.WNDCLASSEXW wcex;
            this.hBitmap = bitmap;
            wcex.cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEXW));
            wcex.style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW | WNDCLASS_STYLES.CS_DBLCLKS;
            wcex.hbrBackground = new Windows.Win32.Graphics.Gdi.HBRUSH(COLOR_BACKGROUND + 1);
            wcex.cbClsExtra = 0;
            wcex.cbWndExtra = 0;
            wcex.hInstance = new Windows.Win32.Foundation.HINSTANCE(Marshal.GetHINSTANCE(this.GetType().Module));
            wcex.hIcon = HICON.Null;
            wcex.hCursor = HCURSOR.Null;
            wcex.lpszMenuName = null;
            string sClassName = "Win32Class";
            fixed (char* name = sClassName)
                wcex.lpszClassName = name;
            wcex.lpfnWndProc = &Win32WndProc;

            wcex.hIconSm = HICON.Null;
            ushort nRet = PInvoke.RegisterClassEx(wcex);
            if (nRet == 0)
            {
                int nError = Marshal.GetLastWin32Error();
                if (nError != 1410) //0x582 ERROR_CLASS_ALREADY_EXISTS
                    return;
            }
            int nWidth = 0, nHeight = 0;
            if (hBitmap != IntPtr.Zero)
            {
                BITMAP bm;
                PInvoke.GetObject(new DeleteObjectSafeHandle(hBitmap), Marshal.SizeOf(typeof(BITMAP)), &bm);
                nWidth = bm.bmWidth;
                nHeight = bm.bmHeight;
            }
            
            hWndSplash = PInvoke.CreateWindowEx(WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_TRANSPARENT | WINDOW_EX_STYLE.WS_EX_TOPMOST,
                sClassName, "Win32 window", WINDOW_STYLE.WS_POPUP | WINDOW_STYLE.WS_VISIBLE, 400, 400, nWidth, nHeight, hWnd, null,
                new DestroyIconSafeHandle(wcex.hInstance), null);
            if (hBitmap != IntPtr.Zero)
            {
                CenterToScreen(hWndSplash);
                SetPictureToLayeredWindow(hWndSplash, hBitmap);
            }
#if MEDIAPLAYER
            if (sVideo != null)
            {
                mediaPlayer = new MFPlayer(this, hWndSplash, sVideo);
            }
#endif
        }
        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct BITMAP
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public short bmPlanes;
            public short bmBitsPixel;
            public IntPtr bmBits;
        }

#if MEDIAPLAYER
        private class MFPlayer : IMFPMediaPlayerCallback, IDisposable
        {
            private readonly SimpleSplashScreen m_ss;
            private readonly Windows.Win32.Foundation.HWND m_hWndParent;
            private readonly IMFPMediaPlayer m_pMediaPlayer;

            internal unsafe MFPlayer(SimpleSplashScreen ss, Windows.Win32.Foundation.HWND hWnd, string sVideo)
            {
                GlobalStructures.HRESULT hr = MFPlayTools.MFPCreateMediaPlayer(sVideo, false, MFPlay.MFPlayTools.MFP_CREATION_OPTIONS.MFP_OPTION_NONE, this, hWnd, out m_pMediaPlayer);
                m_hWndParent = hWnd;
                m_ss = ss;
            }

            void IMFPMediaPlayerCallback.OnMediaPlayerEvent(MFP_EVENT_HEADER pEventHeader)
            {
                switch (pEventHeader.eEventType)
                {
                    case MFP_EVENT_TYPE.MFP_EVENT_TYPE_MEDIAITEM_SET:
                        {
                            GlobalStructures.SIZE szVideo, szARVideo;
                            GlobalStructures.HRESULT hr = m_pMediaPlayer.GetNativeVideoSize(out szVideo, out szARVideo);
                            if (hr == GlobalStructures.HRESULT.S_OK)
                            {
                                PInvoke.SetWindowPos(m_hWndParent, Windows.Win32.Foundation.HWND.Null, 0, 0, szVideo.cx, szVideo.cy, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
                                m_ss.CenterToScreen(m_hWndParent);
                                hr = m_pMediaPlayer.Play();
                            }
                        }
                        break;
                    case MFP_EVENT_TYPE.MFP_EVENT_TYPE_PLAYBACK_ENDED:
                        {
                            PlaybackEnded?.Invoke(this, EventArgs.Empty);
                        }
                        break;
                }
                return;
            }

            ~MFPlayer() => Dispose(false);

            public void Dispose() => Dispose(true);

            private void Dispose(bool disposing) => m_pMediaPlayer.Shutdown();

            public event EventHandler PlaybackEnded;
        }
#endif

        /// <summary>
        /// Hides the splashscreen
        /// </summary>
        /// <param name="fadeTimout">Time spend fading out the splash screen (defaults to no fade)</param>
        public void Hide(TimeSpan fadeTimout = default)
        {
            if (fadeTimout.Ticks <= 0)
                CleanUp();
            else
            {
                dTimer = new DispatcherTimer();
                dTimer.Interval = TimeSpan.FromMilliseconds(16);
                tsFadeoutDuration = fadeTimout;
                tsFadeoutEnd = DateTime.UtcNow + tsFadeoutDuration;
                dTimer.Tick += FadeTimer_Tick;
                dTimer.Start();
            }
        }

        private unsafe Windows.Win32.Graphics.Gdi.HBITMAP GetBitmap(string sBitmapFile)
        {
            var bitmap = new Windows.Win32.Graphics.GdiPlus.GpBitmap();
            var pBitmap = &bitmap;

            var nStatus = PInvoke.GdipCreateBitmapFromFile(sBitmapFile, ref pBitmap);

            if (nStatus == Windows.Win32.Graphics.GdiPlus.Status.Ok)
            {
                var hBitmap = new Windows.Win32.Graphics.Gdi.HBITMAP();
                PInvoke.GdipCreateHBITMAPFromBitmap(pBitmap, &hBitmap, (uint)System.Drawing.ColorTranslator.ToWin32(System.Drawing.Color.FromArgb(0)));
                return hBitmap;
            }
            throw new InvalidOperationException("Failed to open bitmap: " + nStatus.ToString());
        }

        private void FadeTimer_Tick(object? sender, object e)
        {
            DateTime dtNow = DateTime.UtcNow;
            if (dtNow >= tsFadeoutEnd)
            {
                if (dTimer != null)
                {
                    dTimer.Stop();
                    dTimer = null;
                }
                CleanUp();
            }
            else
            {
                double nProgress = (tsFadeoutEnd - dtNow).TotalMilliseconds / tsFadeoutDuration.TotalMilliseconds;
                var bf = new Windows.Win32.Graphics.Gdi.BLENDFUNCTION()
                {
                    BlendOp = AC_SRC_OVER,
                    SourceConstantAlpha = (byte)(255 * nProgress),
                    AlphaFormat = AC_SRC_ALPHA
                };

                PInvoke.UpdateLayeredWindow(hWndSplash,
                    new Windows.Win32.Graphics.Gdi.HDC(0), null, null, new Windows.Win32.Graphics.Gdi.HDC(0), (System.Drawing.Point?)null, new Windows.Win32.Foundation.COLORREF(0), bf, UPDATE_LAYERED_WINDOW_FLAGS.ULW_ALPHA);
            }
        }

        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;

        private unsafe void SetPictureToLayeredWindow(Windows.Win32.Foundation.HWND hWnd, IntPtr hBitmap)
        {
            BITMAP bm;
            Windows.Win32.Graphics.Gdi.HBITMAP bitmap = new Windows.Win32.Graphics.Gdi.HBITMAP(hBitmap);
            PInvoke.GetObject(new Windows.Win32.DeleteObjectSafeHandle(hBitmap), Marshal.SizeOf(typeof(BITMAP)), &bm);
            System.Drawing.Size sizeBitmap = new System.Drawing.Size(bm.bmWidth, bm.bmHeight);

            var hDCScreen = PInvoke.GetDC(Windows.Win32.Foundation.HWND.Null);
            var hDCMem = PInvoke.CreateCompatibleDC(hDCScreen);
            using var hBitmapOld = PInvoke.SelectObject(hDCMem, new DestroyIconSafeHandle(hBitmap));

            var bf = new Windows.Win32.Graphics.Gdi.BLENDFUNCTION()
            {
                BlendOp = AC_SRC_OVER,
                SourceConstantAlpha = 255,
                AlphaFormat = AC_SRC_ALPHA
            };
            PInvoke.GetWindowRect(hWnd, out var rectWnd);

            System.Drawing.Point ptSrc = new System.Drawing.Point();
            System.Drawing.Point ptDest = new System.Drawing.Point(rectWnd.left, rectWnd.top);

            bool bRet = PInvoke.UpdateLayeredWindow(hWnd,
                hDCScreen, ptDest, new Windows.Win32.Foundation.SIZE(sizeBitmap.Width, sizeBitmap.Height),
                hDCMem, ptSrc, new Windows.Win32.Foundation.COLORREF(0), bf, UPDATE_LAYERED_WINDOW_FLAGS.ULW_ALPHA);

            PInvoke.ReleaseDC(Windows.Win32.Foundation.HWND.Null, hDCScreen);
        }

        private unsafe void CenterToScreen(IntPtr hWnd)
        {
            var rcWorkArea = new Windows.Win32.Foundation.RECT();
            PInvoke.SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETWORKAREA, 0, &rcWorkArea, 0);
            var h = new Windows.Win32.Foundation.HWND(hWnd);
            PInvoke.GetWindowRect(h, out Windows.Win32.Foundation.RECT rc);
            int nX = System.Convert.ToInt32((rcWorkArea.left + rcWorkArea.right) / (double)2 - (rc.right - rc.left) / (double)2);
            int nY = System.Convert.ToInt32((rcWorkArea.top + rcWorkArea.bottom) / (double)2 - (rc.bottom - rc.top) / (double)2);
            PInvoke.SetWindowPos(h, Windows.Win32.Foundation.HWND.Null, nX, nY, -1, -1, SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        }

        [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
        private static Windows.Win32.Foundation.LRESULT Win32WndProc(Windows.Win32.Foundation.HWND hwnd,
            uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam)
        {
            return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
        }
    }
}
