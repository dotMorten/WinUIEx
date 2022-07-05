using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Playback;
using WinRT;

namespace WinUIEx
{
    /// <summary>
    /// Represents an object that displays a <see cref="Windows.Media.Playback.MediaPlayer"/>.
    /// </summary>
    public class MediaPlayerPresenter : Microsoft.UI.Xaml.Controls.Control
    {
        private static readonly Guid IID_IDXGIFactory2_Guid = new Guid("50c83a1c-e072-4c48-87b0-3630fa36a6d0");
        private static readonly Guid IID_IDXGISurface = new Guid("cafcb56c-6ac3-4889-bf47-9e23bbd260ec");
        private SwapChainPanel? swapchainPanel;
        private Windows.Win32.Graphics.Dxgi.IDXGISwapChain1? m_swapchain;
        private Windows.Win32.Graphics.Direct3D11.ID3D11Device? m_d3dDevice;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaPlayerPresenter"/> class.
        /// </summary>
        public MediaPlayerPresenter()
        {
            DefaultStyleKey = typeof(MediaPlayerPresenter);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~MediaPlayerPresenter()
        {
            if (m_swapchain != null)
                Marshal.ReleaseComObject(m_swapchain);
            if (m_d3dDevice != null)
                Marshal.ReleaseComObject(m_d3dDevice);
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (swapchainPanel != null)
            {
                swapchainPanel.SizeChanged -= SwapchainPanel_SizeChanged;
                swapchainPanel = null;
            }
            swapchainPanel = GetTemplateChild("MediaSwapChain") as SwapChainPanel;
            if (swapchainPanel != null)
            {
                swapchainPanel.SizeChanged += SwapchainPanel_SizeChanged;
                CreateSwapChain();
            }
        }

        private void SwapchainPanel_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            CreateSwapChain();
            if (ActualHeight > 0 && ActualWidth > 0 && swapchainPanel?.XamlRoot != null)
                MediaPlayer.SetSurfaceSize(new Windows.Foundation.Size(ActualWidth * swapchainPanel.XamlRoot.RasterizationScale, ActualHeight * swapchainPanel.XamlRoot.RasterizationScale));
        }

        private unsafe void MediaPlayer_VideoFrameAvailable(MediaPlayer sender, object args)
        {
            if (m_swapchain is null)
                return;
            swapchainPanel?.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                Guid g = IID_IDXGISurface;
                m_swapchain.GetBuffer(0, &g, out var surfaceobj);
                var surface = surfaceobj.As<Windows.Win32.Graphics.Dxgi.IDXGISurface>();
                Windows.Win32.PInvoke.CreateDirect3D11SurfaceFromDXGISurface(surface, out var isurface);
                var d3dSurface = WinRT.MarshalInterface<Windows.Graphics.DirectX.Direct3D11.IDirect3DSurface>.FromAbi(Marshal.GetIUnknownForObject(isurface));
                MediaPlayer.CopyFrameToVideoSurface(d3dSurface);
                Windows.Win32.Graphics.Dxgi.DXGI_PRESENT_PARAMETERS presentParam = new Windows.Win32.Graphics.Dxgi.DXGI_PRESENT_PARAMETERS(); // { 0, nullptr, nullptr, nullptr };
                presentParam.DirtyRectsCount = 0;
                m_swapchain.Present1(1, 0, &presentParam);
            });
        }

        private unsafe void CreateSwapChain()
        {
            if (swapchainPanel is null || swapchainPanel.XamlRoot is null || swapchainPanel.ActualWidth == 0 || swapchainPanel.ActualHeight == 0)
                return;
            if (m_swapchain != null)
                Marshal.ReleaseComObject(m_swapchain);
            if (m_d3dDevice != null)
                Marshal.ReleaseComObject(m_d3dDevice);
            var featureLevels = new ReadOnlySpan<Windows.Win32.Graphics.Direct3D11.D3D_FEATURE_LEVEL>(new Windows.Win32.Graphics.Direct3D11.D3D_FEATURE_LEVEL[] {
                Windows.Win32.Graphics.Direct3D11.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,
                Windows.Win32.Graphics.Direct3D11.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_1,
                Windows.Win32.Graphics.Direct3D11.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_0,
                Windows.Win32.Graphics.Direct3D11.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_3,
                Windows.Win32.Graphics.Direct3D11.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_2,
                Windows.Win32.Graphics.Direct3D11.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_1,
                });

            var flags = Windows.Win32.Graphics.Direct3D11.D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT;
#if DEBUG
            flags = flags | Windows.Win32.Graphics.Direct3D11.D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_DEBUG;
#endif
            var hresult = Windows.Win32.PInvoke.D3D11CreateDevice(
                null,
                Windows.Win32.Graphics.Direct3D11.D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE, null,
                flags,
                featureLevels, 7, out m_d3dDevice, null, out Windows.Win32.Graphics.Direct3D11.ID3D11DeviceContext context);

            var swapChainDesc = new Windows.Win32.Graphics.Dxgi.DXGI_SWAP_CHAIN_DESC1()
            {
                Width = (uint)Math.Max(1, swapchainPanel.ActualWidth * swapchainPanel.XamlRoot.RasterizationScale),
                Height = (uint)Math.Max(1, swapchainPanel.ActualHeight * swapchainPanel.XamlRoot.RasterizationScale),
                Format = Windows.Win32.Graphics.Dxgi.DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                Stereo = new Windows.Win32.Foundation.BOOL(false),
                SampleDesc = new Windows.Win32.Graphics.Dxgi.DXGI_SAMPLE_DESC()
                {
                    Count = 1,
                    Quality = 0
                },
                BufferUsage = 0x00000020, //DXGI_USAGE_RENDER_TARGET_OUTPUT
                BufferCount = 2,
                Scaling = 0, //DXGI_SCALING_STRETCH
                SwapEffect = Windows.Win32.Graphics.Dxgi.DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL,
                AlphaMode = Windows.Win32.Graphics.Dxgi.DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_PREMULTIPLIED,
                Flags = 0
            };

            var dxgiDevice = m_d3dDevice.As<Windows.Win32.Graphics.Dxgi.IDXGIDevice>();
            dxgiDevice.GetAdapter(out var dxgiAdapter);

            var g = IID_IDXGIFactory2_Guid;
            dxgiAdapter.GetParent(&g, out var parent);
            var dxgiFactory = (Windows.Win32.Graphics.Dxgi.IDXGIFactory2)parent;
            dxgiFactory.CreateSwapChainForComposition(m_d3dDevice, &swapChainDesc, null, out var swapchain);
            m_swapchain = swapchain;

            g = new Guid("cafcb56c-6ac3-4889-bf47-9e23bbd260ec"); // IID_IDXGISurface;
            m_swapchain.GetBuffer(0, &g, out var surfaceobj);
            var panelNative = swapchainPanel.As<ISwapChainPanelNative>();
            panelNative.SetSwapChain(swapchain);

        }

        /// <summary>
        /// Gets or sets a value that specifies if the <see cref="MediaPlayerPresenter"/> is rendering in full window mode.
        /// </summary>
        /// <value>true if the <see cref="MediaPlayerPresenter"/> is in full window mode; otherwise, false. The default is false.</value>
        public bool IsFullWindow
        {
            get { return (bool)GetValue(IsFullWindowProperty); }
            set { SetValue(IsFullWindowProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsFullWindow"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFullWindowProperty =
            DependencyProperty.Register(nameof(IsFullWindow), typeof(bool), typeof(MediaPlayerPresenter), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets the <see cref="Windows.Media.Playback.MediaPlayer"/> instance used to render media.
        /// </summary>
        /// <value>The <see cref="Windows.Media.Playback.MediaPlayer"/> instance used to render media.</value>
        public MediaPlayer MediaPlayer
        {
            get { return (MediaPlayer)GetValue(MediaPlayerProperty); }
            set { SetValue(MediaPlayerProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MediaPlayer"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MediaPlayerProperty =
            DependencyProperty.Register(nameof(MediaPlayer), typeof(MediaPlayer), typeof(MediaPlayerPresenter), new PropertyMetadata(null, (s,e) =>((MediaPlayerPresenter)s).OnMediaPlayerPropertyChanged(e)));

        private void OnMediaPlayerPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if(e.OldValue is MediaPlayer oldPlayer)
            {
                oldPlayer.VideoFrameAvailable -= MediaPlayer_VideoFrameAvailable;
                oldPlayer.Dispose();
            }
            if (e.NewValue is MediaPlayer newPlayer)
            {
                if (ActualHeight > 0 && ActualWidth > 0)
                    newPlayer.SetSurfaceSize(new Windows.Foundation.Size(ActualWidth, ActualHeight));
                newPlayer.IsVideoFrameServerEnabled = true;
                newPlayer.VideoFrameAvailable += MediaPlayer_VideoFrameAvailable;
            }
        }

        /// <summary>
        /// Gets or sets a value that describes how an <see cref="MediaPlayerPresenter"/> should be stretched to fill the destination rectangle.
        /// </summary>
        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Stretch"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(MediaPlayerPresenter), new PropertyMetadata(Stretch.Uniform));

        [Guid("63aad0b8-7c24-40ff-85a8-640d944cc325"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport()]
        internal interface ISwapChainPanelNative
        {
            void SetSwapChain(Windows.Win32.Graphics.Dxgi.IDXGISwapChain swapChain);
        }
    }
}
