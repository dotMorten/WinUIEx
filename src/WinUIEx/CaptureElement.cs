using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Win32.Foundation;
using Windows.Win32.Media.MediaFoundation;
using WinRT;

namespace WinUIEx
{
    /// <summary>
    /// Renders a stream from a capture device, such as a camera or webcam. CaptureElement is used in conjunction with the  <see cref="Windows.Media.Capture.MediaCapture"/> API, and must be hooked up in the code behind.
    /// </summary>
    /// <remarks>
    /// <para>CaptureElement is used in conjunction with the <see cref="Windows.Media.Capture.MediaCapture"/> API.
    /// For more info on how to use CaptureElement, see <see href="https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/capture-photos-and-video-with-mediacapture">Capture photos and video with MediaCapture</see>.</para>
    /// <para>Use the <see cref="MediaCapture"/> object to control the stream and set options on the capture device.The CaptureElement is the UI portion of the stream that is associated with the <see cref="MediaCapture"/>.</para>
    /// <para>You can use at most one CaptureElement to render a stream from a single capture device.</para>
    /// <note>
    /// If your app manually sets the size of the <see cref="CaptureElement"/> control, you must make sure that the dimensions of the control do not exceed the device's native display resolution.
    /// </note>
    /// </remarks>
    public partial class CaptureElement : Control
    {

        /// <summary>
        /// Initializes a new instance of the CaptureElement class.
        /// </summary>
        public CaptureElement()
        {
            DefaultStyleKey = typeof(CaptureElement);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~CaptureElement()
        {
            if (m_swapchain != null)
                Marshal.ReleaseComObject(m_swapchain);
            if (m_d3dDevice != null)
                Marshal.ReleaseComObject(m_d3dDevice);
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (swapchainPanel != null)
            {
                swapchainPanel.SizeChanged -= SwapchainPanel_SizeChanged;
                swapchainPanel = null;
            }
            swapchainPanel = GetTemplateChild("MediaSwapChain") as SwapChainPanel;
            if (swapchainPanel != null)
            {
                swapchainPanel.SizeChanged += SwapchainPanel_SizeChanged;
                CreateSwapChain();
            }
        }

        private void SwapchainPanel_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            CreateSwapChain();
            if (ActualHeight > 0 && ActualWidth > 0 && swapchainPanel?.XamlRoot != null)
            {
                //TODO
                //MediaPlayer.SetSurfaceSize(new Size(ActualWidth, ActualHeight));
            }
        }

        private void OnSourcePropertyChanged(MediaCapture? mediaCaptureOld, MediaCapture? mediaCaptureNew)
        {
            if (mediaCaptureNew is null)
                return;
            RegisterSink(mediaCaptureNew);
        }

        /// <summary>
        /// Gets or sets the source <see cref="MediaCapture"/> that this <see cref="CaptureElement"/> represents.
        /// </summary>
        /// <remarks>
        /// This property should not be set in XAML, because XAML represents initial state, and there is no good way to reference a MediaCapture through XAML resources. Initializing a MediaCapture is typically done by async operations or only when a capture is about to begin.
        /// <note type="important">
        /// You should always set the Source property to null when you are shutting down media capture in your app. For more information on properly cleaning up media capture resources,
        /// see <see href="https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/capture-photos-and-video-with-mediacapture">Capture photos and video with MediaCapture</see>.
        /// </note>
        /// </remarks>
        public MediaCapture Source
        {
            get { return (MediaCapture)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Source"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(MediaCapture), typeof(CaptureElement), new PropertyMetadata(null, (s,e) => ((CaptureElement)s).OnSourcePropertyChanged(e.OldValue as MediaCapture, e.NewValue as MediaCapture)));

        /// <summary>
        /// Gets or sets how content from Source is resized to fill its allocated space, as declared by the Height and Width properties of the CaptureElement.
        /// </summary>
        /// <value>A value of the enumeration.</value>
        public Stretch Stretch
        {
            get { return (Stretch)GetValue( StretchProperty); }
            set { SetValue( StretchProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Stretch"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty  StretchProperty =
            DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(CaptureElement), new PropertyMetadata(Stretch.UniformToFill));

    }
}
