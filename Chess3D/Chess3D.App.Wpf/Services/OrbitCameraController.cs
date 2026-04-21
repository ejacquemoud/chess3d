using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace Chess3D.App.Wpf.Services;

public sealed class OrbitCameraController
{
    private readonly UIElement _inputElement;
    private readonly PerspectiveCamera _camera;

    private Point _lastMousePosition;
    private bool _isRotating;

    private Point3D _target = new(0, 0, 0);
    private double _distance = 13.0;
    private double _yawDegrees = 0.0;
    private double _pitchDegrees = -35.0;
    private double _fieldOfView = 60.0;

    private AnimationClockDriver? _activeAnimation;

    public OrbitCameraController(UIElement inputElement, PerspectiveCamera camera)
    {
        _inputElement = inputElement;
        _camera = camera;

        _inputElement.MouseRightButtonDown += OnMouseRightButtonDown;
        _inputElement.MouseRightButtonUp += OnMouseRightButtonUp;
        _inputElement.MouseMove += OnMouseMove;
        _inputElement.MouseWheel += OnMouseWheel;
        _inputElement.MouseLeave += OnMouseLeave;

        ApplyCameraImmediate();
    }

    public void SetTarget(Point3D target)
    {
        StopActiveAnimation();
        _target = target;
        ApplyCameraImmediate();
    }

    public void SetDistance(double distance)
    {
        StopActiveAnimation();
        _distance = Clamp(distance, 4.0, 30.0);
        ApplyCameraImmediate();
    }

    public void SetAngles(double yawDegrees, double pitchDegrees)
    {
        StopActiveAnimation();
        _yawDegrees = NormalizeAngle(yawDegrees);
        _pitchDegrees = ClampPitch(pitchDegrees);
        ApplyCameraImmediate();
    }

    public void SetFieldOfView(double fieldOfView)
    {
        StopActiveAnimation();
        _fieldOfView = Clamp(fieldOfView, 20.0, 80.0);
        ApplyCameraImmediate();
    }

    public void SetView(Point3D target, double distance, double yawDegrees, double pitchDegrees, double fieldOfView = 60.0)
    {
        StopActiveAnimation();

        _target = target;
        _distance = Clamp(distance, 4.0, 30.0);
        _yawDegrees = NormalizeAngle(yawDegrees);
        _pitchDegrees = ClampPitch(pitchDegrees);
        _fieldOfView = Clamp(fieldOfView, 20.0, 80.0);

        ApplyCameraImmediate();
    }

    public void AnimateToView(Point3D target, double distance, double yawDegrees, double pitchDegrees, double fieldOfView = 60.0, int durationMs = 700)
    {
        SyncFromCurrentCamera();
        StopActiveAnimation();

        var fromTarget = _target;
        var fromDistance = _distance;
        var fromYaw = _yawDegrees;
        var fromPitch = _pitchDegrees;
        var fromFov = _fieldOfView;

        var toTarget = target;
        var toDistance = Clamp(distance, 4.0, 30.0);
        var toYaw = NormalizeAngle(yawDegrees);
        var toPitch = ClampPitch(pitchDegrees);
        var toFov = Clamp(fieldOfView, 20.0, 80.0);

        double yawDelta = ShortestAngleDelta(fromYaw, toYaw);

        var duration = TimeSpan.FromMilliseconds(durationMs);
        var easing = new SineEase { EasingMode = EasingMode.EaseInOut };

        _activeAnimation = new AnimationClockDriver(
            duration,
            progress =>
            {
                double eased = easing.Ease(progress);

                _target = Lerp(fromTarget, toTarget, eased);
                _distance = Lerp(fromDistance, toDistance, eased);
                _yawDegrees = NormalizeAngle(fromYaw + yawDelta * eased);
                _pitchDegrees = Lerp(fromPitch, toPitch, eased);
                _fieldOfView = Lerp(fromFov, toFov, eased);

                ApplyCameraImmediate();
            },
            () => _activeAnimation = null);

        _activeAnimation.Start();
    }

    public void SyncFromCurrentCamera()
    {
        var look = _camera.LookDirection;
        if (look.LengthSquared < 0.000001)
            return;

        Point3D position = _camera.Position;
        Point3D target = position + look;

        Vector3D offset = position - target;
        double distance = offset.Length;
        if (distance < 0.000001)
            return;

        double yaw = Math.Atan2(offset.X, offset.Z) * 180.0 / Math.PI;
        double horizontalLength = Math.Sqrt(offset.X * offset.X + offset.Z * offset.Z);
        double pitch = Math.Atan2(-offset.Y, horizontalLength) * 180.0 / Math.PI;

        _target = target;
        _distance = distance;
        _yawDegrees = NormalizeAngle(yaw);
        _pitchDegrees = ClampPitch(pitch);
        _fieldOfView = _camera.FieldOfView;
    }

    private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        StopActiveAnimation();
        SyncFromCurrentCamera();

        _isRotating = true;
        _lastMousePosition = e.GetPosition(_inputElement);
        _inputElement.CaptureMouse();
    }

    private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isRotating = false;
        _inputElement.ReleaseMouseCapture();
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (!_isRotating)
            return;

        _isRotating = false;
        _inputElement.ReleaseMouseCapture();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isRotating)
            return;

        Point current = e.GetPosition(_inputElement);
        Vector delta = current - _lastMousePosition;
        _lastMousePosition = current;

        const double rotationSpeed = 0.35;

        _yawDegrees = NormalizeAngle(_yawDegrees + delta.X * rotationSpeed);
        _pitchDegrees = ClampPitch(_pitchDegrees - delta.Y * rotationSpeed);

        ApplyCameraImmediate();
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        StopActiveAnimation();
        SyncFromCurrentCamera();

        double zoomFactor = e.Delta > 0 ? 0.90 : 1.10;
        _distance = Clamp(_distance * zoomFactor, 4.0, 30.0);

        ApplyCameraImmediate();
    }

    private void StopActiveAnimation()
    {
        _activeAnimation?.Stop();
        _activeAnimation = null;
    }

    private void ApplyCameraImmediate()
    {
        Point3D position = ComputePosition(_target, _distance, _yawDegrees, _pitchDegrees);

        _camera.BeginAnimation(PerspectiveCamera.PositionProperty, null);
        _camera.BeginAnimation(PerspectiveCamera.LookDirectionProperty, null);
        _camera.BeginAnimation(PerspectiveCamera.FieldOfViewProperty, null);

        _camera.Position = position;
        _camera.LookDirection = _target - position;
        _camera.UpDirection = new Vector3D(0, 1, 0);
        _camera.FieldOfView = _fieldOfView;
    }

    private static Point3D ComputePosition(Point3D target, double distance, double yawDegrees, double pitchDegrees)
    {
        double yaw = yawDegrees * Math.PI / 180.0;
        double pitch = pitchDegrees * Math.PI / 180.0;

        double cosPitch = Math.Cos(pitch);
        double sinPitch = Math.Sin(pitch);
        double sinYaw = Math.Sin(yaw);
        double cosYaw = Math.Cos(yaw);

        double x = target.X + distance * cosPitch * sinYaw;
        double y = target.Y - distance * sinPitch;
        double z = target.Z + distance * cosPitch * cosYaw;

        return new Point3D(x, y, z);
    }

    private static double Clamp(double value, double min, double max)
        => value < min ? min : (value > max ? max : value);

    private static double ClampPitch(double pitchDegrees)
        => Clamp(pitchDegrees, -80.0, 80.0);

    private static double NormalizeAngle(double angleDegrees)
    {
        double angle = angleDegrees % 360.0;
        return angle < 0 ? angle + 360.0 : angle;
    }

    private static double ShortestAngleDelta(double fromDegrees, double toDegrees)
    {
        double delta = NormalizeAngle(toDegrees) - NormalizeAngle(fromDegrees);

        if (delta > 180.0)
            delta -= 360.0;
        else if (delta < -180.0)
            delta += 360.0;

        return delta;
    }

    private static double Lerp(double a, double b, double t)
        => a + (b - a) * t;

    private static Point3D Lerp(Point3D a, Point3D b, double t)
        => new(
            Lerp(a.X, b.X, t),
            Lerp(a.Y, b.Y, t),
            Lerp(a.Z, b.Z, t));

    private sealed class AnimationClockDriver
    {
        private readonly TimeSpan _duration;
        private readonly Action<double> _onProgress;
        private readonly Action? _onCompleted;
        private DateTime _startTime;
        private bool _isRunning;

        public AnimationClockDriver(TimeSpan duration, Action<double> onProgress, Action? onCompleted = null)
        {
            _duration = duration;
            _onProgress = onProgress;
            _onCompleted = onCompleted;
        }

        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            _startTime = DateTime.UtcNow;
            CompositionTarget.Rendering += OnRendering;
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            double elapsed = (DateTime.UtcNow - _startTime).TotalMilliseconds;
            double progress = _duration.TotalMilliseconds <= 0
                ? 1.0
                : Math.Clamp(elapsed / _duration.TotalMilliseconds, 0.0, 1.0);

            _onProgress(progress);

            if (progress >= 1.0)
            {
                Stop();
                _onCompleted?.Invoke();
            }
        }
    }
}