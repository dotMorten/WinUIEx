#include "pch.h"
#include "MediaPlayerElement.h"
#if __has_include("MediaPlayerElement.g.cpp")
#include "MediaPlayerElement.g.cpp"
#endif

namespace winrt::WinUIEx::MediaPlayer::implementation
{
    MediaPlayerElement::MediaPlayerElement()
    {
        DefaultStyleKey(winrt::box_value(L"WinUIEx.MediaPlayer.MediaPlayerElement"));
        m_player = winrt::Windows::Media::Playback::MediaPlayer();
        m_player.SetSurfaceSize(Windows::Foundation::Size{ 640, 480 }); //TODO: Handle surface size changes
        m_player.IsVideoFrameServerEnabled(true);
        m_player.VideoFrameAvailable([this](auto&...) {
            if (!m_swapchain)
                return;
            winrt::com_ptr<IDXGISurface> surface;
            winrt::check_hresult(m_swapchain->GetBuffer(0, IID_IDXGISurface, surface.put_void()));
            winrt::com_ptr<::IInspectable> isurface{ nullptr };
            winrt::check_hresult(::CreateDirect3D11SurfaceFromDXGISurface(surface.get(), isurface.put()));
            auto d3dSurface = isurface.as<winrt::Windows::Graphics::DirectX::Direct3D11::IDirect3DSurface>();
            m_player.CopyFrameToVideoSurface(d3dSurface);
            DXGI_PRESENT_PARAMETERS presentParam = { 0, nullptr, nullptr, nullptr };
            winrt::check_hresult(m_swapchain->Present1(1, 0, &presentParam));
        });
    }

    // Dependency property initializers
    Microsoft::UI::Xaml::DependencyProperty MediaPlayerElement::m_sourceProperty =
        Microsoft::UI::Xaml::DependencyProperty::Register(
            L"Source",
            winrt::xaml_typename<Windows::Foundation::Uri>(),
            winrt::xaml_typename<WinUIEx::MediaPlayer::MediaPlayerElement>(),
            Microsoft::UI::Xaml::PropertyMetadata{ nullptr, Microsoft::UI::Xaml::PropertyChangedCallback{ &MediaPlayerElement::OnSourceChanged } }
    );
    Microsoft::UI::Xaml::DependencyProperty MediaPlayerElement::m_posterSourceProperty =
        Microsoft::UI::Xaml::DependencyProperty::Register(
            L"PosterSource",
            winrt::xaml_typename<Microsoft::UI::Xaml::Media::ImageSource>(),
            winrt::xaml_typename<WinUIEx::MediaPlayer::MediaPlayerElement>(),
            Microsoft::UI::Xaml::PropertyMetadata{ nullptr }
    );
    Microsoft::UI::Xaml::DependencyProperty MediaPlayerElement::m_autoPlayProperty =
        Microsoft::UI::Xaml::DependencyProperty::Register(
            L"AutoPlay",
            winrt::xaml_typename<Microsoft::UI::Xaml::Media::ImageSource>(),
            winrt::xaml_typename<WinUIEx::MediaPlayer::MediaPlayerElement>(),
            Microsoft::UI::Xaml::PropertyMetadata{  box_value(true) }
    );

    Microsoft::UI::Xaml::DependencyProperty MediaPlayerElement::m_stretchProperty =
        Microsoft::UI::Xaml::DependencyProperty::Register(
            L"Stretch",
            winrt::xaml_typename<Microsoft::UI::Xaml::Media::Stretch>(),
            winrt::xaml_typename<WinUIEx::MediaPlayer::MediaPlayerElement>(),
            Microsoft::UI::Xaml::PropertyMetadata{ box_value(Microsoft::UI::Xaml::Media::Stretch::Uniform) }
    );

    void MediaPlayerElement::OnApplyTemplate()
    {
        __super::OnApplyTemplate();
        if (auto panel{ GetTemplateChild(L"MediaPlayerPresenter").try_as<winrt::Microsoft::UI::Xaml::Controls::SwapChainPanel>() })
        {
            if (panel)
            {
                m_swapchainpanel = panel;
                panel.SizeChanged([this](auto&...) {
                    CreateSwapChain();
                });
                CreateSwapChain();
            }
        }
    };

    winrt::Windows::Media::Playback::MediaPlayer MediaPlayerElement::MediaPlayer()
    {
        return m_player;
    }

    void MediaPlayerElement::OnSourceChanged(Microsoft::UI::Xaml::DependencyObject const& d, Microsoft::UI::Xaml::DependencyPropertyChangedEventArgs const& /* e */)
    {
        if (WinUIEx::MediaPlayer::MediaPlayerElement theControl{ d.try_as<WinUIEx::MediaPlayer::MediaPlayerElement>() })
        {
            theControl.MediaPlayer().SetUriSource(theControl.Source());
            if (theControl.AutoPlay())
            {
                theControl.MediaPlayer().Play();
            }
        }
    }

    void MediaPlayerElement::CreateSwapChain()
    {
        const D3D_FEATURE_LEVEL featureLevels[] =
        {
            D3D_FEATURE_LEVEL_11_0,
            D3D_FEATURE_LEVEL_10_1,
            D3D_FEATURE_LEVEL_10_0,
            D3D_FEATURE_LEVEL_9_3,
            D3D_FEATURE_LEVEL_9_2,
            D3D_FEATURE_LEVEL_9_1,
        };

        unsigned int flags = D3D11_CREATE_DEVICE_BGRA_SUPPORT | D3D11_CREATE_DEVICE_DEBUG;

        winrt::check_hresult(D3D11CreateDevice(
            nullptr,
            D3D_DRIVER_TYPE_HARDWARE,
            nullptr,
            flags,
            featureLevels,
            ARRAYSIZE(featureLevels),
            D3D11_SDK_VERSION,
            m_d3dDevice.put(),
            nullptr,
            nullptr
        ));

        DXGI_SWAP_CHAIN_DESC1 swapChainDesc = { 0 };
        swapChainDesc.Width = (UINT)m_swapchainpanel.ActualWidth();
        if (swapChainDesc.Width == 0)
            swapChainDesc.Width = 1;
        swapChainDesc.Height = (UINT)m_swapchainpanel.ActualHeight();
        if (swapChainDesc.Height == 0)
            swapChainDesc.Height = 1;
        swapChainDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
        swapChainDesc.Stereo = false;
        swapChainDesc.SampleDesc.Count = 1;
        swapChainDesc.SampleDesc.Quality = 0;
        swapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
        swapChainDesc.BufferCount = 2;
            swapChainDesc.Scaling = DXGI_SCALING_STRETCH;
        swapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL;
        swapChainDesc.AlphaMode = DXGI_ALPHA_MODE_PREMULTIPLIED;
        swapChainDesc.Flags = 0;

        auto dxgiDevice = m_d3dDevice.as<IDXGIDevice>();
        winrt::com_ptr<IDXGIAdapter> dxgiAdapter;
        winrt::check_hresult(dxgiDevice->GetAdapter(dxgiAdapter.put()));
        winrt::com_ptr<IDXGIFactory2> dxgiFactory;
        winrt::check_hresult(dxgiAdapter->GetParent(IID_IDXGIFactory2, dxgiFactory.put_void()));

        winrt::check_hresult(dxgiFactory->CreateSwapChainForComposition(m_d3dDevice.get(), &swapChainDesc, nullptr, m_swapchain.put()));

        auto panelNative = m_swapchainpanel.as<ISwapChainPanelNative>();
        winrt::check_hresult(panelNative->SetSwapChain(m_swapchain.get()));
    }
}
