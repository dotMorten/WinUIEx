using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Win32.Foundation;
using Windows.Win32.Media.MediaFoundation;
using WinRT;

namespace WinUIEx
{
    // Resources for reference:
    // https://www.codeproject.com/Tips/772038/Custom-Media-Sink-for-Use-with-Media-Foundation-To
    // https://stackoverflow.com/questions/44455881/live-streaming-using-mediacapture
    // https://github.com/Microsoft/Windows-universal-samples/tree/main/Samples/SimpleCommunication/common/MediaExtensions/Microsoft.Samples.SimpleCommunication
    // https://learn.microsoft.com/en-us/windows/win32/api/mfcaptureengine/nf-mfcaptureengine-imfcapturepreviewsink-setrendersurface
    // https://github.com/Microsoft/Windows-classic-samples/tree/main/Samples/Win7Samples/multimedia/mediafoundation/MFCaptureD3D
    // https://learn.microsoft.com/en-us/windows/win32/api/mfcaptureengine/nn-mfcaptureengine-imfcapturepreviewsink
    // https://learn.microsoft.com/en-us/windows/win32/api/mfidl/nn-mfidl-imfmediasink
    // https://github.com/nickluo/CameraCapture/
    // https://github.com/castorix/WinUI3_DirectShow_Capture
    // https://github.com/TripleSM/MediaFrameCapture/blob/main/WinUI3MediaFrameCapture/WinUI3MediaFrameCapture/MainWindow.xaml.cs  - Framereader, not sink based
    // https://github.com/mmaitre314/MediaCaptureWPF/blob/6b42e0c288c3ef290b47f1267b37ac7488f60483/MediaCaptureWPF/CapturePreview.cs#L41
    public partial class CaptureElement
    {
        private static readonly Guid IID_IDXGIFactory2_Guid = new Guid("50c83a1c-e072-4c48-87b0-3630fa36a6d0");
        private static readonly Guid IID_IDXGISurface = new Guid("cafcb56c-6ac3-4889-bf47-9e23bbd260ec");
        private SwapChainPanel? swapchainPanel;
        private Windows.Win32.Graphics.Dxgi.IDXGISwapChain1? m_swapchain;
        private Windows.Win32.Graphics.Direct3D11.ID3D11Device? m_d3dDevice;

        private async void RegisterSink(MediaCapture mediaCaptureNew)
        { 
            var frameSource = mediaCaptureNew.FrameSources?.FirstOrDefault().Value;

            //var src1 = frameSource.As<IMFCaptureEngine>();
            
            //var src = WinRT.MarshalInterface<IMFCaptureEngine>.FromAbi(Marshal.GetIUnknownForObject(frameSource));
            //Windows.Win32.PInvoke.MFCreateSourceReaderFromMediaSource()
            //var profile = MediaEncodingProfile.CreateFromStreamAsync(ms.AsRandomAccessStream());
            //mediaCaptureNew.PrepareLowLagRecordToCustomSinkAsync()
            //mediaCaptureNew.StartPreviewAsync
            if (frameSource != null)
            {
                var mediaFrameReader = await mediaCaptureNew.CreateFrameReaderAsync(frameSource, MediaEncodingSubtypes.Argb32);
                mediaFrameReader.FrameArrived += MediaFrameReader_FrameArrived;
                //await mediaFrameReader.StartAsync();
                
                var profile = new MediaEncodingProfile
                {
                    Audio = null,
                    Video = VideoEncodingProperties.CreateUncompressed(MediaEncodingSubtypes.Rgb32, 640, 480),
                    Container = null
                };
                //await mediaCaptureNew.StartPreviewToCustomSinkAsync(profile, new MySink());
            }
        }

        private unsafe void MediaFrameReader_FrameArrived(Windows.Media.Capture.Frames.MediaFrameReader sender, Windows.Media.Capture.Frames.MediaFrameArrivedEventArgs args)
        {
            if (m_swapchain is null)
                return;
            swapchainPanel?.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                Guid g = IID_IDXGISurface;
                m_swapchain.GetBuffer(0, &g, out var surfaceobj);
                var surface = surfaceobj.As<Windows.Win32.Graphics.Dxgi.IDXGISurface>();
                var hresult = Windows.Win32.PInvoke.CreateDirect3D11SurfaceFromDXGISurface(surface, out var isurface);
                var d3dSurface = WinRT.MarshalInterface<Windows.Graphics.DirectX.Direct3D11.IDirect3DSurface>.FromAbi(Marshal.GetIUnknownForObject(isurface));
                var frame = sender.TryAcquireLatestFrame();
                if (frame == null)
                {
                    return;
                }
                var surface2 = frame.VideoMediaFrame.FrameReference.VideoMediaFrame.Direct3DSurface;
                //MediaPlayer.CopyFrameToVideoSurface(d3dSurface); //TODO: Find something similar to this mediaplayer call
                Windows.Win32.Graphics.Dxgi.DXGI_PRESENT_PARAMETERS presentParam = new Windows.Win32.Graphics.Dxgi.DXGI_PRESENT_PARAMETERS();
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
            //This requires the DX SDK installed or it'll fail
            //flags = flags | Windows.Win32.Graphics.Direct3D11.D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_DEBUG;
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

        [Guid("63aad0b8-7c24-40ff-85a8-640d944cc325"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport()]
        internal interface ISwapChainPanelNative
        {
            void SetSwapChain(Windows.Win32.Graphics.Dxgi.IDXGISwapChain swapChain);
        }
    }
    internal class MySink : IMFCapturePreviewSink, Windows.Media.IMediaExtension
    {
        void IMFCapturePreviewSink.GetOutputMediaType(uint dwSinkStreamIndex, out IMFMediaType ppMediaType)
        {
            throw new NotImplementedException();
        }

        public unsafe void GetService(uint dwSinkStreamIndex, Guid* rguidService, Guid* riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppUnknown)
        {
            throw new NotImplementedException();
        }

        unsafe void IMFCapturePreviewSink.AddStream(uint dwSourceStreamIndex, IMFMediaType pMediaType, IMFAttributes pAttributes, uint* pdwSinkStreamIndex)
        {
            throw new NotImplementedException();
        }

        public void Prepare()
        {
            throw new NotImplementedException();
        }

        public void RemoveAllStreams()
        {
            throw new NotImplementedException();
        }

        void IMFCapturePreviewSink.SetRenderHandle(HANDLE handle)
        {
            throw new NotImplementedException();
        }

        public void SetRenderSurface([MarshalAs(UnmanagedType.IUnknown)] object pSurface)
        {
            throw new NotImplementedException();
        }

        public unsafe void UpdateVideo([Optional] MFVideoNormalizedRect* pSrc, [Optional] RECT* pDst, [Optional] uint* pBorderClr)
        {
            throw new NotImplementedException();
        }

        void IMFCapturePreviewSink.SetSampleCallback(uint dwStreamSinkIndex, IMFCaptureEngineOnSampleCallback pCallback)
        {
            throw new NotImplementedException();
        }

        public unsafe void GetMirrorState(BOOL* pfMirrorState)
        {
            throw new NotImplementedException();
        }

        void IMFCapturePreviewSink.SetMirrorState(BOOL fMirrorState)
        {
            throw new NotImplementedException();
        }

        public unsafe void GetRotation(uint dwStreamIndex, uint* pdwRotationValue)
        {
            throw new NotImplementedException();
        }

        public void SetRotation(uint dwStreamIndex, uint dwRotationValue)
        {
            throw new NotImplementedException();
        }

        void IMFCapturePreviewSink.SetCustomSink(IMFMediaSink pMediaSink)
        {
            throw new NotImplementedException();
        }

        void IMFCaptureSink.GetOutputMediaType(uint dwSinkStreamIndex, out IMFMediaType ppMediaType)
        {
            throw new NotImplementedException();
        }

        unsafe void IMFCaptureSink.AddStream(uint dwSourceStreamIndex, IMFMediaType pMediaType, IMFAttributes pAttributes, uint* pdwSinkStreamIndex)
        {
            throw new NotImplementedException();
        }

        public void SetProperties(IPropertySet configuration)
        {
            throw new NotImplementedException();
        }
    }
}
