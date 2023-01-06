using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.System;
using WinRT.Interop;

namespace WinUIEx
{
    /// <summary>
    /// Provides a full window UI for capturing audio, video, and photos from a camera. As well as
    /// controls for trimming video, time delayed capture, and camera settings.
    /// </summary>
    /// <remarks>
    /// <para>CameraCaptureUI provides a full window UI experience for capturing audio, video, and images. It
    /// provides controls for setting a time delay on photo captures, trimming video, and for adjusting the
    /// camera's settings such as video resolution, the audio device, brightness, and contrast.</para>
    /// <para>Call CaptureFileAsync to launch the UI.The user has control over when to start the capture.When
    /// the asynchronous CaptureFileAsync operation completes, a <see cref="StorageFile"/> object is returned.</para>
    /// </remarks>
    /// <example>
    /// <para>This code shows how to use the <see cref="CameraCaptureUI"/> class to take a picture.</para>
    /// <code lang="csharp">
    /// // Using Windows.Media.Capture.CameraCaptureUI API to capture a photo
    /// CameraCaptureUI dialog = new CameraCaptureUI(mainWindow);
    /// Size aspectRatio = new Size(16, 9);
    /// dialog.PhotoSettings.CroppedAspectRatio = aspectRatio;
    /// StorageFile file = await dialog.CaptureFileAsync(CameraCaptureUIMode.Photo);
    /// </code>
    /// </example>
    /// <seealso cref="https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/">Audio, video, and camera</seealso>
    /// <seealso cref="https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/capture-photos-and-video-with-cameracaptureui">Capture photos and video with Windows built-in camera UI</seealso>
    public sealed class CameraCaptureUI
    {
        private LauncherOptions _launcherOptions;

        /// <summary>
        /// Create a new CameraCaptureUI object.
        /// </summary>
        /// <param name="window">Parent window to associate with the capture UI</param>
        public CameraCaptureUI(Window window)
        {
            if (window is null)
                throw new ArgumentNullException(nameof(window));
            PhotoSettings = new CameraCaptureUIPhotoCaptureSettings();
            VideoSettings = new CameraCaptureUIVideoCaptureSettings();
            var handle = window.GetWindowHandle();

            _launcherOptions = new LauncherOptions()
            {
                TargetApplicationPackageFamilyName = "Microsoft.WindowsCamera_8wekyb3d8bbwe",
                TreatAsUntrusted = false,
                DisplayApplicationPicker = false
            };
            InitializeWithWindow.Initialize(_launcherOptions, handle);
        }

        /// <summary>
        /// Provides settings for capturing photos. The settings include aspect ratio, image size, format, resolution, and whether or not cropping is allowed by the user interface (UI).
        /// </summary>
        /// <remarks>For information on the available photo capture settings, see <see cref="CameraCaptureUIPhotoCaptureSettings"/>.</remarks>
        /// <value>An object containing settings for capturing photos.</value>
        /// <seealso cref="CameraCaptureUIPhotoCaptureSettings"/>
        public CameraCaptureUIPhotoCaptureSettings PhotoSettings { get; }
        /// <summary>
        /// Provides settings for capturing videos. The settings include format, maximum resolution, maximum duration, and whether or not to allow trimming.
        /// </summary>
        /// <remarks>For information on the available video capture settings, see <see cref="CameraCaptureUIVideoCaptureSettings"/>.</remarks>
        /// <value>An object that provides settings for capturing videos.</value>
        public CameraCaptureUIVideoCaptureSettings VideoSettings { get; }

        /// <summary>
        /// Launches the <see cref="CameraCaptureUI"/> user interface.
        /// </summary>
        /// <remarks>
        /// Call CaptureFileAsync to launch the UI. The user has control over when to start the capture. When the asynchronous operation completes, a <see cref="StorageFile"/> object is returned.
        /// </remarks>
        /// <param name="mode">Specifies whether the user interface that will be shown allows the user to capture a photo, capture a video, or capture both photos and videos.</param>
        /// <returns>When this operation completes, a <see cref="StorageFile"/> object is returned.</returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<StorageFile> CaptureFileAsync(CameraCaptureUIMode mode)
        {
            StorageFile? videoFile = null;
            StorageFile? photoFile = null;
            string? photoFileToken = null;
            string? videoFileToken = null;
            
            var set = new ValueSet();
            if (mode == CameraCaptureUIMode.Photo)
            {
                set.Add("MediaType", "photo");
            }
            else if (mode == CameraCaptureUIMode.Video)
            {
                set.Add("MediaType", "video");
            }
            else if (mode == CameraCaptureUIMode.PhotoOrVideo)
            {
                set.Add("MediaType", "photoOrVideo");
            }
            if (mode == CameraCaptureUIMode.Photo || mode == CameraCaptureUIMode.PhotoOrVideo)
            {
                var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + (PhotoSettings.Format == CameraCaptureUIPhotoFormat.Png ? ".png" : ((PhotoSettings.Format == CameraCaptureUIPhotoFormat.Jpeg ? ".jpg" : ".jxr"))));
                File.WriteAllBytes(path, new byte[] { });
                photoFile = await StorageFile.GetFileFromPathAsync(path);
                photoFileToken = Windows.ApplicationModel.DataTransfer.SharedStorageAccessManager.AddFile(photoFile);
                set.Add("PhotoFileToken", photoFileToken);
                set.Add("AllowCropping", PhotoSettings.AllowCropping);
                if (PhotoSettings.CroppedAspectRatio.Width > 0)
                    set.Add("PhotoCroppedARWidth", (int)PhotoSettings.CroppedAspectRatio.Width);
                if (PhotoSettings.CroppedAspectRatio.Height > 0)
                    set.Add("PhotoCroppedARHeight", (int)PhotoSettings.CroppedAspectRatio.Height);
                if (PhotoSettings.CroppedSizeInPixels.Width > 0)
                    set.Add("PhotoCropWidth", (int)PhotoSettings.CroppedSizeInPixels.Width);
                if (PhotoSettings.CroppedSizeInPixels.Height > 0)
                    set.Add("PhotoCropHeight", (int)PhotoSettings.CroppedSizeInPixels.Height);
                set.Add("PhotoFormat", (int)PhotoSettings.Format);
                set.Add("MaxResolution", (int)PhotoSettings.MaxResolution);
            }

            if (mode == CameraCaptureUIMode.Video || mode == CameraCaptureUIMode.PhotoOrVideo)
            {
                var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + "." + VideoSettings.Format.ToString().ToLowerInvariant());
                File.WriteAllBytes(path, new byte[] { });
                videoFile = await StorageFile.GetFileFromPathAsync(path);
                videoFileToken = Windows.ApplicationModel.DataTransfer.SharedStorageAccessManager.AddFile(videoFile);
                set.Add("VideoFileToken", videoFileToken);
                set.Add("AllowTrimming", VideoSettings.AllowTrimming);
                if (VideoSettings.MaxDurationInSeconds > 0)
                    set.Add("MaxDurationInSeconds", (int)VideoSettings.MaxDurationInSeconds);
                set.Add("MaxVideoResolution", (int)VideoSettings.MaxResolution);
                set.Add("VideoFormat", (int)VideoSettings.Format);
            }

            var uri = new Uri("microsoft.windows.camera.picker:");
            var result = await Launcher.LaunchUriForResultsAsync(uri, _launcherOptions, set);
            Windows.ApplicationModel.DataTransfer.SharedStorageAccessManager.RemoveFile(videoFileToken);
            Windows.ApplicationModel.DataTransfer.SharedStorageAccessManager.RemoveFile(photoFileToken);
            if (result.Status == LaunchUriStatus.Success)
            {
                if (result.Result is null)
                    throw new OperationCanceledException();
                string? status = result.Result["Status"]?.ToString();
                if(status == "Failed")
                {
                    string errorMessage = result.Result["ErrorMessage"]?.ToString() ?? "Unknown Error " + result.Result["ErrorCode"];
                    throw new InvalidOperationException(errorMessage);
                }
                string? selectedToken = result.Result.ContainsKey("SelectedTokens") ? result.Result["SelectedTokens"] as string : null;
                if(selectedToken == videoFileToken)
                {
                    _ = photoFile?.DeleteAsync();
                    return videoFile!;
                }
                else if (selectedToken == photoFileToken)
                {
                    _ = videoFile?.DeleteAsync();
                    return photoFile!;
                }
            }
            throw new InvalidOperationException(result.Status.ToString());
        }
    }
    /// <summary>
    /// Provides settings for capturing photos with <see cref="CameraCaptureUI"/>. The settings include aspect ratio, image size, format, resolution, and whether or not cropping is allowed by the user interface (UI).
    /// </summary>
    public sealed class CameraCaptureUIPhotoCaptureSettings
    {
        internal CameraCaptureUIPhotoCaptureSettings() { }

        /// <summary>
        ///  Determines whether photo cropping will be enabled in the user interface for capture a photo.
        /// </summary>
        /// <value><c>True</c>, if photo cropping will be enabled; otherwise, <c>false</c>.</value>
        public bool AllowCropping { get; set; }

        /// <summary>
        ///  The aspect ratio of the captured photo.
        /// </summary>
        /// <remarks>If a non-zero value is provided, the user interface will force the user to crop the photo to the specified aspect ratio.</remarks>
        /// <value>The aspect ratio of the captured photo. If zero, the aspect ratio is not enforced.</value>
        public Size CroppedAspectRatio { get; set; }

        /// <summary>
        /// The exact size in pixels of the captured photo.
        /// </summary>
        /// <remarks>
        /// <para>If size is provided, the user interface for cropping photos will force the user to crop the photo to the specified size.</para>
        /// <para>If a size is specified that is larger than any available resolution, then the captured photo will be scaled to a large enough size first.</para>
        /// <para>Setting this property requires that the <see cref="MaxResolution"/> property is set to <see cref="CameraCaptureUIMaxPhotoResolution.HighestAvailable"/> and the <see cref="AllowCropping"/> property is set to True.</para>
        /// </remarks>
        /// <value>The size of the captured photo.</value>
        public Size CroppedSizeInPixels { get; set; }

        /// <summary>
        /// Determines the format that captured photos will be stored in.
        /// </summary>
        /// <value>A value that indicates the format for captured photos.</value>
        public CameraCaptureUIPhotoFormat Format { get; set; }

        /// <summary>
        /// Determines the maximum resolution the user will be able to select.
        /// </summary>
        /// <value>A value that indicates the maximum resolution the user will be able to select.</value>
        public CameraCaptureUIMaxPhotoResolution MaxResolution { get; set; }
    }
    /// <summary>
    /// Provides settings for capturing videos. The settings include format, maximum resolution, maximum duration, and whether or not to allow trimming.
    /// </summary>
    public sealed class CameraCaptureUIVideoCaptureSettings
    {
        internal CameraCaptureUIVideoCaptureSettings() { }

        /// <summary>
        /// Determines whether or not the video trimming user interface will be enabled.
        /// </summary>
        /// <value><c>True</c>, if the user will be allowed to trim videos; otherwise, <c>false</c>.</value>
        public bool AllowTrimming { get; set; }

        /// <summary>
        /// Determines the format for storing captured videos.
        /// </summary>
        /// <value>A value indicating the format for storing captured videos.</value>
        public CameraCaptureUIVideoFormat Format { get; set; }

        /// <summary>
        /// Determines the maximum duration of a video.
        /// </summary>
        /// <value>The maximum duration of a video. If this property is set to zero, no maximum duration is enforced.</value>
        public float MaxDurationInSeconds { get; set; }

        /// <summary>
        /// Determines the maximum resolution that the user can select.
        /// </summary>
        /// <value>The maximum resolution that the user can select.</value>
        public CameraCaptureUIMaxVideoResolution MaxResolution { get; set; }

    }
}
