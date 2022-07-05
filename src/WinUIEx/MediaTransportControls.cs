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
    public class MediaTransportControls : Control
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
                ProgressSlider.ValueChanged += ProgressSlider_ValueChanged;
            }
            if (GetTemplateChild("PlayPauseButton") is ButtonBase button)
            {
                button.Click += (s, e) =>
                {
                    var _mediaPlayer = GetMediaPlayer();
                    if (_mediaPlayer?.CurrentState == Windows.Media.Playback.MediaPlayerState.Playing)
                        _mediaPlayer?.Pause();
                    else
                        _mediaPlayer?.Play();
                };
            }
            if (GetTemplateChild("VolumeMuteButton") is ButtonBase muteButton)
            {
                muteButton.Click += (s, e) =>
                {
                    var player = GetMediaPlayer();
                    if (player is not null)
                        player.IsMuted = !player.IsMuted;
                };
            }
            if (GetTemplateChild("StopButton") is ButtonBase stopButton)
            {
                stopButton.Click += (s, e) =>
                {
                    var player = GetMediaPlayer();
                    if (player is not null)
                    {
                        player.Pause();
                        player.Position = TimeSpan.Zero;
                    }
                };
                stopButton.Visibility = IsStopButtonVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            if (GetTemplateChild("ZoomButton") is ButtonBase zoomButton)
            {
                zoomButton.Click += (s, e) =>
                {
                    // TODO
                };
                zoomButton.Visibility = IsZoomButtonVisible ? Visibility.Visible : Visibility.Collapsed;
            }

            if (GetTemplateChild("SkipBackwardButton") is ButtonBase skipBackwardButton)
            {
                skipBackwardButton.Click += (s, e) =>
                {
                    var player = GetMediaPlayer();
                    if (player is not null)
                    {
                        // TODO
                    }
                };
                skipBackwardButton.Visibility = IsSkipBackwardButtonVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            
            VolumeSlider = GetTemplateChild("VolumeSlider") as Slider;
            if(VolumeSlider is not null)
            {
                var _mediaPlayer = GetMediaPlayer();
                if (_mediaPlayer != null)
                    VolumeSlider.Value = _mediaPlayer.Volume * 100;
                VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
            }

            VisualStateManager.GoToState(this, IsCompact ? "CompactMode" : "NormalMode", false);
            VisualStateManager.GoToState(this, GetMediaPlayer()?.IsMuted == true ? "MuteState" : "VolumeState", false);
            UpdatePlaybackState(false);
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
                    TimeElapsedElement.Text = FormatTimeSpan(sender.Position);
                }
                if (TimeRemainingElement != null)
                {
                    TimeRemainingElement.Text = FormatTimeSpan(sender.NaturalDuration - sender.Position);
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

        private static string FormatTimeSpan(TimeSpan ts)
        {
            if(ts.Hours >=1)
            {
                return ts.ToString("h\\:mm\\:ss");
            }
            return ts.ToString("m\\:ss");
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

        /// <summary>
        /// Gets or sets a value that indicates whether the controls are shown and hidden automatically.
        /// </summary>
        /// <value><c>true</c> if the controls are shown and hidden automatically; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        public bool ShowAndHideAutomatically
        {
            get { return (bool)GetValue(ShowAndHideAutomaticallyProperty); }
            set { SetValue(ShowAndHideAutomaticallyProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ShowAndHideAutomatically"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowAndHideAutomaticallyProperty =
            DependencyProperty.Register(nameof(ShowAndHideAutomatically), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value that indicates whether transport controls are shown on one row instead of two.
        /// </summary>
        /// <value><c>true</c> if the transport controls are shown in one row; <c>false</c> if the transport controls are shown in two rows. The default is <c>false</c>.</value>
        public bool IsCompact
        {
            get { return (bool)GetValue(IsCompactProperty); }
            set { SetValue(IsCompactProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsCompact"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCompactProperty =
            DependencyProperty.Register(nameof(IsCompact), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false, (s, e) => ((MediaTransportControls)s).OnIsCompactPropertyChanged()));

        private void OnIsCompactPropertyChanged() => VisualStateManager.GoToState(this, IsCompact ? "CompactMode" : "NormalMode", true);

        /// <summary>
        /// Gets or sets a value that indicates whether the stop button is shown.
        /// </summary>
        /// <value><c>true</c> to show the stop button. <c>false</c> to hide the stop button. The default is <c>false</c>.</value>
        public bool IsStopButtonVisible
        {
            get { return (bool)GetValue(IsStopButtonVisibleProperty); }
            set { SetValue(IsStopButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsStopButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsStopButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsStopButtonVisible), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false, (s,e) => ((MediaTransportControls)s).ToggleButtonVisibility("StopButton",(bool)e.NewValue)));

        /// <summary>
        /// Gets or sets a value that indicates whether a user can stop the media playback.
        /// </summary>
        /// <value><c>true</c> to allow the user to stop playback; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        public bool IsStopEnabled
        {
            get { return (bool)GetValue(IsStopEnabledProperty); }
            set { SetValue(IsStopEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsStopEnabled" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsStopEnabledProperty =
            DependencyProperty.Register(nameof(IsStopEnabled), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether a user can skip backward in the media.
        /// </summary>
        /// <value><c>true</c> to allow the user to skip backward; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        public bool IsSkipBackwardButtonVisible
        {
            get { return (bool)GetValue(IsSkipBackwardButtonVisibleProperty); }
            set { SetValue(IsSkipBackwardButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsSkipBackwardButtonVisible" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSkipBackwardButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsSkipBackwardButtonVisible), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false, (s, e) => ((MediaTransportControls)s).ToggleButtonVisibility("SkipBackwardButton", (bool)e.NewValue)));

        // TODO:
        //FastPlayFallbackBehaviour
        //IsCompactOverlayButtonVisible
        //IsCompactOverlayEnabled
        //IsFastForwardButtonVisible
        //IsFastForwardEnabled
        //IsFastRewindButtonVisible
        //IsFastRewindEnabled
        //IsFullWindowButtonVisible
        //IsFullWindowEnabled
        //IsNextTrackButtonVisible
        //IsPlaybackRateButtonVisible
        //IsPlaybackRateEnabled
        //IsPreviousTrackButtonVisible
        //IsRepeatButtonVisible
        //IsRepeatEnabled
        //IsSeekBarVisible
        //IsSeekEnabled
        //IsSkipBackwardEnabled
        //IsSkipForwardButtonVisible
        //IsSkipForwardEnabled
        //IsVolumeButtonVisible
        //IsVolumeEnabled

        /// <summary>
        /// Gets or sets a value that indicates whether a user can zoom the media.
        /// </summary>
        /// <value><c>true</c> to show the zoom button. <c>false</c> to hide the zoom button. The default is <c>true</c>.</value>
        public bool IsZoomButtonVisible
        {
            get { return (bool)GetValue(IsZoomButtonVisibleProperty); }
            set { SetValue(IsZoomButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsZoomButtonVisible" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsZoomButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsZoomButtonVisible), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(true, (s, e) => ((MediaTransportControls)s).ToggleButtonVisibility("ZoomButton", (bool)e.NewValue)));

        /// <summary>
        /// Gets or sets a value that indicates whether a user can zoom the media.
        /// </summary>
        /// <value><c>true</c> to allow the user to zoom; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        public bool IsZoomEnabled
        {
            get { return (bool)GetValue(IsZoomEnabledProperty); }
            set { SetValue(IsZoomEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsZoomEnabled" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsZoomEnabledProperty =
            DependencyProperty.Register(nameof(IsZoomEnabled), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(true));



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
