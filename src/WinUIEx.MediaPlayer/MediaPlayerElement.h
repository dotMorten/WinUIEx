#pragma once

#include "MediaPlayerElement.g.h"

namespace winrt::WinUIEx::MediaPlayer::implementation
{
    struct MediaPlayerElement : MediaPlayerElementT<MediaPlayerElement>
    {
        MediaPlayerElement();

        Windows::Foundation::Uri Source();
        void Source(Windows::Foundation::Uri value);
        void Play();
        void Pause();

    private:
        void CreateSwapChain();
        winrt::Windows::Media::Playback::MediaPlayer m_player{ nullptr };
        winrt::com_ptr<ID3D11Device> m_d3dDevice;
        winrt::com_ptr<IDXGISwapChain1> m_swapchain;
        winrt::Microsoft::UI::Xaml::Controls::SwapChainPanel m_swapchainpanel { nullptr };
        Windows::Foundation::Uri m_source { nullptr };
    };
}

namespace winrt::WinUIEx::MediaPlayer::factory_implementation
{
    struct MediaPlayerElement : MediaPlayerElementT<MediaPlayerElement, implementation::MediaPlayerElement>
    {
    };
}
