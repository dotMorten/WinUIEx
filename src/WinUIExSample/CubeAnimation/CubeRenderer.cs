using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Windows.Foundation;
using Windows.UI;

namespace WinUIExSample.CubeAnimation;

internal sealed class CubeRenderer : IDisposable
{
    private const float Width = 1600, Height = 1000;
    private const int TextureSize = 1024;
    private static readonly Vector2 LogoOrigin = new(510, 509);
    private static readonly Vector2[] Axes = [new(241.3324f, -418), new(-482.6648f, 0), new(241.3324f, 418)];
    private static readonly Vector3[] WorldAxes = [Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ];
    private readonly CanvasBitmap _logo;
    private readonly CanvasRenderTarget[] _faces = new CanvasRenderTarget[3];
    private readonly Vector2[][] _contours = new Vector2[3][];
    private readonly List<Vector2> _outlinePoints = new(3200);
    private readonly List<Vector2> _hull = new(3200);
    private readonly List<Face> _visible = new(6);
    private readonly CanvasRadialGradientBrush _background, _ground;
    private readonly GaussianBlurEffect _halo;
    private Vector2[] _hitPolygon = [];
    private readonly record struct Face(int Texture, Vector3 Origin, Vector3 U, Vector3 V, float Depth);

    public CubeRenderer(ICanvasResourceCreator creator, CanvasBitmap logo)
    {
        _logo = logo;
        _halo = new GaussianBlurEffect { Source = logo, BlurAmount = 45 };
        _background = new CanvasRadialGradientBrush(creator, new[]
        {
            new CanvasGradientStop { Position = 0, Color = Color.FromArgb(255, 12, 37, 56) },
            new CanvasGradientStop { Position = 0.5f, Color = Color.FromArgb(255, 5, 17, 32) },
            new CanvasGradientStop { Position = 1, Color = Color.FromArgb(255, 2, 5, 13) }
        }) { Center = new Vector2(800, 470), RadiusX = 990, RadiusY = 690 };
        _ground = new CanvasRadialGradientBrush(creator, new[]
        {
            new CanvasGradientStop { Position = 0, Color = Color.FromArgb(90, 15, 149, 210) },
            new CanvasGradientStop { Position = 1, Color = Color.FromArgb(0, 10, 100, 160) }
        }) { Center = new Vector2(800, 809), RadiusX = 420, RadiusY = 85 };

        try
        {
            int[,] pairs = { { 0, 1 }, { 0, 2 }, { 1, 2 } };
            for (int face = 0; face < 3; face++)
            {
                Vector2 u = Axes[pairs[face, 0]], v = Axes[pairs[face, 1]];
                var transform = new Matrix3x2(u.X / TextureSize, u.Y / TextureSize,
                    v.X / TextureSize, v.Y / TextureSize, LogoOrigin.X, LogoOrigin.Y);
                if (!Matrix3x2.Invert(transform, out Matrix3x2 inverse))
                    throw new InvalidOperationException("Degenerate logo face mapping.");
                _faces[face] = new CanvasRenderTarget(creator, TextureSize, TextureSize, 96);
                using (CanvasDrawingSession ds = _faces[face].CreateDrawingSession())
                {
                    ds.Clear(Microsoft.UI.Colors.Transparent);
                    ds.Transform = inverse;
                    ds.DrawImage(logo);
                }
                byte[] pixels = _faces[face].GetPixelBytes();
                var contour = new List<Vector2>();
                for (int y = 0; y < TextureSize; y += 2)
                {
                    int first = 0, last = TextureSize - 1;
                    while (first < TextureSize && pixels[(y * TextureSize + first) * 4 + 3] < 128) first++;
                    while (last >= first && pixels[(y * TextureSize + last) * 4 + 3] < 128) last--;
                    if (first <= last)
                    {
                        contour.Add(new Vector2((first + 0.5f) / TextureSize, (y + 0.5f) / TextureSize));
                        contour.Add(new Vector2((last + 0.5f) / TextureSize, (y + 0.5f) / TextureSize));
                    }
                }
                if (contour.Count < 3) throw new InvalidOperationException("The logo contains an empty cube face.");
                _contours[face] = contour.ToArray();
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Draw(CanvasDrawingSession ds, Size size, double time, Matrix4x4? rotationOverride = null)
    {
        ds.Clear(Color.FromArgb(255, 2, 5, 13));
        if (size.Width <= 0 || size.Height <= 0) return;
        float scale = Math.Min((float)size.Width / Width, (float)size.Height / Height);
        ds.Transform = Matrix3x2.CreateScale(scale) *
            Matrix3x2.CreateTranslation(((float)size.Width - Width * scale) / 2,
                ((float)size.Height - Height * scale) / 2);
        using (ds.CreateLayer(1, new Rect(0, 0, Width, Height)))
        {
            Background(ds, (float)time);
            float entrance = Smooth((float)time / 0.4f);
            float cubeScale = 0.56f + 0.025f * Smooth((float)(time / CubeMotion.Duration));
            Vector2 center = new(800, 473);
            ds.FillRectangle(0, 710, Width, 220, _ground);
            float glow = 0.055f + CubeMotion.SettlingProgress(time) * 0.025f;
            ds.DrawImage(_halo, new Rect(center.X - 300, center.Y - 300, 600, 600),
                new Rect(0, 0, _logo.Size.Width, _logo.Size.Height), glow * entrance);
            double trailDuration = CubeMotion.Duration * 0.7;
            if (time < trailDuration && rotationOverride is null)
            {
                for (int trail = 2; trail >= 1; trail--)
                    Cube(ds, Math.Max(0, time - trail * 0.014), center, cubeScale,
                        entrance * 0.025f * (1 - (float)(time / trailDuration)));
            }
            Cube(ds, time, center, cubeScale, entrance, rotationOverride);
            Volatile.Write(ref _hitPolygon, _hull.ToArray());
        }
        ds.Transform = Matrix3x2.Identity;
    }

    public bool HitTest(Vector2 point, Size size)
    {
        float scale = Math.Min((float)size.Width / Width, (float)size.Height / Height);
        if (scale <= 0) return false;
        point = (point - new Vector2(((float)size.Width - Width * scale) / 2,
            ((float)size.Height - Height * scale) / 2)) / scale;
        Vector2[] polygon = Volatile.Read(ref _hitPolygon);
        if (polygon.Length < 3) return false;
        for (int i = 0; i < polygon.Length; i++)
            if (Cross(polygon[(i + 1) % polygon.Length] - polygon[i], point - polygon[i]) < 0) return false;
        return true;
    }

    private void Cube(CanvasDrawingSession ds, double time, Vector2 center, float scale, float opacity,
        Matrix4x4? rotationOverride = null)
    {
        Matrix4x4 rotation = rotationOverride ?? CubeMotion.Rotation(time);
        _visible.Clear();
        AddFaces(0, 0, 1, 2, rotation);
        AddFaces(1, 0, 2, 1, rotation);
        AddFaces(2, 1, 2, 0, rotation);
        _visible.Sort((a, b) => b.Depth.CompareTo(a.Depth));
        Matrix3x2 previous = ds.Transform;
        using CanvasGeometry silhouette = CreateSilhouette(ds, center, scale);
        using (ds.CreateLayer(opacity, silhouette))
        {
            foreach (Face face in _visible)
            {
                Vector2 origin = Project(face.Origin) * scale + center;
                Vector2 u = Project(face.U) * scale / TextureSize;
                Vector2 v = Project(face.V) * scale / TextureSize;
                ds.Transform = new Matrix3x2(u.X, u.Y, v.X, v.Y, origin.X, origin.Y) * previous;
                Color body = face.Texture switch
                {
                    0 => Color.FromArgb(255, 29, 185, 239),
                    1 => Color.FromArgb(255, 0, 107, 186),
                    _ => Color.FromArgb(255, 0, 143, 224)
                };
                ds.FillRectangle(-0.6f, -0.6f, TextureSize + 1.2f, TextureSize + 1.2f, body);
                ds.DrawImage(_faces[face.Texture]);
            }
            ds.Transform = previous;
        }
    }

    private CanvasGeometry CreateSilhouette(ICanvasResourceCreator creator, Vector2 center, float scale)
    {
        // One shared outline preserves the logo's rounded corners without opening gaps between faces.
        _outlinePoints.Clear();
        foreach (Face face in _visible)
            foreach (Vector2 uv in _contours[face.Texture])
                _outlinePoints.Add(Project(face.Origin + face.U * uv.X + face.V * uv.Y) * scale + center);
        _outlinePoints.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
        _hull.Clear();
        foreach (Vector2 point in _outlinePoints)
        {
            while (_hull.Count >= 2 && Cross(_hull[^1] - _hull[^2], point - _hull[^1]) <= 0)
                _hull.RemoveAt(_hull.Count - 1);
            _hull.Add(point);
        }
        int lower = _hull.Count;
        for (int i = _outlinePoints.Count - 2; i >= 0; i--)
        {
            Vector2 point = _outlinePoints[i];
            while (_hull.Count > lower && Cross(_hull[^1] - _hull[^2], point - _hull[^1]) <= 0)
                _hull.RemoveAt(_hull.Count - 1);
            _hull.Add(point);
        }
        _hull.RemoveAt(_hull.Count - 1);
        using var path = new CanvasPathBuilder(creator);
        path.BeginFigure(_hull[0]);
        for (int i = 1; i < _hull.Count; i++) path.AddLine(_hull[i]);
        path.EndFigure(CanvasFigureLoop.Closed);
        return CanvasGeometry.CreatePath(path);
    }

    private void AddFaces(int texture, int u, int v, int normalAxis, Matrix4x4 rotation)
    {
        for (int side = 0; side <= 1; side++)
        {
            Vector3 normal = Vector3.TransformNormal(WorldAxes[normalAxis] * (side == 0 ? -1 : 1), rotation);
            if (normal.X + normal.Y + normal.Z >= -0.0001f) continue;
            Vector3 origin = Vector3.Transform(WorldAxes[normalAxis] * side - new Vector3(0.5f), rotation);
            Vector3 du = Vector3.TransformNormal(WorldAxes[u], rotation);
            Vector3 dv = Vector3.TransformNormal(WorldAxes[v], rotation);
            Vector3 middle = origin + (du + dv) / 2;
            _visible.Add(new Face(texture, origin, du, dv, middle.X + middle.Y + middle.Z));
        }
    }

    private void Background(CanvasDrawingSession ds, float time)
    {
        float t = (float)Math.Min(time, CubeMotion.Duration);
        float duration = (float)CubeMotion.Duration;
        float drift = 1 - MathF.Pow(1 - t / duration, 3);
        ds.FillRectangle(0, 0, Width, Height, _background);
        for (int ribbon = 0; ribbon < 5; ribbon++)
        {
            Vector2 previous = default;
            for (int i = 0; i <= 100; i++)
            {
                float x = -80 + i * 17.6f;
                float y = 645 + ribbon * 23 + MathF.Sin(i * 0.039f + drift * 1.4f + ribbon * 0.11f) * 135;
                Vector2 p = new(x, y);
                if (i > 0) ds.DrawLine(previous, p, Color.FromArgb((byte)(9 + ribbon * 2), 33, 183, 239), 1);
                previous = p;
            }
        }
        for (int i = 0; i < StarField.Count; i++)
        {
            StarField.Star star = StarField.Sample(i, time);
            if (star.Opacity <= 0) continue;
            byte alpha = (byte)(star.Opacity * 255);
            ds.FillCircle(star.Position, star.Radius * 2.8f, Color.FromArgb((byte)(alpha * 0.1f), 100, 200, 255));
            ds.FillCircle(star.Position, star.Radius, Color.FromArgb(alpha, 175, 230, 255));
        }
        float arrival = MathF.Exp(-MathF.Pow((t - (duration - 0.6f)) / 0.5f, 2));
        if (arrival > 0.01f)
            ds.DrawEllipse(800, 806, 250 + (t - (duration - 1.5f)) * 90, 30 + (t - (duration - 1.5f)) * 9,
                Color.FromArgb((byte)(arrival * 30), 50, 210, 255), 1.4f);
    }

    private static Vector2 Project(Vector3 p) => Axes[0] * p.X + Axes[1] * p.Y + Axes[2] * p.Z;
    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    private static float Smooth(float value)
    {
        value = Math.Clamp(value, 0, 1);
        return value * value * (3 - 2 * value);
    }

    public void Dispose()
    {
        foreach (CanvasRenderTarget? face in _faces) face?.Dispose();
        _logo.Dispose();
        _halo.Dispose();
        _background.Dispose();
        _ground.Dispose();
    }
}
