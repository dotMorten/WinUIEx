using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

// states

//AudioSelectionAvailablityStates
//  AudioSelectionAvailable
//  AudioSelectionUnavailable
//CCSelectionAvailablityStates
//  CCSelectionAvailable
//  CCSelectionUnavailable
//MediaTransportControlMode
//  NormalMode
//  CompactMode
//VolumeMuteStates
//  VolumeState
//  MuteState
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
                switch (sender.PlaybackState)
                {
                    case Windows.Media.Playback.MediaPlaybackState.Opening:
                        VisualStateManager.GoToState(this, "Loading", true); break;
                    case Windows.Media.Playback.MediaPlaybackState.Buffering:
                        VisualStateManager.GoToState(this, "Buffering", true); break;
                    case Windows.Media.Playback.MediaPlaybackState.Playing:
                        if (ShowAndHideAutomatically) _interactionTimer.Start();
                        VisualStateManager.GoToState(this, "Normal", true);
                        VisualStateManager.GoToState(this, "PlayState", true); break;
                    case Windows.Media.Playback.MediaPlaybackState.Paused:
                        VisualStateManager.GoToState(this, "Normal", true);
                        VisualStateManager.GoToState(this, "PauseState", true); break;
                    default:
                        VisualStateManager.GoToState(this, "Normal", true); break;
                }
            });
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

        private bool progressValueChanging;

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

        private static string FormatTimeSpan(TimeSpan ts)
        {
            if(ts.Hours >=1)
            {
                return ts.ToString("h\\:mm\\:ss");
            }
            return ts.ToString("m\\:ss");
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            TimeElapsedElement = GetTemplateChild("TimeElapsedElement") as TextBlock;
            TimeRemainingElement = GetTemplateChild("TimeRemainingElement") as TextBlock;
            ProgressSlider = GetTemplateChild("ProgressSlider") as Slider;
            if(ProgressSlider != null)
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
        /// Hides the transport controls if they're shown.
        /// </summary>
        public void Hide() => VisualStateManager.GoToState(this, "ControlPanelFadeOut", true);

        /// <summary>
        /// Shows the tranport controls if they're hidden.
        /// </summary>
        public void Show() => VisualStateManager.GoToState(this, "ControlPanelFadeIn", true);
    }
}
