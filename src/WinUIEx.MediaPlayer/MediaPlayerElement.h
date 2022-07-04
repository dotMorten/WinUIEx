#pragma once

#include "MediaPlayerElement.g.h"

namespace winrt::WinUIEx::MediaPlayer::implementation
{
    struct MediaPlayerElement : MediaPlayerElementT<MediaPlayerElement>
    {
        MediaPlayerElement();

        Windows::Foundation::Uri Source()
        {
            return winrt::unbox_value<Windows::Foundation::Uri>(GetValue(m_sourceProperty));
        }
        void Source(Windows::Foundation::Uri const& value)
        {
            SetValue(m_sourceProperty, winrt::box_value(value));
        }
        static Microsoft::UI::Xaml::DependencyProperty SourceProperty() { return m_sourceProperty; }
        static void OnSourceChanged(Microsoft::UI::Xaml::DependencyObject const&, Microsoft::UI::Xaml::DependencyPropertyChangedEventArgs const&);

        winrt::Windows::Media::Playback::MediaPlayer MediaPlayer();
        bool AutoPlay();
        void AutoPlay(bool value);

        void OnApplyTemplate();

    private:
        Windows::Foundation::Uri m_source{ nullptr };
        static Microsoft::UI::Xaml::DependencyProperty m_sourceProperty;
        void CreateSwapChain();
        winrt::Windows::Media::Playback::MediaPlayer m_player{ nullptr };
        winrt::com_ptr<ID3D11Device> m_d3dDevice;
        winrt::com_ptr<IDXGISwapChain1> m_swapchain;
        winrt::Microsoft::UI::Xaml::Controls::SwapChainPanel m_swapchainpanel { nullptr };
        bool m_autoplay{ false };
    };
}

namespace winrt::WinUIEx::MediaPlayer::factory_implementation
{
    struct MediaPlayerElement : MediaPlayerElementT<MediaPlayerElement, implementation::MediaPlayerElement>
    {
    };
}
