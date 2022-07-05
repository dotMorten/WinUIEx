using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Playback;
using WinRT;

namespace WinUIEx
{
    /// <summary>
    /// Represents an object that uses a <see cref="Windows.Media.Playback.MediaPlayer"/> to render audio and video to the display.
    /// </summary>
    public class MediaPlayerElement : Control
    {
        private MediaTransportControls? _mediaTransportControls;
        private MediaPlayerPresenter? _playerPresenter;
        private UIElement? _posterImage;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaPlayerElement"/> class.
        /// </summary>
        public MediaPlayerElement()
        {
            DefaultStyleKey = typeof(MediaPlayerElement);
            SetMediaPlayer(new Windows.Media.Playback.MediaPlayer());
            TransportControls = new MediaTransportControls();
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => MediaPlayer?.Pause();

        /// <summary>
        /// Sets the MediaPlayer instance used to render media.
        /// </summary>
        /// <param name="mediaPlayer">The new MediaPlayer instance used to render media.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <remarks>
        /// <para>You can use the SetMediaPlayer method to change the underlying <see cref="Windows.Media.Playback.MediaPlayer"/> instance. Calling this
        /// method to change the <see cref="Windows.Media.Playback.MediaPlayer"/> can cause non-trivial side effects because it can change other properties
        /// of the <see cref="MediaPlayerElement"/>.</para>
        /// <para>Use the <see cref="MediaPlayerElement.MediaPlayer"/> property to get the current instance of <see cref="Windows.Media.Playback.MediaPlayer"/>.</para>
        /// </remarks>
        public void SetMediaPlayer(MediaPlayer mediaPlayer)
        {
            if (mediaPlayer is null)
            {
                throw new ArgumentNullException(nameof(mediaPlayer));
            }
            SetValue(MediaPlayerProperty, mediaPlayer);
        }
        private void OnMediaPlayerPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is MediaPlayer oldPlayer)
            {
                oldPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
                oldPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
                oldPlayer.VolumeChanged -= MediaPlayer_VolumeChanged;
                oldPlayer.IsMutedChanged -= MediaPlayer_IsMutedChanged;
                oldPlayer.MediaFailed -= MediaPlayer_MediaFailed;
            }
            if (e.NewValue is MediaPlayer newPlayer)
            {
                if (Source is not null)
                    newPlayer.SetUriSource(Source);
                newPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
                newPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
                newPlayer.VolumeChanged += MediaPlayer_VolumeChanged;
                newPlayer.IsMutedChanged += MediaPlayer_IsMutedChanged;
                newPlayer.MediaFailed += MediaPlayer_MediaFailed;
                newPlayer.AutoPlay = AutoPlay;
            }
        }

        private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            TransportControls?.OnMediaFailed(args);
        }

        private void MediaPlayer_VolumeChanged(MediaPlayer sender, object args)
        {   
            TransportControls?.OnVolumeChanged(sender.Volume);
        }

        private void MediaPlayer_IsMutedChanged(MediaPlayer sender, object args)
            => TransportControls?.OnMuteChanged(sender.IsMuted);

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
            => TransportControls?.OnPositionChanged(sender);

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            var state = sender.PlaybackState;
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                UpdatePosterVisibility();
            });
            TransportControls?.OnPlaybackStateChanged(sender);
        }

        private void UpdatePosterVisibility()
        {
            bool showPosterSource = false;
            if (Source is null)
                showPosterSource = true;
            else if (MediaPlayer?.PlaybackSession.NaturalVideoWidth == 0) // Audio only
                showPosterSource = true;
            //todo: Show poster if streaming to another device
            else
            {
                var state = MediaPlayer?.PlaybackSession.PlaybackState ?? MediaPlaybackState.None;
                switch (state)
                {
                    case MediaPlaybackState.Opening:
                    case MediaPlaybackState.None:
                        showPosterSource = true;
                        break;
                    default: break;

                }
            }
            if (_posterImage is not null)
                _posterImage.Visibility = showPosterSource ? Visibility.Visible : Visibility.Collapsed;
            if (_playerPresenter is not null)
                _playerPresenter.Visibility = showPosterSource ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Gets the MediaPlayer instance used to render media.
        /// </summary>
        /// <value>The <see cref="Windows.Media.Playback.MediaPlayer"/> instance used to render media.</value>
        /// <remarks>
        /// You can use the <see cref="SetMediaPlayer"/> method to change the underlying <see cref="Windows.Media.Playback.MediaPlayer"/>
        /// instance. Changing the <see cref="Windows.Media.Playback.MediaPlayer"/> can cause non-trivial side effects because it can change 
        /// other properties of the <see cref="MediaPlayerElement"/>.
        /// </remarks>
        public MediaPlayer? MediaPlayer
        {
            get { return (MediaPlayer)GetValue(MediaPlayerProperty); }
        }

        /// <summary>
        /// Identifies the <see cref="MediaPlayer"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MediaPlayerProperty =
            DependencyProperty.Register(nameof(MediaPlayer), typeof(MediaPlayer), typeof(MediaPlayerElement), new PropertyMetadata(null, (s, e) => ((MediaPlayerElement)s).OnMediaPlayerPropertyChanged(e)));

        /// <summary>
        /// Gets or sets the transport controls for the media.
        /// </summary>
        /// <value>The transport controls for the media.</value>
        public MediaTransportControls? TransportControls
        {
            get { return _mediaTransportControls; }
            set
            {
                _mediaTransportControls = value;
                if (GetTemplateChild("TransportControlsPresenter") is ContentPresenter presenter)
                {
                    presenter.Content = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that specifies if the <see cref="MediaPlayerElement"/> is rendering in full window mode.
        /// </summary>
        /// <value><c>true</c> if the <see cref="MediaPlayerElement"/> is in full window mode; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        public bool IsFullWindow
        {
            get { return (bool)GetValue(IsFullWindowProperty); }
            set { SetValue(IsFullWindowProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsFullWindow"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFullWindowProperty =
            DependencyProperty.Register(nameof(IsFullWindow), typeof(bool), typeof(MediaPlayerElement), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a media source on the <see cref="MediaPlayerElement"/>.
        /// </summary>
        /// <value>The source of the media. The default is null.</value>
        public Uri? Source
        {
            get { return (Uri?)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Source"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(Uri), typeof(MediaPlayerElement), new PropertyMetadata(null, (s,e) => ((MediaPlayerElement)s).OnSourcePropertyChanged(e)));

        private void OnSourcePropertyChanged(DependencyPropertyChangedEventArgs e) => MediaPlayer?.SetUriSource(Source);

        /// <summary>
        /// Gets or sets the image source that is used for a placeholder image during <see cref="MediaPlayerElement"/> loading transition states.
        /// </summary>
        /// <value>An image source for a transition <see cref="Microsoft.UI.Xaml.Media.ImageBrush"/> that is applied to the <see cref="MediaPlayerElement"/> content area.</value>
        /// <remarks>
        /// <para>A PosterSource is an image, such as a album cover or movie poster, that is displayed in place of video.
        /// It provides your <see cref="MediaPlayerElement"/> with a visual representation before the media is loaded,
        /// or when the media is audio only.</para>
        /// <para>The PosterSource is displayed in the following situations:
        /// <list type="bullet">
        ///   <item>When a valid source is not set. For example, Source is not set, Source is set to <c>Null</c>, or the source is
        ///   invalid (as is the case when a <see cref="MediaPlayer.MediaFailed"/> event fires).</item>
        ///   <item>While media is loading. For example, a valid source is set, but the <see cref="MediaPlayer.MediaOpened"/> event has not fired yet.</item>
        ///   <item>While media is streaming to another device.</item>
        ///   <item>When the media is audio only.</item>
        /// </list>
        /// </para>
        /// </remarks>
        public Microsoft.UI.Xaml.Media.ImageSource PosterSource
        {
            get { return (Microsoft.UI.Xaml.Media.ImageSource)GetValue(PosterSourceProperty); }
            set { SetValue(PosterSourceProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PosterSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PosterSourceProperty =
            DependencyProperty.Register(nameof(PosterSource), typeof(Microsoft.UI.Xaml.Media.ImageSource), typeof(MediaPlayerElement), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets a value that determines whether the standard transport controls are enabled.
        /// </summary>
        /// <value><c>true</c> if the standard transport controls are enabled; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        public bool AreTransportControlsEnabled
        {
            get { return (bool)GetValue(AreTransportControlsEnabledProperty); }
            set { SetValue(AreTransportControlsEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AreTransportControlsEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AreTransportControlsEnabledProperty =
            DependencyProperty.Register(nameof(AreTransportControlsEnabled), typeof(bool), typeof(MediaPlayerElement), 
                new PropertyMetadata(false, (s,e) => ((MediaPlayerElement)s).AreTransportControlsEnabledPropertyChanged()));

        private void AreTransportControlsEnabledPropertyChanged()
        {
            if (GetTemplateChild("TransportControlsPresenter") is UIElement element)
            {
                element.Visibility = AreTransportControlsEnabled ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether media will begin playback automatically when the <see cref="Source"/> property is set.
        /// </summary>
        /// <value><c>true</c> if playback is automatic; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        public bool AutoPlay
        {
            get { return (bool)GetValue(AutoPlayProperty); }
            set { SetValue(AutoPlayProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AutoPlay"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoPlayProperty =
            DependencyProperty.Register(nameof(AutoPlay), typeof(bool), typeof(MediaPlayerElement), new PropertyMetadata(true, (s,e) => ((MediaPlayerElement)s).OnAutoPlayPropertyChanged()));

        private void OnAutoPlayPropertyChanged()
        {
            if (MediaPlayer != null)
                MediaPlayer.AutoPlay = AutoPlay;
        }

        /// <summary>
        /// Gets or sets a value that describes how an <see cref="MediaPlayerElement"/> should be stretched to fill the destination rectangle.
        /// </summary>
        /// <value>A value of the <see cref="Microsoft.UI.Xaml.Media.Stretch"/> enumeration that specifies how the source visual media is rendered. The default value is <c>Uniform</c>.</value>
        public Microsoft.UI.Xaml.Media.Stretch Stretch
        {
            get { return (Microsoft.UI.Xaml.Media.Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Stretch"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register(nameof(Stretch), typeof(Microsoft.UI.Xaml.Media.Stretch), typeof(MediaPlayerElement), new PropertyMetadata(Microsoft.UI.Xaml.Media.Stretch.Uniform));

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (GetTemplateChild("TransportControlsPresenter") is ContentPresenter presenter)
            {
                presenter.Content = TransportControls;
                presenter.Visibility = AreTransportControlsEnabled ? Visibility.Visible : Visibility.Collapsed;
            }
            if (GetTemplateChild("MediaPlayerPresenter") is MediaPlayerPresenter playerPresenter)
            {
                _playerPresenter = playerPresenter;
            }
            if (GetTemplateChild("PosterImage") is UIElement posterImage)
            {
                _posterImage = posterImage;
                UpdatePosterVisibility();
            }
        }
    }
}
