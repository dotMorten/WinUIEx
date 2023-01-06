using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.UI.WebUI;
using WinUIEx;

namespace WinUIExSample
{
    public sealed partial class CameraCaptureWindow : WindowEx
    {
        WinUIEx.CameraCaptureUI captureUI;
        public CameraCaptureWindow()
        {
            captureUI = new WinUIEx.CameraCaptureUI(this);
            this.InitializeComponent();
        }
        public CameraCaptureUIPhotoCaptureSettings PhotoSettings => captureUI.PhotoSettings;
        public CameraCaptureUIVideoCaptureSettings VideoSettings => captureUI.VideoSettings;
        
        public double CroppedAspectRatioWidth
        {
            get => PhotoSettings.CroppedAspectRatio.Width;
            set { PhotoSettings.CroppedAspectRatio = new Size(value, PhotoSettings.CroppedAspectRatio.Height); }
        }
        public double CroppedAspectRatioHeight
        {
            get => PhotoSettings.CroppedAspectRatio.Height;
            set { PhotoSettings.CroppedAspectRatio = new Size(PhotoSettings.CroppedAspectRatio.Width, value); }
        }
        public double CroppedSizeInPixelsWidth
        {
            get => PhotoSettings.CroppedSizeInPixels.Width;
            set { PhotoSettings.CroppedSizeInPixels = new Size(value, PhotoSettings.CroppedSizeInPixels.Height); }
        }
        public double CroppedSizeInPixelsHeight
        {
            get => PhotoSettings.CroppedSizeInPixels.Height;
            set { PhotoSettings.CroppedSizeInPixels = new Size(PhotoSettings.CroppedSizeInPixels.Width, value); }
        }

        public int PhotoFormatIndex
        {
            get => (int)PhotoSettings.Format;
            set => PhotoSettings.Format = (Windows.Media.Capture.CameraCaptureUIPhotoFormat)value;
        }

        public int PhotoResolutionIndex
        {
            get => (int)PhotoSettings.MaxResolution;
            set => PhotoSettings.MaxResolution = (Windows.Media.Capture.CameraCaptureUIMaxPhotoResolution)value;
        }

        public int VideoFormatIndex
        {
            get => (int)VideoSettings.Format;
            set => VideoSettings.Format = (Windows.Media.Capture.CameraCaptureUIVideoFormat)value;
        }

        public int VideoResolutionIndex
        {
            get => (int)VideoSettings.MaxResolution;
            set => VideoSettings.MaxResolution = (Windows.Media.Capture.CameraCaptureUIMaxVideoResolution)value;
        }


        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Windows.Media.Capture.CameraCaptureUIMode mode;
            switch(((Button)sender).Tag as string )
            {
                case "Photo": mode = Windows.Media.Capture.CameraCaptureUIMode.Photo; break;
                case "Video": mode = Windows.Media.Capture.CameraCaptureUIMode.Video; break;
                case "PhotoOrVideo":
                default: mode=Windows.Media.Capture.CameraCaptureUIMode.PhotoOrVideo; break;
            }
            try
            {
                var file = await captureUI.CaptureFileAsync(mode);
                if (file.Path.EndsWith(".mp4") || file.Path.EndsWith(".wmv"))
                {
                    capturedImage.Source = null;
                    capturedVideo.Source = MediaSource.CreateFromStorageFile(file); // MediaSource.CreateFromUri(new Uri("file://" + file.Path));

                }
                else
                {
                    capturedImage.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage() { UriSource = new Uri("file://" + file.Path) };
                    capturedVideo.Source = null;
                }
            }
            catch(OperationCanceledException) { }
            catch(System.Exception ex)
            {
                await ShowMessageDialogAsync(ex.Message, "Capture error");
            }
        }
    }
}
