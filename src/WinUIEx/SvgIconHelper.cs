using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct2D;
using Windows.Win32.Graphics.Direct2D.Common;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Com;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinUIEx
{
    internal static unsafe class SvgIconHelper
    {
        internal static HICON CreateIconFromSvg(string filename, uint width, uint height)
        {
            if (filename is null)
                throw new ArgumentNullException(nameof(filename));
            if (width == 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height == 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            filename = IconPathHelper.ResolvePath(filename);
            byte[] svgBytes = File.ReadAllBytes(filename);
            D2D_SIZE_F svgDocumentSize = GetSvgDocumentSize(svgBytes, width, height);

            ID3D11Device* d3dDevice = null;
            ID3D11DeviceContext* immediateContext = null;
            IDXGIDevice* dxgiDevice = null;
            ID2D1Factory1* d2dFactory = null;
            ID2D1Device* d2dDevice = null;
            ID2D1DeviceContext* d2dContext = null;
            ID2D1DeviceContext5* d2dContext5 = null;
            ID3D11Texture2D* renderTexture = null;
            ID3D11Texture2D* stagingTexture = null;
            IDXGISurface* dxgiSurface = null;
            ID2D1Bitmap1* targetBitmap = null;
            ID2D1SvgDocument* svgDocument = null;
            IStream* svgStream = null;
            HBITMAP colorBitmap = HBITMAP.Null;
            HBITMAP maskBitmap = HBITMAP.Null;

            try
            {
                CreateD3DDevice(&d3dDevice, &immediateContext);

                Guid dxgiDeviceId = typeof(IDXGIDevice).GUID;
                void* dxgiDevicePointer = null;
                ThrowIfFailed(d3dDevice->QueryInterface(&dxgiDeviceId, &dxgiDevicePointer));
                dxgiDevice = (IDXGIDevice*)dxgiDevicePointer;

                Guid d2dFactoryId = typeof(ID2D1Factory1).GUID;
                void* d2dFactoryPointer = null;
                ThrowIfFailed(PInvoke.D2D1CreateFactory(
                    D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_SINGLE_THREADED,
                    &d2dFactoryId,
                    (D2D1_FACTORY_OPTIONS*)null,
                    &d2dFactoryPointer));
                d2dFactory = (ID2D1Factory1*)d2dFactoryPointer;

                d2dFactory->CreateDevice(dxgiDevice, &d2dDevice);
                d2dDevice->CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS.D2D1_DEVICE_CONTEXT_OPTIONS_NONE, &d2dContext);

                Guid d2dContext5Id = typeof(ID2D1DeviceContext5).GUID;
                void* d2dContext5Pointer = null;
                ThrowIfFailed(d2dContext->QueryInterface(&d2dContext5Id, &d2dContext5Pointer));
                d2dContext5 = (ID2D1DeviceContext5*)d2dContext5Pointer;

                D3D11_TEXTURE2D_DESC textureDescription = new D3D11_TEXTURE2D_DESC
                {
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                    SampleDesc = new DXGI_SAMPLE_DESC
                    {
                        Count = 1,
                        Quality = 0
                    },
                    Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                    BindFlags = D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET
                };
                d3dDevice->CreateTexture2D(&textureDescription, null, &renderTexture);

                Guid dxgiSurfaceId = typeof(IDXGISurface).GUID;
                void* dxgiSurfacePointer = null;
                ThrowIfFailed(renderTexture->QueryInterface(&dxgiSurfaceId, &dxgiSurfacePointer));
                dxgiSurface = (IDXGISurface*)dxgiSurfacePointer;

                D2D1_BITMAP_PROPERTIES1 bitmapProperties = new D2D1_BITMAP_PROPERTIES1
                {
                    pixelFormat = new D2D1_PIXEL_FORMAT
                    {
                        format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                        alphaMode = D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_PREMULTIPLIED
                    },
                    dpiX = 96,
                    dpiY = 96,
                    bitmapOptions = D2D1_BITMAP_OPTIONS.D2D1_BITMAP_OPTIONS_TARGET | D2D1_BITMAP_OPTIONS.D2D1_BITMAP_OPTIONS_CANNOT_DRAW
                };

                d2dContext5->CreateBitmapFromDxgiSurface(dxgiSurface, &bitmapProperties, &targetBitmap);
                d2dContext5->SetTarget((ID2D1Image*)targetBitmap);

                fixed (byte* pSvgBytes = svgBytes)
                {
                    ThrowIfFailed(CreateStreamOnHGlobal(nint.Zero, true, &svgStream));
                    ThrowIfFailed(svgStream->Write(pSvgBytes, (uint)svgBytes.Length, null));
                }
                svgStream->Seek(0, SeekOrigin.Begin, null);

                d2dContext5->CreateSvgDocument(svgStream, svgDocumentSize, &svgDocument);

                D2D1_COLOR_F clearColor = default;
                D2D_MATRIX_3X2_F transform = default;
                float* transformValues = (float*)&transform;
                transformValues[0] = width / svgDocumentSize.width;
                transformValues[3] = height / svgDocumentSize.height;

                D2D_MATRIX_3X2_F identity = default;
                float* identityValues = (float*)&identity;
                identityValues[0] = 1f;
                identityValues[3] = 1f;

                d2dContext5->BeginDraw();
                d2dContext5->SetTransform(&transform);
                d2dContext5->Clear(&clearColor);
                d2dContext5->DrawSvgDocument(svgDocument);
                d2dContext5->SetTransform(&identity);
                ThrowIfFailed(d2dContext5->EndDraw(null, null));

                textureDescription.Usage = D3D11_USAGE.D3D11_USAGE_STAGING;
                textureDescription.BindFlags = 0;
                textureDescription.CPUAccessFlags = D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ;
                d3dDevice->CreateTexture2D(&textureDescription, null, &stagingTexture);
                immediateContext->CopyResource((ID3D11Resource*)stagingTexture, (ID3D11Resource*)renderTexture);

                CreateIconBitmapsFromTexture(stagingTexture, immediateContext, width, height, width, height, out colorBitmap, out maskBitmap);

                ICONINFO iconInfo = new ICONINFO()
                {
                    fIcon = true,
                    hbmColor = colorBitmap,
                    hbmMask = maskBitmap
                };

                HICON handle = PInvoke.CreateIconIndirect(&iconInfo);
                ThrowIfInvalid(handle);
                return handle;
            }
            finally
            {
                if (!maskBitmap.IsNull)
                    PInvoke.DeleteObject(maskBitmap);
                if (!colorBitmap.IsNull)
                    PInvoke.DeleteObject(colorBitmap);
                if (svgDocument != null)
                    svgDocument->Release();
                if (targetBitmap != null)
                    targetBitmap->Release();
                if (dxgiSurface != null)
                    dxgiSurface->Release();
                if (stagingTexture != null)
                    stagingTexture->Release();
                if (renderTexture != null)
                    renderTexture->Release();
                if (svgStream != null)
                    svgStream->Release();
                if (d2dContext5 != null)
                    d2dContext5->Release();
                if (d2dContext != null)
                    d2dContext->Release();
                if (d2dDevice != null)
                    d2dDevice->Release();
                if (d2dFactory != null)
                    d2dFactory->Release();
                if (dxgiDevice != null)
                    dxgiDevice->Release();
                if (immediateContext != null)
                    immediateContext->Release();
                if (d3dDevice != null)
                    d3dDevice->Release();
            }
        }

        private static void ThrowIfInvalid(HICON handle)
        {
            if (handle.Value == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        private static void ThrowIfInvalid(HBITMAP handle)
        {
            if (handle.Value == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        private static void ThrowIfFailed(HRESULT hr)
        {
            if (hr.Value < 0)
                Marshal.ThrowExceptionForHR(hr.Value);
        }

        private static void CreateD3DDevice(ID3D11Device** d3dDevice, ID3D11DeviceContext** immediateContext)
        {
            D3D_FEATURE_LEVEL featureLevel;
            D3D_FEATURE_LEVEL[] featureLevels =
            [
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_1,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_0
            ];

            fixed (D3D_FEATURE_LEVEL* pFeatureLevels = featureLevels)
            {
                HRESULT hr = PInvoke.D3D11CreateDevice(
                    (IDXGIAdapter*)null,
                    D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
                    default(HMODULE),
                    D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
                    pFeatureLevels,
                    (uint)featureLevels.Length,
                    PInvoke.D3D11_SDK_VERSION,
                    d3dDevice,
                    &featureLevel,
                    immediateContext);

                if (hr.Value >= 0)
                    return;

                ThrowIfFailed(PInvoke.D3D11CreateDevice(
                    (IDXGIAdapter*)null,
                    D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_WARP,
                    default(HMODULE),
                    D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
                    pFeatureLevels,
                    (uint)featureLevels.Length,
                    PInvoke.D3D11_SDK_VERSION,
                    d3dDevice,
                    &featureLevel,
                    immediateContext));
            }
        }

        private static void CreateIconBitmapsFromTexture(ID3D11Texture2D* texture, ID3D11DeviceContext* deviceContext, uint sourceWidth, uint sourceHeight, uint targetWidth, uint targetHeight, out HBITMAP colorBitmap, out HBITMAP maskBitmap)
        {
            void* dibBits = null;
            colorBitmap = HBITMAP.Null;
            maskBitmap = HBITMAP.Null;
            D3D11_MAPPED_SUBRESOURCE mappedResource = default;

            try
            {
                deviceContext->Map((ID3D11Resource*)texture, 0, D3D11_MAP.D3D11_MAP_READ, 0, &mappedResource);

                BITMAPINFO bitmapInfo = new BITMAPINFO();
                bitmapInfo.bmiHeader.biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
                bitmapInfo.bmiHeader.biWidth = checked((int)targetWidth);
                bitmapInfo.bmiHeader.biHeight = checked((int)targetHeight);
                bitmapInfo.bmiHeader.biPlanes = 1;
                bitmapInfo.bmiHeader.biBitCount = 32;

                colorBitmap = PInvoke.CreateDIBSection(default, &bitmapInfo, DIB_USAGE.DIB_RGB_COLORS, &dibBits, HANDLE.Null, 0);
                ThrowIfInvalid(colorBitmap);

                nuint rowSize = targetWidth * 4u;
                int maskStride = checked((((int)targetWidth + 31) / 32) * 4);
                byte[] maskBytes = new byte[maskStride * checked((int)targetHeight)];

                for (uint y = 0; y < targetHeight; y++)
                {
                    uint sampleY = Math.Min((uint)((y * sourceHeight) / targetHeight), sourceHeight - 1);
                    byte* sourceRow = (byte*)mappedResource.pData + (sampleY * mappedResource.RowPitch);
                    uint destinationY = targetHeight - 1 - y;
                    byte* destinationRow = (byte*)dibBits + (destinationY * rowSize);
                    int maskRowOffset = checked((int)destinationY) * maskStride;

                    for (uint x = 0; x < targetWidth; x++)
                    {
                        uint sampleX = Math.Min((uint)((x * sourceWidth) / targetWidth), sourceWidth - 1);
                        byte* sourcePixel = sourceRow + (sampleX * 4);
                        byte* destinationPixel = destinationRow + (x * 4);

                        byte blue = sourcePixel[0];
                        byte green = sourcePixel[1];
                        byte red = sourcePixel[2];
                        byte alpha = sourcePixel[3];

                        if (alpha != 0 && alpha != 255)
                        {
                            blue = Unpremultiply(blue, alpha);
                            green = Unpremultiply(green, alpha);
                            red = Unpremultiply(red, alpha);
                        }
                        else if (alpha == 0)
                        {
                            blue = 0;
                            green = 0;
                            red = 0;
                        }

                        destinationPixel[0] = blue;
                        destinationPixel[1] = green;
                        destinationPixel[2] = red;
                        destinationPixel[3] = alpha;

                        if (alpha < 16)
                        {
                            int maskByteIndex = maskRowOffset + checked((int)(x / 8));
                            maskBytes[maskByteIndex] |= (byte)(0x80 >> checked((int)(x % 8)));
                        }
                    }
                }

                fixed (byte* pMaskBytes = maskBytes)
                {
                    maskBitmap = PInvoke.CreateBitmap(checked((int)targetWidth), checked((int)targetHeight), 1, 1, pMaskBytes);
                }
                ThrowIfInvalid(maskBitmap);
            }
            finally
            {
                if (mappedResource.pData != null)
                    deviceContext->Unmap((ID3D11Resource*)texture, 0);
            }
        }

        private static byte Unpremultiply(byte color, byte alpha)
        {
            uint value = (uint)((color * 255u) + (alpha / 2u)) / alpha;
            return (byte)Math.Min(value, 255u);
        }

        private static D2D_SIZE_F GetSvgDocumentSize(byte[] svgBytes, uint fallbackWidth, uint fallbackHeight)
        {
            using MemoryStream stream = new MemoryStream(svgBytes, writable: false);
            XDocument document = XDocument.Load(stream, LoadOptions.None);
            XElement root = document.Root ?? throw new InvalidOperationException("SVG document does not have a root element.");

            if (TryGetViewBoxSize(root, out float viewBoxWidth, out float viewBoxHeight))
            {
                return new D2D_SIZE_F { width = viewBoxWidth, height = viewBoxHeight };
            }

            if (TryParseSvgLength((string?)root.Attribute("width"), out float width) &&
                TryParseSvgLength((string?)root.Attribute("height"), out float height))
            {
                return new D2D_SIZE_F { width = width, height = height };
            }

            return new D2D_SIZE_F { width = fallbackWidth, height = fallbackHeight };
        }

        private static bool TryGetViewBoxSize(XElement root, out float width, out float height)
        {
            width = 0;
            height = 0;
            string? viewBox = (string?)root.Attribute("viewBox");
            if (string.IsNullOrWhiteSpace(viewBox))
                return false;

            string[] parts = viewBox.Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
                return false;

            return float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out width) &&
                   float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out height) &&
                   width > 0 &&
                   height > 0;
        }

        private static bool TryParseSvgLength(string? value, out float result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();
            if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                value = value[..^2];

            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) && result > 0;
        }

        [DllImport("ole32.dll", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern HRESULT CreateStreamOnHGlobal(nint hGlobal, [MarshalAs(UnmanagedType.Bool)] bool deleteOnRelease, IStream** stream);
    }
}
