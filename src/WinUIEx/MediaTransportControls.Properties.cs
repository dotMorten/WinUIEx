using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUIEx
{
    public partial class MediaTransportControls : Control
    {
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
        /// Gets or sets a value that specifies how the fast-forward/fast-rewind buttons behave.
        /// </summary>
        public FastPlayFallbackBehaviour FastPlayFallbackBehaviour
        {
            get { return (FastPlayFallbackBehaviour)GetValue(FastPlayFallbackBehaviourProperty); }
            set { SetValue(FastPlayFallbackBehaviourProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FastPlayFallbackBehaviour" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty FastPlayFallbackBehaviourProperty =
            DependencyProperty.Register(nameof(FastPlayFallbackBehaviour), typeof(FastPlayFallbackBehaviour), typeof(MediaTransportControls), new PropertyMetadata(FastPlayFallbackBehaviour.Skip));

        /// <summary>
        /// Gets or sets a value that indicates whether the compact overlay button is shown.
        /// </summary>
        /// <value><c>true</c> to show the compact overlay button. <c>false</c> to hide the compact overlay button. The default is <c>false</c>.</value>
        public bool IsCompactOverlayButtonVisible
        {
            get { return (bool)GetValue(IsCompactOverlayButtonVisibleProperty); }
            set { SetValue(IsCompactOverlayButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsCompactOverlayButtonVisible" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCompactOverlayButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsCompactOverlayButtonVisible), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether a user can enter compact overlay mode.
        /// </summary>
        /// <value><c>true</c> to allow the user to enter compact overlay mode; otherwise, <c>false</c>.</value>
        public bool IsCompactOverlayEnabled
        {
            get { return (bool)GetValue(IsCompactOverlayEnabledProperty); }
            set { SetValue(IsCompactOverlayEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsCompactOverlayEnabled" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCompactOverlayEnabledProperty =
            DependencyProperty.Register(nameof(IsCompactOverlayEnabled), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether the fast forward button is shown.
        /// </summary>
        /// <value><c>true</c> to show the fast forward button. <c>false</c> to hide the fast forward button. The default is <c>false</c>.</value>
        public bool IsFastForwardButtonVisible
        {
            get { return (bool)GetValue(IsFastForwardButtonVisibleProperty); }
            set { SetValue(IsFastForwardButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsFastForwardButtonVisible" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFastForwardButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsFastForwardButtonVisible), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether a user can fast forward the media.
        /// </summary>
        /// <value><c>true</c> to allow the user to fast forward; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        public bool IsFastForwardEnabled
        {
            get { return (bool)GetValue(IsFastForwardEnabledProperty); }
            set { SetValue(IsFastForwardEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsFastForwardEnabled" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFastForwardEnabledProperty =
            DependencyProperty.Register(nameof(IsFastForwardEnabled), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether the rewind button is shown.
        /// </summary>
        /// <value><c>true</c> to show the rewind button. <c>false</c> to hide the rewind button. The default is <c>false</c>.</value>
        public bool IsFastRewindButtonVisible
        {
            get { return (bool)GetValue(IsFastRewindButtonVisibleProperty); }
            set { SetValue(IsFastRewindButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsFastRewindButtonVisible" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFastRewindButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsFastRewindButtonVisible), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether the rewind button is shown.
        /// </summary>
        /// <value><c>true</c> to show the rewind button. <c>false</c> to hide the rewind button. The default is <c>false</c>.</value>
        public bool IsFastRewindEnabled
        {
            get { return (bool)GetValue(IsFastRewindEnabledProperty); }
            set { SetValue(IsFastRewindEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsFastRewindEnabled" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFastRewindEnabledProperty =
            DependencyProperty.Register(nameof(IsFastRewindEnabled), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether the full screen button is shown.
        /// </summary>
        /// <value><c>true</c> to show the full screen button. <c>false</c> to hide the full screen button. The default is <c>true</c>.</value>
        public bool IsFullWindowButtonVisible
        {
            get { return (bool)GetValue(IsFullWindowButtonVisibleProperty); }
            set { SetValue(IsFullWindowButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsFullWindowButtonVisible" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFullWindowButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsFullWindowButtonVisible), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value that indicates whether a user can play the media in full-screen mode.
        /// </summary>
        /// <value><c>true</c> to allow the user to play the media in full-screen mode; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        public bool IsFullWindowEnabled
        {
            get { return (bool)GetValue(IsFullWindowEnabledProperty); }
            set { SetValue(IsFullWindowEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsFullWindowEnabled" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFullWindowEnabledProperty =
            DependencyProperty.Register(nameof(IsFullWindowEnabled), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value that indicates whether the next track button is shown.
        /// </summary>
        /// <value><c>true</c> to show the next track button. <c>false</c> to hide the next track button. The default is <c>false</c>.</value>
        public bool IsNextTrackButtonVisible
        {
            get { return (bool)GetValue(IsNextTrackButtonVisibleProperty); }
            set { SetValue(IsNextTrackButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsNextTrackButtonVisible" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsNextTrackButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsNextTrackButtonVisible), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether the playback rate button is shown.
        /// </summary>
        /// <value><c>true</c> to show the playback rate button. <c>false</c> to hide the playback rate button. The default is <c>false</c>.</value>
        public bool IsPlaybackRateButtonVisible
        {
            get { return (bool)GetValue(IsPlaybackRateButtonVisibleProperty); }
            set { SetValue(IsPlaybackRateButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsPlaybackRateButtonVisible" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPlaybackRateButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsPlaybackRateButtonVisible), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether a user can adjust the playback rate of the media.
        /// </summary>
        /// <value><c>true</c> to allow the user to adjust the playback rate; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        public bool IsPlaybackRateEnabled
        {
            get { return (bool)GetValue(IsPlaybackRateEnabledProperty); }
            set { SetValue(IsPlaybackRateEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsPlaybackRateEnabled" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPlaybackRateEnabledProperty =
            DependencyProperty.Register(nameof(IsPlaybackRateEnabled), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether the previous track button is shown.
        /// </summary>
        /// <value><c>true</c> to show the previous track button. <c>false</c> to hide the previous track button. The default is <c>false</c>.</value>
        public bool IsPreviousTrackButtonVisible
        {
            get { return (bool)GetValue(IsPreviousTrackButtonVisibleProperty); }
            set { SetValue(IsPreviousTrackButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsPreviousTrackButtonVisible" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPreviousTrackButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsPreviousTrackButtonVisible), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether the repeat button is shown.
        /// </summary>
        /// <value><c>true</c> to show the repeat button. <c>false</c> to hide the repeat button. The default is <c>false</c>.</value>
        public bool IsRepeatButtonVisible
        {
            get { return (bool)GetValue(IsRepeatButtonVisibleProperty); }
            set { SetValue(IsRepeatButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsRepeatButtonVisible" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsRepeatButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsRepeatButtonVisible), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether a user repeat the playback of the media.
        /// </summary>
        /// <value><c>true</c> to allow the user to repeat the media; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        public bool IsRepeatEnabled
        {
            get { return (bool)GetValue(IsRepeatEnabledProperty); }
            set { SetValue(IsRepeatEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsRepeatEnabled" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsRepeatEnabledProperty =
            DependencyProperty.Register(nameof(IsRepeatEnabled), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value that indicates whether the seek bar is shown.
        /// </summary>
        /// <value><c>true</c> to show the seek bar. <c>false</c> to hide the seek bar. The default is <c>true</c>.</value>
        public bool IsSeekBarVisible
        {
            get { return (bool)GetValue(IsSeekBarVisibleProperty); }
            set { SetValue(IsSeekBarVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsSeekBarVisible" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSeekBarVisibleProperty =
            DependencyProperty.Register(nameof(IsSeekBarVisible), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value that indicates whether a user can use the seek bar to find a location in the media.
        /// </summary>
        /// <value><c>true</c> to allow the user to use the seek bar to find a location; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        public bool IsSeekEnabled
        {
            get { return (bool)GetValue(IsSeekEnabledProperty); }
            set { SetValue(IsSeekEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsSeekEnabled" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSeekEnabledProperty =
            DependencyProperty.Register(nameof(IsSeekEnabled), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(true));

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

        /// <summary>
        /// Gets or sets a value that indicates whether a user can skip backward in the media.
        /// </summary>
        /// <value><c>true</c> to allow the user to skip backward; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        public bool IsSkipBackwardEnabled
        {
            get { return (bool)GetValue(IsSkipBackwardEnabledProperty); }
            set { SetValue(IsSkipBackwardEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsSkipBackwardEnabled" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSkipBackwardEnabledProperty =
            DependencyProperty.Register(nameof(IsSkipBackwardEnabled), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether the skip forward button is shown.
        /// </summary>
        /// <value><c>true</c> to show the skip forward button. <c>false</c> to hide the skip forward button. The default is <c>false</c>.</value>
        public bool IsSkipForwardButtonVisible
        {
            get { return (bool)GetValue(IsSkipForwardButtonVisibleProperty); }
            set { SetValue(IsSkipForwardButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsSkipForwardButtonVisible" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSkipForwardButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsSkipForwardButtonVisible), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether a user can skip forward in the media.
        /// </summary>
        /// <value><c>true</c> to allow the user to skip forward; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        public bool IsSkipForwardEnabled
        {
            get { return (bool)GetValue(IsSkipForwardEnabledProperty); }
            set { SetValue(IsSkipForwardEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsSkipForwardEnabled" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSkipForwardEnabledProperty =
            DependencyProperty.Register(nameof(IsSkipForwardEnabled), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false));

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
            DependencyProperty.Register(nameof(IsStopButtonVisible), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(false, (s, e) => ((MediaTransportControls)s).ToggleButtonVisibility("StopButton", (bool)e.NewValue)));

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
        /// Gets or sets a value that indicates whether the volume button is shown.
        /// </summary>
        /// <value><c>true</c> to show the volume button. <c>false</c> to hide the volume button. The default is <c>true</c>.</value>
        public bool IsVolumeButtonVisible
        {
            get { return (bool)GetValue(IsVolumeButtonVisibleProperty); }
            set { SetValue(IsVolumeButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsVolumeButtonVisible" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsVolumeButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsVolumeButtonVisible), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value that indicates whether a user can adjust the volume of the media.
        /// </summary>
        /// <value><c>true</c> to allow the user to adjust the volume; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        public bool IsVolumeEnabled
        {
            get { return (bool)GetValue(IsVolumeEnabledProperty); }
            set { SetValue(IsVolumeEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsVolumeEnabled" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsVolumeEnabledProperty =
            DependencyProperty.Register(nameof(IsVolumeEnabled), typeof(bool), typeof(MediaTransportControls), new PropertyMetadata(true));

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
    }

    /// <summary>
    /// Defines constants that specify how MediaTransportControls fast-forward/backward buttons behave.
    /// </summary>
    public enum FastPlayFallbackBehaviour
    {
        /// <summary>
        /// If the media doesn't support fast-forward/fast-rewind, the media skips 30 seconds.
        /// </summary>
        Skip,
        /// <summary>
        /// If the media doesn't support fast-forward/fast-rewind, the buttons are hidden.
        /// </summary>
        Hide,
        /// <summary>
        /// If the media doesn't support fast-forward/fast-rewind, the buttons are disabled.
        /// </summary>
        Disable
    }
}
