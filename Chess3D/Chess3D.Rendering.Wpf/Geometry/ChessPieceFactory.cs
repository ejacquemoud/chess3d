using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Chess3D.Rendering.Wpf.Enums;

namespace Chess3D.Rendering.Wpf.Geometry;

public static class ChessPieceFactory
{
    private const double PieceLift = 0.01;
    private const int DefaultSlices = 44;
    private const int SphereSlices = 24;
    private const int SphereStacks = 16;

    public static List<GeometryModel3D> CreatePieceModels(PieceType type, PieceColor pieceColor, Point3D baseCenter)
    {
        var color = pieceColor == PieceColor.White
            ? Color.FromRgb(238, 232, 220)
            : Color.FromRgb(58, 44, 34);

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

    private static List<GeometryModel3D> CreatePawn(Point3D c, Color color)
    {
        var body = new (double Radius, double Y)[]
        {
            (0.00, 0.00),
            (0.31, 0.02),
            (0.30, 0.05),
            (0.25, 0.09),
            (0.20, 0.12),
            (0.17, 0.18),
            (0.13, 0.28),
            (0.11, 0.39),
            (0.13, 0.48),
            (0.16, 0.55),
            (0.10, 0.60),
            (0.00, 0.62)
        };

        return new()
        {
            CreateLathedSolid(body, c, color, DefaultSlices),
            CreateSphere(new Point3D(c.X, c.Y + 0.70, c.Z), 0.11, color, SphereSlices, SphereStacks)
        };
    }

    private static List<GeometryModel3D> CreateRook(Point3D c, Color color)
    {
        var body = new (double Radius, double Y)[]
        {
            (0.00, 0.00),
            (0.33, 0.02),
            (0.31, 0.06),
            (0.25, 0.10),
            (0.21, 0.16),
            (0.19, 0.30),
            (0.19, 0.52),
            (0.21, 0.62),
            (0.25, 0.69),
            (0.24, 0.74),
            (0.00, 0.74)
        };

        var models = new List<GeometryModel3D>
        {
            CreateLathedSolid(body, c, color, DefaultSlices)
        };

        int crenelCount = 6;
        double crenelRadius = 0.265;

        for (int i = 0; i < crenelCount; i++)
        {
            double angle = 2.0 * Math.PI * i / crenelCount;
            double x = c.X + crenelRadius * Math.Cos(angle);
            double z = c.Z + crenelRadius * Math.Sin(angle);

            models.Add(CreateBox(
                new Point3D(x, c.Y + 0.79, z),
                0.085,
                0.10,
                0.10,
                color));
        }

        return models;
    }

    private static List<GeometryModel3D> CreateKnight(Point3D c, Color color)
    {
        var models = new List<GeometryModel3D>
        {
            CreateLathedSolid(new (double Radius, double Y)[]
            {
                (0.00, 0.00),
                (0.33, 0.02),
                (0.30, 0.06),
                (0.23, 0.10),
                (0.19, 0.17),
                (0.17, 0.24),
                (0.16, 0.31),
                (0.00, 0.33)
            }, c, color, DefaultSlices),

            CreateBox(new Point3D(c.X, c.Y + 0.49, c.Z), 0.22, 0.30, 0.27, color),
            CreateBox(new Point3D(c.X + 0.05, c.Y + 0.65, c.Z), 0.18, 0.20, 0.24, color),
            CreateBox(new Point3D(c.X + 0.11, c.Y + 0.80, c.Z), 0.14, 0.16, 0.18, color),
            CreateBox(new Point3D(c.X - 0.03, c.Y + 0.61, c.Z), 0.08, 0.30, 0.18, color),
            CreateBox(new Point3D(c.X + 0.03, c.Y + 0.92, c.Z), 0.05, 0.12, 0.08, color),
            CreateBox(new Point3D(c.X + 0.11, c.Y + 0.95, c.Z), 0.04, 0.10, 0.06, color),
            CreateBox(new Point3D(c.X + 0.11, c.Y + 0.72, c.Z + 0.08), 0.025, 0.025, 0.025, color),
            CreateBox(new Point3D(c.X + 0.11, c.Y + 0.72, c.Z - 0.08), 0.025, 0.025, 0.025, color)
        };

        return models;
    }

    private static List<GeometryModel3D> CreateBishop(Point3D c, Color color)
    {
        var body = new (double Radius, double Y)[]
        {
            (0.00, 0.00),
            (0.32, 0.02),
            (0.29, 0.06),
            (0.22, 0.10),
            (0.18, 0.18),
            (0.14, 0.33),
            (0.12, 0.52),
            (0.15, 0.64),
            (0.19, 0.73),
            (0.12, 0.82),
            (0.00, 0.84)
        };

        return new()
        {
            CreateLathedSolid(body, c, color, DefaultSlices),
            CreateSphere(new Point3D(c.X, c.Y + 0.86, c.Z), 0.085, color, SphereSlices, SphereStacks),
            CreateBox(new Point3D(c.X + 0.01, c.Y + 0.76, c.Z), 0.03, 0.20, 0.20, color)
        };
    }

    private static List<GeometryModel3D> CreateQueen(Point3D c, Color color)
    {
        var body = new (double Radius, double Y)[]
        {
            (0.00, 0.00),
            (0.34, 0.02),
            (0.31, 0.06),
            (0.24, 0.10),
            (0.19, 0.19),
            (0.15, 0.34),
            (0.13, 0.55),
            (0.16, 0.69),
            (0.21, 0.77),
            (0.17, 0.83),
            (0.00, 0.84)
        };

        var models = new List<GeometryModel3D>
        {
            CreateLathedSolid(body, c, color, DefaultSlices),
            CreateSphere(new Point3D(c.X, c.Y + 0.95, c.Z), 0.078, color, SphereSlices, SphereStacks)
        };

        AddCrownBeads(models, c, 0.86, 0.135, 6, 0.038, color);

        return models;
    }

    private static List<GeometryModel3D> CreateKing(Point3D c, Color color)
    {
        var body = new (double Radius, double Y)[]
        {
            (0.00, 0.00),
            (0.34, 0.02),
            (0.31, 0.06),
            (0.24, 0.10),
            (0.19, 0.19),
            (0.15, 0.36),
            (0.13, 0.60),
            (0.16, 0.74),
            (0.21, 0.82),
            (0.16, 0.88),
            (0.00, 0.89)
        };

        return new()
        {
            CreateLathedSolid(body, c, color, DefaultSlices),
            CreateBox(new Point3D(c.X, c.Y + 0.99, c.Z), 0.06, 0.20, 0.06, color),
            CreateBox(new Point3D(c.X, c.Y + 1.07, c.Z), 0.20, 0.04, 0.04, color)
        };
    }

    private static void AddCrownBeads(
        List<GeometryModel3D> models,
        Point3D c,
        double y,
        double radius,
        int count,
        double beadRadius,
        Color color)
    {
        for (int i = 0; i < count; i++)
        {
            double angle = 2.0 * Math.PI * i / count;
            double x = c.X + radius * Math.Cos(angle);
            double z = c.Z + radius * Math.Sin(angle);

            models.Add(CreateSphere(
                new Point3D(x, c.Y + y, z),
                beadRadius,
                color,
                16,
                12));
        }
    }

    private static GeometryModel3D CreateLathedSolid(
        IReadOnlyList<(double Radius, double Y)> profile,
        Point3D origin,
        Color color,
        int slices)
    {
        var mesh = new MeshGeometry3D();

        for (int i = 0; i < profile.Count; i++)
        {
            var (radius, y) = profile[i];

            for (int s = 0; s <= slices; s++)
            {
                double angle = 2.0 * Math.PI * s / slices;
                double x = origin.X + radius * Math.Cos(angle);
                double z = origin.Z + radius * Math.Sin(angle);

                mesh.Positions.Add(new Point3D(x, origin.Y + y, z));
            }
        }

        int ringSize = slices + 1;

        for (int i = 0; i < profile.Count - 1; i++)
        {
            for (int s = 0; s < slices; s++)
            {
                int a = i * ringSize + s;
                int b = a + 1;
                int c = (i + 1) * ringSize + s;
                int d = c + 1;

                mesh.TriangleIndices.Add(a);
                mesh.TriangleIndices.Add(c);
                mesh.TriangleIndices.Add(b);

                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(c);
                mesh.TriangleIndices.Add(d);
            }
        }

        return CreateModel(mesh, color);
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

    private static GeometryModel3D CreateModel(MeshGeometry3D mesh, Color baseColor)
    {
        var materialGroup = new MaterialGroup();

        var diffuse = new DiffuseMaterial(new SolidColorBrush(baseColor));
        var specular = new SpecularMaterial(
            new SolidColorBrush(Color.FromArgb(185, 255, 255, 255)),
            80);
        var emissive = new EmissiveMaterial(
            new SolidColorBrush(Color.FromArgb(18, 255, 244, 230)));

        materialGroup.Children.Add(diffuse);
        materialGroup.Children.Add(specular);
        materialGroup.Children.Add(emissive);

        return new GeometryModel3D
        {
            Geometry = mesh,
            Material = materialGroup,
            BackMaterial = materialGroup
        };
    }
}