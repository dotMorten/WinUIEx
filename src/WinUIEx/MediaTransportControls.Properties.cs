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
    }
}
