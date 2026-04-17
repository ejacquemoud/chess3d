using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace Chess3D.App.Wpf.Services;

public sealed class OrbitCameraController
{
    private readonly UIElement _inputElement;
    private readonly PerspectiveCamera _camera;

    private Point _lastMousePosition;
    private bool _isRotating;
    private bool _isPanning;

    private Point3D _target;
    private double _distance;
    private double _yaw;
    private double _pitch;

    public OrbitCameraController(UIElement inputElement, PerspectiveCamera camera)
    {
        _inputElement = inputElement;
        _camera = camera;

        _target = new Point3D(0, 0.5, 0);
        _distance = 13.0;
        _yaw = 0.0;
        _pitch = -35.0;

        _inputElement.MouseDown += OnMouseDown;
        _inputElement.MouseUp += OnMouseUp;
        _inputElement.MouseMove += OnMouseMove;
        _inputElement.MouseWheel += OnMouseWheel;

        UpdateCamera();
    }

    public void SetTarget(Point3D target)
    {
        _target = target;
        UpdateCamera();
    }

    public void SetDistance(double distance)
    {
        _distance = Math.Max(4.0, Math.Min(40.0, distance));
        UpdateCamera();
    }

    public void SetAngles(double yaw, double pitch)
    {
        _yaw = yaw;
        _pitch = Math.Max(-80.0, Math.Min(-10.0, pitch));
        UpdateCamera();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _lastMousePosition = e.GetPosition(_inputElement);

        if (e.ChangedButton == MouseButton.Left)
        {
            _isRotating = true;
            Mouse.Capture(_inputElement);
        }
        else if (e.ChangedButton == MouseButton.Right)
        {
            _isPanning = true;
            Mouse.Capture(_inputElement);
        }
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isRotating = false;
        _isPanning = false;
        Mouse.Capture(null);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var current = e.GetPosition(_inputElement);
        var delta = current - _lastMousePosition;
        _lastMousePosition = current;

        if (_isRotating)
        {
            _yaw += delta.X * 0.5;
            _pitch -= delta.Y * 0.4;
            _pitch = Math.Max(-80.0, Math.Min(-10.0, _pitch));
            UpdateCamera();
        }
        else if (_isPanning)
        {
            Pan(delta.X, delta.Y);
            UpdateCamera();
        }
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        double zoomFactor = e.Delta > 0 ? 0.9 : 1.1;
        _distance *= zoomFactor;
        _distance = Math.Max(4.0, Math.Min(40.0, _distance));
        UpdateCamera();
    }

    private void Pan(double dx, double dy)
    {
        var look = _camera.LookDirection;
        look.Normalize();

        var up = _camera.UpDirection;
        up.Normalize();

        var right = Vector3D.CrossProduct(look, up);
        right.Normalize();

        double panSpeed = _distance * 0.0025;

        _target -= right * (dx * panSpeed);
        _target += up * (dy * panSpeed);
    }

    private void UpdateCamera()
    {
        double yawRad = _yaw * Math.PI / 180.0;
        double pitchRad = _pitch * Math.PI / 180.0;

        double x = _target.X + _distance * Math.Cos(pitchRad) * Math.Sin(yawRad);
        double y = _target.Y - _distance * Math.Sin(pitchRad);
        double z = _target.Z + _distance * Math.Cos(pitchRad) * Math.Cos(yawRad);

        var position = new Point3D(x, y, z);
        var lookDirection = _target - position;

        _camera.Position = position;
        _camera.LookDirection = lookDirection;
        _camera.UpDirection = new Vector3D(0, 1, 0);
    }
}