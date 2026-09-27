using System;
using System.Numerics;

namespace WinUIExSample.CubeAnimation;

internal sealed class CubeInteraction
{
    private readonly object _gate = new();
    private bool _interacted, _dragging;
    private Quaternion _orientation = Quaternion.Identity, _releaseOrientation = Quaternion.Identity;
    private Vector2 _lastPoint;
    private Vector3 _velocity, _spinAxis = Vector3.UnitY;
    private double _lastMove, _releaseTime, _duration;
    private int _turns;

    public Matrix4x4? Rotation(double now)
    {
        lock (_gate) return _interacted ? Matrix4x4.CreateFromQuaternion(Current(now)) : null;
    }

    public void Grab(Vector2 point, double now)
    {
        lock (_gate)
        {
            _orientation = _interacted ? Current(now) : Quaternion.CreateFromRotationMatrix(CubeMotion.Rotation(now));
            _interacted = _dragging = true;
            _lastPoint = point;
            _lastMove = now;
            _velocity = Vector3.Zero;
        }
    }

    public void Move(Vector2 point, double now)
    {
        lock (_gate)
        {
            if (!_dragging) return;
            Vector2 delta = point - _lastPoint;
            Vector3 angular = new(delta.Y * 0.009f, delta.X * 0.009f, 0);
            float angle = angular.Length();
            if (angle > 0.00001f)
            {
                _orientation = Quaternion.Normalize(_orientation *
                    Quaternion.CreateFromAxisAngle(angular / angle, angle));
                float dt = (float)Math.Clamp(now - _lastMove, 0.001, 0.15);
                Vector3 speed = angular / dt;
                if (speed.Length() > 24) speed = Vector3.Normalize(speed) * 24;
                _velocity = Vector3.Lerp(_velocity, speed, 0.65f);
                _lastMove = now;
            }
            _lastPoint = point;
        }
    }

    public void Release(double now, bool cancelled = false)
    {
        lock (_gate)
        {
            if (!_dragging) return;
            _dragging = false;
            _releaseOrientation = _orientation;
            _releaseTime = now;
            float speed = cancelled || now - _lastMove > 0.15 ? 0 : _velocity.Length();
            _spinAxis = speed > 0.01f ? Vector3.Normalize(_velocity) : Vector3.UnitY;
            _duration = Math.Clamp(1.4 + speed * 0.13, 1.4, 4.5);
            _turns = (int)Math.Clamp(Math.Round(speed * _duration / (Math.Tau * 3)), 0, 3);
        }
    }

    private Quaternion Current(double now)
    {
        if (_dragging) return _orientation;
        if (_duration <= 0 || now >= _releaseTime + _duration) return Quaternion.Identity;
        float u = (float)Math.Clamp((now - _releaseTime) / _duration, 0, 1);
        float ease = u * u * (3 - 2 * u);
        Quaternion alignment = Quaternion.Slerp(_releaseOrientation, Quaternion.Identity, ease);
        float angle = (float)(Math.Tau * _turns) * (1 - MathF.Pow(1 - u, 3));
        return Quaternion.Normalize(alignment * Quaternion.CreateFromAxisAngle(_spinAxis, angle));
    }
}
