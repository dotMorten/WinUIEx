using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;
using WinRT;

namespace WinUIEx
{
    /// <summary>
    /// Represents an object that uses a <see cref="Windows.Media.Playback.MediaPlayer"/> to render audio and video to the display.
    /// </summary>
    public class MediaPlayerElement : Control
    {
        private SwapChainPanel? swapchainPanel;
        private MediaPlayer m_player;
        private Windows.Win32.Graphics.Dxgi.IDXGISwapChain1? m_swapchain;
        private Windows.Win32.Graphics.Direct3D11.ID3D11Device? m_d3dDevice;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaPlayerElement"/> class.
        /// </summary>
        public MediaPlayerElement()

        {
            DefaultStyleKey = typeof(MediaPlayerElement);
            _mediaTransportControls = new MediaTransportControls();
            SetMediaPlayer(new Windows.Media.Playback.MediaPlayer());
        }

        /// <summary>
        /// Sets the MediaPlayer instance used to render media.
        /// </summary>
        /// <param name="mediaPlayer">The new MediaPlayer instance used to render media.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <remarks>
        /// <para>You can use the SetMediaPlayer method to change the underlying <see cref="Windows.Media.Playback.MediaPlayer"/> instance. Calling this
        /// method to change the <see cref="Windows.Media.Playback.MediaPlayer"/> can cause non-trivial side effects because it can change other properties
        /// of the <see cref="MediaPlayerElement"/>.</para>
        /// <para>Use the <see cref="MediaPlayerElement.MediaPlayer"/> property to get the current instance of <see cref="Windows.Media.Playback.MediaPlayer"/>.</para>
        /// </remarks>
        public void SetMediaPlayer(MediaPlayer mediaPlayer)
        {
            if (mediaPlayer is null)
            {
                throw new ArgumentNullException(nameof(mediaPlayer));
            }
            if (m_player is not null)
            {
                m_player.VideoFrameAvailable -= MediaPlayer_VideoFrameAvailable;
                m_player.Dispose();
            }
            m_player = mediaPlayer;
            if (Source is not null)
                m_player.SetUriSource(Source);
            if (ActualHeight > 0 && ActualWidth > 0)
                m_player.SetSurfaceSize(new Windows.Foundation.Size(ActualWidth, ActualHeight));
            m_player.IsVideoFrameServerEnabled = true;
            m_player.VideoFrameAvailable += MediaPlayer_VideoFrameAvailable;
        }

        public MediaPlayer MediaPlayer => m_player;

        private unsafe void MediaPlayer_VideoFrameAvailable(MediaPlayer sender, object args)
        {
            if (m_swapchain is null)
                return;
            swapchainPanel?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                Guid g = IID_IDXGISurface;
                m_swapchain.GetBuffer(0, &g, out var surfaceobj);
                var surface = surfaceobj.As<Windows.Win32.Graphics.Dxgi.IDXGISurface>();
                Windows.Win32.PInvoke.CreateDirect3D11SurfaceFromDXGISurface(surface, out var isurface);
                var d3dSurface = WinRT.MarshalInterface<Windows.Graphics.DirectX.Direct3D11.IDirect3DSurface>.FromAbi(Marshal.GetIUnknownForObject(isurface));
                m_player.CopyFrameToVideoSurface(d3dSurface);
                Windows.Win32.Graphics.Dxgi.DXGI_PRESENT_PARAMETERS presentParam = new Windows.Win32.Graphics.Dxgi.DXGI_PRESENT_PARAMETERS(); // { 0, nullptr, nullptr, nullptr };
                presentParam.DirtyRectsCount = 0;
                m_swapchain.Present1(1, 0, &presentParam);
            });
        }

        private MediaTransportControls _mediaTransportControls;

        /// <summary>
        /// Gets or sets the transport controls for the media.
        /// </summary>
        /// <value>The transport controls for the media.</value>
        public MediaTransportControls TransportControls
        {
            get { return _mediaTransportControls; }
            set
            {
                _mediaTransportControls = value;
                if (GetTemplateChild("TransportControlsPresenter") is ContentPresenter presenter)
                {
                    presenter.Content = value;
                }
            }
        }

        private static readonly Guid IID_IDXGISurface = new Guid("cafcb56c-6ac3-4889-bf47-9e23bbd260ec");

        /// <summary>
        /// Gets or sets a media source on the <see cref="MediaPlayerElement"/>.
        /// </summary>
        /// <value>The source of the media. The default is null.</value>
        public Uri? Source
        {
            get { return (Uri?)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Source"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(Uri), typeof(MediaPlayerElement), new PropertyMetadata(null, (s,e) => ((MediaPlayerElement)s).OnSourcePropertyChanged(e)));

        private void OnSourcePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            m_player.SetUriSource(Source);
            if (AutoPlay)
                m_player.Play();
        }

        /// <summary>
        /// Gets or sets the image source that is used for a placeholder image during <see cref="MediaPlayerElement"/> loading transition states.
        /// </summary>
        /// <value>An image source for a transition <see cref="Microsoft.UI.Xaml.Media.ImageBrush"/> that is applied to the <see cref="MediaPlayerElement"/> content area.</value>
        /// //TODO: Remarks https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.mediaplayerelement.postersource?view=winrt-22621
        public Microsoft.UI.Xaml.Media.ImageSource PosterSource
        {
            get { return (Microsoft.UI.Xaml.Media.ImageSource)GetValue(PosterSourceProperty); }
            set { SetValue(PosterSourceProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PosterSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PosterSourceProperty =
            DependencyProperty.Register(nameof(PosterSource), typeof(Microsoft.UI.Xaml.Media.ImageSource), typeof(MediaPlayerElement), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets a value that determines whether the standard transport controls are enabled.
        /// </summary>
        /// <value>true if the standard transport controls are enabled; otherwise, false. The default is false.</value>
        public bool AreTransportControlsEnabled
        {
            get { return (bool)GetValue(AreTransportControlsEnabledProperty); }
            set { SetValue(AreTransportControlsEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AreTransportControlsEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AreTransportControlsEnabledProperty =
            DependencyProperty.Register(nameof(AreTransportControlsEnabled), typeof(bool), typeof(MediaPlayerElement), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether media will begin playback automatically when the <see cref="Source"/> property is set.
        /// </summary>
        /// <value><c>true</c> if playback is automatic; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        public bool AutoPlay
        {
            get { return (bool)GetValue(AutoPlayProperty); }
            set { SetValue(AutoPlayProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AutoPlay"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoPlayProperty =
            DependencyProperty.Register(nameof(AutoPlay), typeof(bool), typeof(MediaPlayerElement), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value that describes how an <see cref="MediaPlayerElement"/> should be stretched to fill the destination rectangle.
        /// </summary>
        /// <value>A value of the <see cref="Microsoft.UI.Xaml.Media.Stretch"/> enumeration that specifies how the source visual media is rendered. The default value is <c>Uniform</c>.</value>
        public Microsoft.UI.Xaml.Media.Stretch Stretch
        {
            get { return (Microsoft.UI.Xaml.Media.Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Stretch"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register(nameof(Stretch), typeof(Microsoft.UI.Xaml.Media.Stretch), typeof(MediaPlayer), new PropertyMetadata(Microsoft.UI.Xaml.Media.Stretch.Uniform));

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (swapchainPanel != null)
            {
                swapchainPanel.SizeChanged -= SwapchainPanel_SizeChanged;
                swapchainPanel = null;
            }
            swapchainPanel = GetTemplateChild("MediaPlayerPresenter") as SwapChainPanel;
            if (swapchainPanel != null)
            {
                swapchainPanel.SizeChanged += SwapchainPanel_SizeChanged;
                CreateSwapChain();
            }
            if (GetTemplateChild("TransportControlsPresenter") is ContentPresenter presenter)
            {
                presenter.Content = TransportControls;
            }
        }

        private void SwapchainPanel_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            CreateSwapChain();
            if (ActualHeight > 0 && ActualWidth > 0)
                m_player.SetSurfaceSize(new Windows.Foundation.Size(ActualWidth, ActualHeight));
        }
        ~MediaPlayerElement()
        {
            if (m_swapchain != null)
                Marshal.ReleaseComObject(m_swapchain);
            if (m_d3dDevice != null)
                Marshal.ReleaseComObject(m_d3dDevice);
        }
        private unsafe void CreateSwapChain()
        {
            if (swapchainPanel is null || swapchainPanel.ActualWidth == 0 || swapchainPanel.ActualHeight == 0)
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
            //Windows.Win32.Graphics.Direct3D11.ID3D11Device
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
                Width = (uint)Math.Max(1, swapchainPanel.ActualWidth),
                Height = (uint)Math.Max(1, swapchainPanel.ActualHeight),
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

            //var dxgiDevice = m_d3dDevice as Windows.Win32.Graphics.Dxgi.IDXGIDevice;
            var dxgiDevice = m_d3dDevice.As<Windows.Win32.Graphics.Dxgi.IDXGIDevice>();
            dxgiDevice.GetAdapter(out var dxgiAdapter);

            var g = IID_IDXGIFactory2_Guid;
            dxgiAdapter.GetParent(&g, out var parent);
            var dxgiFactory = (Windows.Win32.Graphics.Dxgi.IDXGIFactory2)parent;
            dxgiFactory.CreateSwapChainForComposition(m_d3dDevice, &swapChainDesc, null, out var swapchain);
            m_swapchain = swapchain;

            g = new Guid("cafcb56c-6ac3-4889-bf47-9e23bbd260ec"); // IID_IDXGISurface;
            m_swapchain.GetBuffer(0, &g, out var surfaceobj);
            //var iunknown = Marshal.GetIUnknownForObject(swapchainPanel);
            //IUnknown
            //var guid = new Guid("63aad0b8-7c24-40ff-85a8-640d944cc325");
            //var co = new ComObject(swapchainPanel);
            //var pn0 = co.QueryInterface<Windows.Win32.System.WinRT.ISwapChainPanelNative>(guid);
            //g = ISwapChainPanelNative_Guid;
            //Marshal.QueryInterface(iunknown, ref g, out IntPtr ppv);
            //var pna = Activator.CreateInstance(typeof(Windows.Win32.System.WinRT.ISwapChainPanelNative), ppv);
            //var pp = Marshal.GetObjectForIUnknown(ppv);
            //var pn = (ISwapChainPanelNative)pp;
            var panelNative = swapchainPanel.As<ISwapChainPanelNative>();
            panelNative.SetSwapChain(swapchain);
            
        }

        [Guid("63aad0b8-7c24-40ff-85a8-640d944cc325"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport()]
        internal interface ISwapChainPanelNative
        {
            void SetSwapChain(Windows.Win32.Graphics.Dxgi.IDXGISwapChain swapChain);
        }
        private static readonly Guid IID_IDXGIFactory2_Guid =new Guid("50c83a1c-e072-4c48-87b0-3630fa36a6d0");
        //private static readonly Guid ISwapChainPanelNative_Guid = new Guid("63aad0b8-7c24-40ff-85a8-640d944cc325");
    }
}
