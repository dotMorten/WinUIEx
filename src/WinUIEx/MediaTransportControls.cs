using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Playback;

// TODO States:
//
//AudioSelectionAvailablityStates
//  AudioSelectionAvailable
//  AudioSelectionUnavailable
//CCSelectionAvailablityStates
//  CCSelectionAvailable
//  CCSelectionUnavailable
//FullWindowStates
//  NonFullWindowState
//  FullWindowState
//RepeatStates
//  RepeatNoneState
//  RepeatOneState
//  RepeatAllState

namespace WinUIEx
{
    /// <summary>
    /// Represents the playback controls for a media player element.
    /// </summary>
    /// <remarks>
    /// <para>The media transport controls let users interact with their media by providing a default playback
    /// experience comprised of various buttons including play, pause, closed captions, and others. It has many
    /// properties to allow for easy customization of the UI and configuration of which buttons are visible or enabled.</para>
    /// <para>You can use the MediaTransportControls to make it easy for users to control their audio and video content.
    /// The MediaTransportControls class is intended to be used only in conjunction with a <see cref="MediaPlayerElement"/> control.
    /// It doesn't function as a stand-alone control. You access an instance of MediaTransportControls through the <see cref="MediaPlayerElement.TransportControls"/>
    /// property.</para>
    /// <image src="https://user-images.githubusercontent.com/1378165/177467868-5cabfd46-19dc-443c-921b-9fb91be47dba.png" />
    /// </remarks>
    /// <seealso href="https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.mediatransportcontrols"/>
    public partial class MediaTransportControls : Control
    {
        private TextBlock? TimeElapsedElement;
        private TextBlock? TimeRemainingElement;
        private Slider? ProgressSlider;
        private bool progressValueChanging;
        private Slider? VolumeSlider;
        private bool volumeValueChanging;

        private DispatcherTimer _interactionTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaTransportControls"/> class.
        /// </summary>
        public MediaTransportControls()
        {
            DefaultStyleKey = typeof(MediaTransportControls);
            _interactionTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(5) };
            _interactionTimer.Tick += InteractionTimer_Tick;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            TimeElapsedElement = GetTemplateChild("TimeElapsedElement") as TextBlock;
            TimeRemainingElement = GetTemplateChild("TimeRemainingElement") as TextBlock;
            ProgressSlider = GetTemplateChild("ProgressSlider") as Slider;
            if (ProgressSlider is not null)
            {
                ProgressSlider.Visibility = IsSeekBarVisible ? Visibility.Visible : Visibility.Collapsed;
                ProgressSlider.ValueChanged += ProgressSlider_ValueChanged;
            }
            VolumeSlider = GetTemplateChild("VolumeSlider") as Slider;
            if (VolumeSlider is not null)
            {
                var _mediaPlayer = GetMediaPlayer();
                if (_mediaPlayer != null)
                    VolumeSlider.Value = _mediaPlayer.Volume * 100;
                VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
            }
            // Configure button states and clicks. 
            // See https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.mediatransportcontrols?view=winrt-22621#hiding-showing-enabling-and-disabling-buttons 
            // for description on behavior of buttons
            InitializeButton("PlayPauseButton", true, (p) =>
            {
                if (p.CurrentState == MediaPlayerState.Playing)
                    p.Pause();
                else
                    p.Play();
            });
            InitializeButton("VolumeMuteButton", IsVolumeButtonVisible, null);
            InitializeButton("AudioMuteButton", true, (p) => { p.IsMuted = !p.IsMuted; });
            InitializeButton("StopButton", IsStopButtonVisible, (p) =>
                {
                        p.Pause();
                        p.Position = TimeSpan.Zero;
                });
            InitializeButton("CompactOverlayButton", IsCompactOverlayButtonVisible, (p) => { IsCompact = !IsCompact; });
            InitializeButton("SkipBackwardButton", IsSkipBackwardButtonVisible, (p) => p.Position = TimeSpan.FromSeconds(Math.Max(p.Position.TotalSeconds - 10, 0)));
            InitializeButton("SkipForwardButton", IsSkipForwardButtonVisible, (p) => p.Position = TimeSpan.FromSeconds(Math.Min(p.Position.TotalSeconds + 30, p.NaturalDuration.TotalSeconds)));
            // TODO:
            InitializeButton("ZoomButton", IsZoomButtonVisible, (p) => { });
            InitializeButton("FastForwardButton", IsFastForwardButtonVisible, (p) => { });
            InitializeButton("RewindButton", IsFastRewindEnabled, (p) => { });
            InitializeButton("NextTrackButton", IsNextTrackButtonVisible, (p) => { });
            InitializeButton("PreviousTrackButton", IsPreviousTrackButtonVisible, (p) => { });
            InitializeButton("PlaybackRateButton", IsPlaybackRateButtonVisible, (p) => { });
            InitializeButton("FullWindowButton", IsFullWindowButtonVisible, (p) => { });
            InitializeButton("RepeatButton", IsRepeatButtonVisible, (p) =>
            {
                p.IsLoopingEnabled = !p.IsLoopingEnabled;
                VisualStateManager.GoToState(this, p.IsLoopingEnabled ? "RepeatAllState" : "RepeatNoneState", true);
            });

            var player = GetMediaPlayer();
            VisualStateManager.GoToState(this, IsCompact ? "CompactMode" : "NormalMode", false);
            VisualStateManager.GoToState(this, player?.IsMuted == true ? "MuteState" : "VolumeState", false);
            VisualStateManager.GoToState(this, player?.IsLoopingEnabled == true ? "RepeatAllState" : "RepeatNoneState", false);
            UpdatePlaybackState(false);
        }

        private void InitializeButton(string elementName, bool isVisible, Action<MediaPlayer>? onClick)
        {
            if (GetTemplateChild(elementName) is UIElement element)
            {
                element.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                if (onClick != null && element is ButtonBase button)
                    button.Click += (s, e) =>
                    {
                        var player = GetMediaPlayer();
                        if (player != null)
                            onClick(player);
                    };
            }
        }
        private Windows.Media.Playback.MediaPlayer? GetMediaPlayer()
        {
            var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(this);
            while (parent is not null)
            {
                if (parent is MediaPlayerElement elm)
                {
                    return elm.MediaPlayer;
                }
                parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        internal void OnPlaybackStateChanged(MediaPlaybackSession sender)
        {
            DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                UpdatePlaybackState(true);
            });
        }
        private void UpdatePlaybackState(bool useTransitions)
        {
            switch (GetMediaPlayer()?.PlaybackSession.PlaybackState)
            {
                case Windows.Media.Playback.MediaPlaybackState.Opening:
                    VisualStateManager.GoToState(this, "Loading", useTransitions); break;
                case Windows.Media.Playback.MediaPlaybackState.Buffering:
                    VisualStateManager.GoToState(this, "Buffering", useTransitions); break;
                case Windows.Media.Playback.MediaPlaybackState.Playing:
                    if (ShowAndHideAutomatically) _interactionTimer.Start();
                    VisualStateManager.GoToState(this, "Normal", useTransitions);
                    VisualStateManager.GoToState(this, "PlayState", useTransitions); break;
                case Windows.Media.Playback.MediaPlaybackState.Paused:
                    VisualStateManager.GoToState(this, "Normal", useTransitions);
                    VisualStateManager.GoToState(this, "PauseState", useTransitions); break;
                default:
                    VisualStateManager.GoToState(this, "Normal", useTransitions); break;
            }
        }

        internal void OnPositionChanged(MediaPlaybackSession sender)
        {
            DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                if (TimeElapsedElement != null)
                {
                    TimeElapsedElement.Text = sender.Position.ToString("h\\:mm\\:ss");
                }
                if (TimeRemainingElement != null)
                {
                    TimeRemainingElement.Text = (sender.NaturalDuration - sender.Position).ToString("h\\:mm\\:ss");
                }
                if (ProgressSlider != null)
                {
                    progressValueChanging = true;
                    if (sender.NaturalDuration == TimeSpan.Zero)
                        ProgressSlider.Value = ProgressSlider.Minimum;
                    else
                        ProgressSlider.Value = (ProgressSlider.Maximum - ProgressSlider.Minimum) * (sender.Position / sender.NaturalDuration) + ProgressSlider.Minimum;
                    progressValueChanging = false;
                }
            });
        }

        internal void OnVolumeChanged(double volume)
        {
            DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                if (VolumeSlider != null)
                {
                    volumeValueChanging = true;
                    VolumeSlider.Value = volume * 100;
                    volumeValueChanging = false;
                }
            });
        }

        internal void OnMuteChanged(bool muted)
        {
            DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                VisualStateManager.GoToState(this, muted ? "MuteState" : "VolumeState", true);
            });
        }

        internal void OnMediaFailed(MediaPlayerFailedEventArgs args)
        {
            DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                VisualStateManager.GoToState(this, "Error", true);
                if(GetTemplateChild("ErrorTextBlock") is TextBlock ErrorTextBlock)
                {
                    string msg = args.ErrorMessage;
                    if(string.IsNullOrEmpty(msg))
                    {
                        msg = args.Error switch
                        {
                             MediaPlayerError.NetworkError => "Network error",
                             MediaPlayerError.Aborted => "Aborted",
                             MediaPlayerError.DecodingError=> "Decoding error",
                             MediaPlayerError.SourceNotSupported => "Source not supported",
                            _ => "Unknown error"
                        };
                    }
                    ErrorTextBlock.Text = msg;
                }
            });
        }

        private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (progressValueChanging || ProgressSlider is null) return;
            var fraction = (e.NewValue - ProgressSlider.Minimum) / (ProgressSlider.Maximum - ProgressSlider.Minimum);
            var mp = GetMediaPlayer();
            if (mp != null && mp.PlaybackSession.CanSeek && mp.PlaybackSession.NaturalDuration > TimeSpan.Zero)
            {
                mp.PlaybackSession.Position = mp.PlaybackSession.NaturalDuration* fraction;
            }
        }


        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (volumeValueChanging || VolumeSlider is null) return;
            var mp = GetMediaPlayer();
            if (mp != null)
            {
                mp.Volume = e.NewValue / 100;
            }
        }

        private void InteractionTimer_Tick(object? sender, object e)
        {
            if(ShowAndHideAutomatically)
                Hide();
            _interactionTimer.Stop();
        }

        /// <inheritdoc />
        protected override void OnPointerMoved(PointerRoutedEventArgs e)
        {
            if (ShowAndHideAutomatically)
            {
                Show();
                _interactionTimer.Stop();
                _interactionTimer.Start();
            }
            base.OnPointerMoved(e);
        }


        private void ToggleButtonVisibility(string elementName, bool isVisible)
        {
            if (GetTemplateChild(elementName) is UIElement element)
            {
                element.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Hides the transport controls if they're shown.
        /// </summary>
        public void Hide() => VisualStateManager.GoToState(this, "ControlPanelFadeOut", true);

        /// <summary>
        /// Shows the tranport controls if they're hidden.
        /// </summary>
        public void Show() => VisualStateManager.GoToState(this, "ControlPanelFadeIn", true);

    }
}
