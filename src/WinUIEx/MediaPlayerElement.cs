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
                oldPlayer.Dispose();
            }
            if (e.NewValue is MediaPlayer newPlayer)
            {
                if (Source is not null)
                    newPlayer.SetUriSource(Source);
                if (ActualHeight > 0 && ActualWidth > 0)
                    newPlayer.SetSurfaceSize(new Windows.Foundation.Size(ActualWidth, ActualHeight));
                newPlayer.IsVideoFrameServerEnabled = true;
                newPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
                newPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            }
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
            => TransportControls?.OnPositionChanged(sender);

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
            => TransportControls?.OnPlaybackStateChanged(sender);

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

        private MediaTransportControls? _mediaTransportControls;

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
        /// <value>true if the <see cref="MediaPlayerElement"/> is in full window mode; otherwise, false. The default is false.</value>
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

        private void OnSourcePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            MediaPlayer?.SetUriSource(Source);
            if (AutoPlay)
                MediaPlayer?.Play();
        }

        /// <summary>
        /// Gets or sets the image source that is used for a placeholder image during <see cref="MediaPlayerElement"/> loading transition states.
        /// </summary>
        /// <value>An image source for a transition <see cref="Microsoft.UI.Xaml.Media.ImageBrush"/> that is applied to the <see cref="MediaPlayerElement"/> content area.</value>
        /// //TODO: Remarks https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.mediaplayerelement.postersource?view=winrt-22621
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
        /// <value>true if the standard transport controls are enabled; otherwise, false. The default is false.</value>
        public bool AreTransportControlsEnabled
        {
            get { return (bool)GetValue(AreTransportControlsEnabledProperty); }
            set { SetValue(AreTransportControlsEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AreTransportControlsEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AreTransportControlsEnabledProperty =
            DependencyProperty.Register(nameof(AreTransportControlsEnabled), typeof(bool), typeof(MediaPlayerElement), new PropertyMetadata(false));

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
            DependencyProperty.Register(nameof(AutoPlay), typeof(bool), typeof(MediaPlayerElement), new PropertyMetadata(true));

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
            }
        }
    }
}
