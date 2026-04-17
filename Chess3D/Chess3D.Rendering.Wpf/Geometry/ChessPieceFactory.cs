using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Chess3D.Rendering.Wpf.Enums;

namespace Chess3D.Rendering.Wpf.Geometry;

public static class ChessPieceFactory
{
    private const double PieceLift = 0.01;

    public static List<GeometryModel3D> CreatePieceModels(PieceType type, PieceColor pieceColor, Point3D baseCenter)
    {
        var color = pieceColor == PieceColor.White
            ? Color.FromRgb(236, 228, 214)
            : Color.FromRgb(52, 42, 36);

        var c = new Point3D(baseCenter.X, baseCenter.Y + PieceLift, baseCenter.Z);

        return type switch
        {
            PieceType.Pawn => CreatePawn(c, color),
            PieceType.Rook => CreateRook(c, color),
            PieceType.Knight => CreateKnight(c, color),
            PieceType.Bishop => CreateBishop(c, color),
            PieceType.Queen => CreateQueen(c, color),
            PieceType.King => CreateKing(c, color),
            _ => new List<GeometryModel3D>()
        };
    }

    private static double CenterY(Point3D c, double bottom, double height)
        => c.Y + bottom + (height / 2.0);

    private static List<GeometryModel3D> CreatePawn(Point3D c, Color color)
    {
        return new()
        {
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.00, 0.08), c.Z), 0.28, 0.08, color, 28),
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.08, 0.06), c.Z), 0.20, 0.06, color, 28),
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.14, 0.30), c.Z), 0.11, 0.30, color, 24),
            CreateSphere(new Point3D(c.X, c.Y + 0.46, c.Z), 0.16, color, 18, 12)
        };
    }

    private static List<GeometryModel3D> CreateRook(Point3D c, Color color)
    {
        return new()
        {
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.00, 0.08), c.Z), 0.30, 0.08, color, 28),
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.08, 0.08), c.Z), 0.22, 0.08, color, 28),
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.16, 0.40), c.Z), 0.16, 0.40, color, 28),
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.56, 0.10), c.Z), 0.22, 0.10, color, 28),

            CreateBox(new Point3D(c.X - 0.13, CenterY(c, 0.66, 0.10), c.Z), 0.08, 0.10, 0.10, color),
            CreateBox(new Point3D(c.X + 0.13, CenterY(c, 0.66, 0.10), c.Z), 0.08, 0.10, 0.10, color),
            CreateBox(new Point3D(c.X, CenterY(c, 0.66, 0.10), c.Z - 0.13), 0.10, 0.10, 0.08, color),
            CreateBox(new Point3D(c.X, CenterY(c, 0.66, 0.10), c.Z + 0.13), 0.10, 0.10, 0.08, color),
        };
    }

    private static List<GeometryModel3D> CreateKnight(Point3D c, Color color)
    {
        return new()
        {
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.00, 0.08), c.Z), 0.30, 0.08, color, 28),
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.08, 0.08), c.Z), 0.22, 0.08, color, 28),

            CreateBox(new Point3D(c.X, CenterY(c, 0.16, 0.34), c.Z), 0.20, 0.34, 0.26, color),
            CreateBox(new Point3D(c.X + 0.04, CenterY(c, 0.39, 0.22), c.Z), 0.18, 0.22, 0.22, color),
            CreateBox(new Point3D(c.X + 0.10, CenterY(c, 0.54, 0.16), c.Z), 0.16, 0.16, 0.18, color),
            CreateBox(new Point3D(c.X - 0.06, CenterY(c, 0.32, 0.26), c.Z), 0.08, 0.26, 0.18, color)
        };
    }

    private static List<GeometryModel3D> CreateBishop(Point3D c, Color color)
    {
        return new()
        {
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.00, 0.08), c.Z), 0.30, 0.08, color, 28),
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.08, 0.08), c.Z), 0.21, 0.08, color, 28),
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.16, 0.46), c.Z), 0.12, 0.46, color, 26),
            CreateSphere(new Point3D(c.X, c.Y + 0.65, c.Z), 0.17, color, 20, 14),
            CreateBox(new Point3D(c.X, CenterY(c, 0.74, 0.18), c.Z), 0.04, 0.18, 0.22, color)
        };
    }

    private static List<GeometryModel3D> CreateQueen(Point3D c, Color color)
    {
        return new()
        {
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.00, 0.08), c.Z), 0.31, 0.08, color, 28),
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.08, 0.08), c.Z), 0.23, 0.08, color, 28),
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.16, 0.50), c.Z), 0.13, 0.50, color, 28),
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.66, 0.08), c.Z), 0.19, 0.08, color, 28),
            CreateSphere(new Point3D(c.X, c.Y + 0.80, c.Z), 0.14, color, 20, 14),

            CreateSphere(new Point3D(c.X - 0.11, c.Y + 0.72, c.Z), 0.04, color, 12, 8),
            CreateSphere(new Point3D(c.X + 0.11, c.Y + 0.72, c.Z), 0.04, color, 12, 8),
            CreateSphere(new Point3D(c.X, c.Y + 0.72, c.Z - 0.11), 0.04, color, 12, 8),
            CreateSphere(new Point3D(c.X, c.Y + 0.72, c.Z + 0.11), 0.04, color, 12, 8)
        };
    }

    private static List<GeometryModel3D> CreateKing(Point3D c, Color color)
    {
        return new()
        {
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.00, 0.08), c.Z), 0.31, 0.08, color, 28),
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.08, 0.08), c.Z), 0.23, 0.08, color, 28),
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.16, 0.56), c.Z), 0.13, 0.56, color, 28),
            CreateCylinder(new Point3D(c.X, CenterY(c, 0.72, 0.08), c.Z), 0.18, 0.08, color, 28),
            CreateBox(new Point3D(c.X, CenterY(c, 0.80, 0.20), c.Z), 0.07, 0.20, 0.07, color),
            CreateBox(new Point3D(c.X, CenterY(c, 0.95, 0.05), c.Z), 0.22, 0.05, 0.05, color)
        };
    }

    private static GeometryModel3D CreateBox(Point3D center, double sizeX, double sizeY, double sizeZ, Color color)
    {
        double hx = sizeX / 2.0;
        double hy = sizeY / 2.0;
        double hz = sizeZ / 2.0;

        var p0 = new Point3D(center.X - hx, center.Y - hy, center.Z - hz);
        var p1 = new Point3D(center.X + hx, center.Y - hy, center.Z - hz);
        var p2 = new Point3D(center.X + hx, center.Y + hy, center.Z - hz);
        var p3 = new Point3D(center.X - hx, center.Y + hy, center.Z - hz);
        var p4 = new Point3D(center.X - hx, center.Y - hy, center.Z + hz);
        var p5 = new Point3D(center.X + hx, center.Y - hy, center.Z + hz);
        var p6 = new Point3D(center.X + hx, center.Y + hy, center.Z + hz);
        var p7 = new Point3D(center.X - hx, center.Y + hy, center.Z + hz);

        var mesh = new MeshGeometry3D
        {
            Positions = new Point3DCollection { p0, p1, p2, p3, p4, p5, p6, p7 },
            TriangleIndices = new Int32Collection
            {
                4,5,6, 4,6,7,
                0,2,1, 0,3,2,
                0,4,7, 0,7,3,
                1,2,6, 1,6,5,
                3,7,6, 3,6,2,
                0,1,5, 0,5,4
            }
        };

        return CreateModel(mesh, color);
    }

    private static GeometryModel3D CreateCylinder(Point3D center, double radius, double height, Color color, int divisions)
    {
        var mesh = new MeshGeometry3D();

        double y0 = center.Y - height / 2.0;
        double y1 = center.Y + height / 2.0;

        mesh.Positions.Add(new Point3D(center.X, y0, center.Z));
        mesh.Positions.Add(new Point3D(center.X, y1, center.Z));

        for (int i = 0; i < divisions; i++)
        {
            double angle = 2.0 * Math.PI * i / divisions;
            double x = center.X + radius * Math.Cos(angle);
            double z = center.Z + radius * Math.Sin(angle);

            mesh.Positions.Add(new Point3D(x, y0, z));
            mesh.Positions.Add(new Point3D(x, y1, z));
        }

        for (int i = 0; i < divisions; i++)
        {
            int next = (i + 1) % divisions;

            int b0 = 2 + i * 2;
            int t0 = b0 + 1;
            int b1 = 2 + next * 2;
            int t1 = b1 + 1;

            mesh.TriangleIndices.Add(b0);
            mesh.TriangleIndices.Add(t0);
            mesh.TriangleIndices.Add(t1);

            mesh.TriangleIndices.Add(b0);
            mesh.TriangleIndices.Add(t1);
            mesh.TriangleIndices.Add(b1);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(b1);
            mesh.TriangleIndices.Add(b0);

            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(t0);
            mesh.TriangleIndices.Add(t1);
        }

        return CreateModel(mesh, color);
    }

    private static GeometryModel3D CreateSphere(Point3D center, double radius, Color color, int slices, int stacks)
    {
        var mesh = new MeshGeometry3D();

        for (int stack = 0; stack <= stacks; stack++)
        {
            double phi = Math.PI * stack / stacks;
            double y = radius * Math.Cos(phi);
            double r = radius * Math.Sin(phi);

            for (int slice = 0; slice <= slices; slice++)
            {
                double theta = 2.0 * Math.PI * slice / slices;
                double x = r * Math.Cos(theta);
                double z = r * Math.Sin(theta);

                mesh.Positions.Add(new Point3D(center.X + x, center.Y + y, center.Z + z));
            }
        }

        for (int stack = 0; stack < stacks; stack++)
        {
            for (int slice = 0; slice < slices; slice++)
            {
                int first = stack * (slices + 1) + slice;
                int second = first + slices + 1;

                mesh.TriangleIndices.Add(first);
                mesh.TriangleIndices.Add(second);
                mesh.TriangleIndices.Add(first + 1);

                mesh.TriangleIndices.Add(first + 1);
                mesh.TriangleIndices.Add(second);
                mesh.TriangleIndices.Add(second + 1);
            }
        }

        return CreateModel(mesh, color);
    }

    private static GeometryModel3D CreateModel(MeshGeometry3D mesh, Color color)
    {
        var diffuse = new DiffuseMaterial(new SolidColorBrush(color));
        var specular = new SpecularMaterial(
            new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)),
            60);

        var materialGroup = new MaterialGroup();
        materialGroup.Children.Add(diffuse);
        materialGroup.Children.Add(specular);

        return new GeometryModel3D
        {
            Geometry = mesh,
            Material = materialGroup,
            BackMaterial = materialGroup
        };
    }
}