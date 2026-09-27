using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace WinUIExSample.CubeAnimation;

public sealed partial class CubeScene : UserControl
{
    private readonly object _renderGate = new();
    private CanvasAnimatedControl? _canvas;
    private CubeRenderer? _renderer;
    private CubeInteraction _interaction = new();
    private long _start;
    private uint? _capturedPointer;

    public CubeScene()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        if (_canvas is not null) return;
        Error.IsOpen = false;
        _interaction = new CubeInteraction();
        _start = Stopwatch.GetTimestamp();
        var canvas = new CanvasAnimatedControl { ManipulationMode = ManipulationModes.None };
        AutomationProperties.SetAutomationId(canvas, "HomeCubeAnimation");
        AutomationProperties.SetName(canvas, "Home cube animation");
        canvas.CreateResources += CreateResources;
        canvas.Draw += Draw;
        canvas.PointerPressed += Canvas_PointerPressed;
        canvas.PointerMoved += Canvas_PointerMoved;
        canvas.PointerReleased += Canvas_PointerReleased;
        canvas.PointerCanceled += PointerCancelled;
        canvas.PointerCaptureLost += PointerCancelled;
        _canvas = canvas;
        Host.Children.Insert(0, canvas);
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        CanvasAnimatedControl? canvas = _canvas;
        if (canvas is null) return;
        _canvas = null;
        _capturedPointer = null;
        canvas.ReleasePointerCaptures();
        canvas.CreateResources -= CreateResources;
        canvas.Draw -= Draw;
        canvas.PointerPressed -= Canvas_PointerPressed;
        canvas.PointerMoved -= Canvas_PointerMoved;
        canvas.PointerReleased -= Canvas_PointerReleased;
        canvas.PointerCanceled -= PointerCancelled;
        canvas.PointerCaptureLost -= PointerCancelled;
        canvas.RemoveFromVisualTree();
        Host.Children.Remove(canvas);
        lock (_renderGate)
        {
            _renderer?.Dispose();
            _renderer = null;
        }
    }

    private void CreateResources(CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args) =>
        args.TrackAsyncAction(LoadAsync(sender).AsAsyncAction());

    private async Task LoadAsync(CanvasAnimatedControl canvas)
    {
        try
        {
            CanvasBitmap logo = await CanvasBitmap.LoadAsync(canvas, new Uri("ms-appx:///CubeAnimation/Logo_Large.png"));
            if (!ReferenceEquals(_canvas, canvas))
            {
                logo.Dispose();
                return;
            }
            lock (_renderGate)
            {
                bool firstLoad = _renderer is null;
                _renderer?.Dispose();
                _renderer = null;
                _renderer = new CubeRenderer(canvas, logo);
                if (firstLoad) _start = Stopwatch.GetTimestamp();
            }
        }
        catch (Exception ex) when (ex is IOException or COMException or UnauthorizedAccessException or InvalidOperationException)
        {
            if (!ReferenceEquals(_canvas, canvas)) return;
            Trace.WriteLine($"Home cube resources failed: {ex}");
            if (((App)Application.Current).MainWindow is MainWindow window)
                window.Log($"Home cube resources failed: {ex.Message}");
            Error.Title = "Could not load the cube animation";
            Error.Message = ex.Message;
            Error.IsOpen = true;
        }
    }

    private double Elapsed => Stopwatch.GetElapsedTime(_start).TotalSeconds;

    private void Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
    {
        lock (_renderGate)
        {
            if (!ReferenceEquals(sender, _canvas)) return;
            double now = Elapsed;
            if (_renderer is null)
            {
                args.DrawingSession.Clear(Microsoft.UI.Colors.Transparent);
                return;
            }
            _renderer.Draw(args.DrawingSession, sender.Size, now, _interaction.Rotation(now));
        }
    }

    private static Vector2 DesignPoint(CanvasAnimatedControl canvas, Point point)
    {
        float scale = Math.Min((float)canvas.ActualWidth / 1600, (float)canvas.ActualHeight / 1000);
        return new Vector2((float)point.X, (float)point.Y) / Math.Max(scale, 0.001f);
    }

    private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs args)
    {
        if (sender is not CanvasAnimatedControl canvas || _capturedPointer is not null || _renderer is null) return;
        var point = args.GetCurrentPoint(canvas);
        if (!point.Properties.IsLeftButtonPressed ||
            !_renderer.HitTest(new Vector2((float)point.Position.X, (float)point.Position.Y),
                new Size(canvas.ActualWidth, canvas.ActualHeight))) return;
        if (!canvas.CapturePointer(args.Pointer)) return;
        _capturedPointer = args.Pointer.PointerId;
        _interaction.Grab(DesignPoint(canvas, point.Position), Elapsed);
        args.Handled = true;
    }

    private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs args)
    {
        if (sender is not CanvasAnimatedControl canvas || _capturedPointer != args.Pointer.PointerId) return;
        _interaction.Move(DesignPoint(canvas, args.GetCurrentPoint(canvas).Position), Elapsed);
        args.Handled = true;
    }

    private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs args)
    {
        if (sender is not CanvasAnimatedControl canvas || _capturedPointer != args.Pointer.PointerId) return;
        _interaction.Move(DesignPoint(canvas, args.GetCurrentPoint(canvas).Position), Elapsed);
        _interaction.Release(Elapsed);
        _capturedPointer = null;
        canvas.ReleasePointerCapture(args.Pointer);
        args.Handled = true;
    }

    private void PointerCancelled(object sender, PointerRoutedEventArgs args)
    {
        if (_capturedPointer != args.Pointer.PointerId) return;
        _interaction.Release(Elapsed, true);
        _capturedPointer = null;
    }
}
