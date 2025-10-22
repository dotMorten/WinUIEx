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
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUIExSample.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Home : Page
    {
        public Home()
        {
            this.InitializeComponent();
            LoadWebcam();
        }

        public MainWindow MainWindow => (MainWindow)((App)Application.Current).MainWindow!;

        Windows.Media.Capture.MediaCapture mediaCapture;
        private async void LoadWebcam()
        {
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            DeviceInformation cameraDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front) ?? allVideoDevices.FirstOrDefault();

            var frameSourceGroups = await Windows.Media.Capture.Frames.MediaFrameSourceGroup.FindAllAsync();
            var selectedFrameSourceGroup = frameSourceGroups.First();
            mediaCapture = new Windows.Media.Capture.MediaCapture();
            var settings = new MediaCaptureInitializationSettings
            {
                VideoDeviceId = cameraDevice.Id,
                SourceGroup = selectedFrameSourceGroup,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };
            await mediaCapture.InitializeAsync(settings);
            elm.Source = mediaCapture;
            //await mediaCapture.StartPreviewAsync();
        }

        private void OpenLogWindow_Click(object sender, RoutedEventArgs e) => MainWindow.ShowLogWindow();
    }
}
