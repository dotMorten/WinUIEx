#pragma once

#include "MediaPlayerElement.g.h"

namespace winrt::WinUIEx::MediaPlayer::implementation
{
    struct MediaPlayerElement : MediaPlayerElementT<MediaPlayerElement>
    {
        MediaPlayerElement();
        // Source DP
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
        // PosterSource DP
        Microsoft::UI::Xaml::Media::ImageSource PosterSource()
        {
            return winrt::unbox_value<Microsoft::UI::Xaml::Media::ImageSource>(GetValue(m_posterSourceProperty));
        }
        void PosterSource(Microsoft::UI::Xaml::Media::ImageSource const& value)
        {
            SetValue(m_posterSourceProperty, winrt::box_value(value));
        }
        static Microsoft::UI::Xaml::DependencyProperty PosterSourceProperty() { return m_posterSourceProperty; }
        // AutoPlay DP
        bool AutoPlay()
        {
            return winrt::unbox_value<bool>(GetValue(m_autoPlayProperty));
        }
        void AutoPlay(bool const& value)
        {
            SetValue(m_autoPlayProperty, winrt::box_value(value));
        }
        static Microsoft::UI::Xaml::DependencyProperty AutoPlayProperty() { return m_stretchProperty; }
        // Stretch DP
        Microsoft::UI::Xaml::Media::Stretch Stretch()
        {
            return winrt::unbox_value<Microsoft::UI::Xaml::Media::Stretch>(GetValue(m_stretchProperty));
        }
        void Stretch(Microsoft::UI::Xaml::Media::Stretch const& value)
        {
            SetValue(m_autoPlayProperty, winrt::box_value(value));
        }
        static Microsoft::UI::Xaml::DependencyProperty StretchProperty() { return m_stretchProperty; }

        //
        winrt::Windows::Media::Playback::MediaPlayer MediaPlayer();

        void OnApplyTemplate();

    private:
        Windows::Foundation::Uri m_source{ nullptr };
        static Microsoft::UI::Xaml::DependencyProperty m_sourceProperty;
        static Microsoft::UI::Xaml::DependencyProperty m_posterSourceProperty;
        static Microsoft::UI::Xaml::DependencyProperty m_autoPlayProperty;
        static Microsoft::UI::Xaml::DependencyProperty m_stretchProperty;
        void CreateSwapChain();
        winrt::Windows::Media::Playback::MediaPlayer m_player{ nullptr };
        winrt::com_ptr<ID3D11Device> m_d3dDevice;
        winrt::com_ptr<IDXGISwapChain1> m_swapchain;
        winrt::Microsoft::UI::Xaml::Controls::SwapChainPanel m_swapchainpanel { nullptr };
    };
}

namespace winrt::WinUIEx::MediaPlayer::factory_implementation
{
    struct MediaPlayerElement : MediaPlayerElementT<MediaPlayerElement, implementation::MediaPlayerElement>
    {
    };
}
