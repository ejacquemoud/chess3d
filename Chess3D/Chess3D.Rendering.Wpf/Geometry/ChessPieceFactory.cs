using Chess3D.Rendering.Wpf.Enums;
using HelixToolkit.Wpf;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Chess3D.Rendering.Wpf.Geometry;

public static class ChessPieceFactory
{
    private const double PieceLift = 0.01;
    private const int DefaultSlices = 56;
    private const int SphereSlices = 28;
    private const int SphereStacks = 18;

    public static List<GeometryModel3D> CreatePieceModels(PieceType type, PieceColor pieceColor, Point3D baseCenter)
    {
        var c = new Point3D(baseCenter.X, baseCenter.Y + PieceLift, baseCenter.Z);

        return type switch
        {
            PieceType.Pawn => CreatePawn(c, pieceColor),
            //PieceType.Pawn => CreatePawnFromObj(c, pieceColor),
            PieceType.Rook => CreateRook(c, pieceColor),
            PieceType.Knight => CreateKnight(c, pieceColor),
            PieceType.Bishop => CreateBishop(c, pieceColor),
            PieceType.Queen => CreateQueen(c, pieceColor),
            PieceType.King => CreateKing(c, pieceColor),
            _ => new List<GeometryModel3D>()
        };
    }

    private static List<GeometryModel3D> CreatePawn(Point3D c, PieceColor color)
    {
        var body = new (double Radius, double Y)[]
        {
            (0.000, 0.000),
            (0.355, 0.018),
            (0.340, 0.050),
            (0.285, 0.095),
            (0.225, 0.145),
            (0.185, 0.215),
            (0.155, 0.310),
            (0.125, 0.430),
            (0.115, 0.525),
            (0.138, 0.585),
            (0.106, 0.635),
            (0.082, 0.662),
            (0.000, 0.676)
        };

        return new()
        {
            CreateLathedSolid(body, c, color, DefaultSlices),
            CreateLathedSolid(new (double Radius, double Y)[]
            {
                (0.000, 0.000),
                (0.100, 0.010),
                (0.093, 0.028),
                (0.078, 0.050),
                (0.000, 0.060)
            }, new Point3D(c.X, c.Y + 0.640, c.Z), color, DefaultSlices),
            CreateSphere(new Point3D(c.X, c.Y + 0.760, c.Z), 0.108, color, SphereSlices, SphereStacks)
        };
    }

    private static List<GeometryModel3D> CreateRook(Point3D c, PieceColor color)
    {
        var body = new (double Radius, double Y)[]
        {
            (0.000, 0.000),
            (0.365, 0.020),
            (0.345, 0.055),
            (0.290, 0.100),
            (0.240, 0.165),
            (0.214, 0.280),
            (0.205, 0.455),
            (0.208, 0.610),
            (0.238, 0.690),
            (0.285, 0.748),
            (0.270, 0.790),
            (0.000, 0.805)
        };

        var models = new List<GeometryModel3D>
        {
            CreateLathedSolid(body, c, color, DefaultSlices),
            CreateLathedSolid(new (double Radius, double Y)[]
            {
                (0.000, 0.000),
                (0.255, 0.010),
                (0.272, 0.028),
                (0.258, 0.050),
                (0.000, 0.062)
            }, new Point3D(c.X, c.Y + 0.752, c.Z), color, DefaultSlices)
        };

        int crenelCount = 6;
        double crenelRadius = 0.248;

        for (int i = 0; i < crenelCount; i++)
        {
            double angle = 2.0 * Math.PI * i / crenelCount;
            double x = c.X + crenelRadius * Math.Cos(angle);
            double z = c.Z + crenelRadius * Math.Sin(angle);

            models.Add(CreateBox(
                new Point3D(x, c.Y + 0.844, z),
                0.080,
                0.095,
                0.086,
                color));
        }

        return models;
    }

    private static List<GeometryModel3D> CreateBishop(Point3D c, PieceColor color)
    {
        var body = new (double Radius, double Y)[]
        {
            (0.000, 0.000),
            (0.345, 0.020),
            (0.315, 0.060),
            (0.238, 0.105),
            (0.190, 0.195),
            (0.145, 0.360),
            (0.128, 0.560),
            (0.154, 0.690),
            (0.205, 0.785),
            (0.165, 0.845),
            (0.118, 0.905),
            (0.000, 0.940)
        };

        var models = new List<GeometryModel3D>
        {
            CreateLathedSolid(body, c, color, DefaultSlices),
            CreateSphere(new Point3D(c.X, c.Y + 0.955, c.Z), 0.070, color, SphereSlices, SphereStacks)
        };

        models.Add(CreateBishopMiterCut(new Point3D(c.X, c.Y + 0.805, c.Z), color));

        return models;
    }

    private static List<GeometryModel3D> CreateQueen(Point3D c, PieceColor color)
    {
        var body = new (double Radius, double Y)[]
        {
            (0.000, 0.000),
            (0.360, 0.020),
            (0.330, 0.060),
            (0.250, 0.105),
            (0.198, 0.195),
            (0.155, 0.360),
            (0.138, 0.585),
            (0.165, 0.730),
            (0.220, 0.825),
            (0.182, 0.885),
            (0.125, 0.940),
            (0.000, 0.960)
        };

        var models = new List<GeometryModel3D>
        {
            CreateLathedSolid(body, c, color, DefaultSlices),
            CreateLathedSolid(new (double Radius, double Y)[]
            {
                (0.000, 0.000),
                (0.155, 0.010),
                (0.175, 0.026),
                (0.160, 0.048),
                (0.000, 0.060)
            }, new Point3D(c.X, c.Y + 0.905, c.Z), color, DefaultSlices),
            CreateSphere(new Point3D(c.X, c.Y + 1.045, c.Z), 0.060, color, SphereSlices, SphereStacks)
        };

        AddCrownBeads(models, c, 0.975, 0.150, 8, 0.030, color);

        return models;
    }

    private static List<GeometryModel3D> CreateKing(Point3D c, PieceColor color)
    {
        var body = new (double Radius, double Y)[]
        {
        (0.000, 0.000),
        (0.365, 0.020),
        (0.332, 0.060),
        (0.252, 0.108),
        (0.200, 0.200),
        (0.158, 0.390),
        (0.138, 0.640),
        (0.155, 0.760),
        (0.178, 0.840),
        (0.150, 0.900),
        (0.108, 0.955),
        (0.000, 0.980)
        };

        var models = new List<GeometryModel3D>
    {
        CreateLathedSolid(body, c, color, DefaultSlices),

        CreateLathedSolid(new (double Radius, double Y)[]
        {
            (0.000, 0.000),
            (0.090, 0.010),
            (0.100, 0.028),
            (0.088, 0.050),
            (0.060, 0.090),
            (0.048, 0.145),
            (0.000, 0.160)
        }, new Point3D(c.X, c.Y + 0.955, c.Z), color, DefaultSlices),

        CreateBox(
            new Point3D(c.X, c.Y + 1.120, c.Z),
            0.036,
            0.175,
            0.036,
            color),

        CreateBox(
            new Point3D(c.X, c.Y + 1.160, c.Z),
            0.145,
            0.024,
            0.036,
            color)
    };

        return models;
    }

    private static List<GeometryModel3D> CreateKnight(Point3D c, PieceColor color)
    {
        var models = new List<GeometryModel3D>
        {
            CreateLathedSolid(new (double Radius, double Y)[]
            {
                (0.000, 0.000),
                (0.360, 0.020),
                (0.335, 0.058),
                (0.275, 0.102),
                (0.228, 0.168),
                (0.194, 0.262),
                (0.176, 0.348),
                (0.165, 0.430),
                (0.000, 0.445)
            }, c, color, DefaultSlices),

            CreateLathedSolid(new (double Radius, double Y)[]
            {
                (0.000, 0.000),
                (0.185, 0.015),
                (0.196, 0.070),
                (0.180, 0.128),
                (0.142, 0.175),
                (0.000, 0.192)
            }, new Point3D(c.X, c.Y + 0.410, c.Z), color, DefaultSlices)
        };

        models.Add(CreateKnightNeck(
            new Point3D(c.X - 0.015, c.Y + 0.570, c.Z),
            0.22,
            0.34,
            0.16,
            color));

        models.Add(CreateKnightHead(
            new Point3D(c.X + 0.025, c.Y + 0.790, c.Z),
            color));

        models.Add(CreateKnightEar(
            new Point3D(c.X + 0.050, c.Y + 0.970, c.Z + 0.050),
            color,
            0.040,
            0.095,
            0.028));

        models.Add(CreateKnightEar(
            new Point3D(c.X + 0.050, c.Y + 0.970, c.Z - 0.050),
            color,
            0.040,
            0.095,
            0.028));

        models.Add(CreateKnightSnout(
            new Point3D(c.X + 0.152, c.Y + 0.785, c.Z),
            color));

        models.Add(CreateKnightMane(
            new Point3D(c.X - 0.060, c.Y + 0.790, c.Z),
            color));

        return models;
    }

    private static GeometryModel3D CreateKnightNeck(Point3D center, double width, double height, double depth, PieceColor color)
    {
        var points = new List<Point>
        {
            new(-0.45, -0.50),
            new(-0.25, -0.20),
            new(-0.06,  0.18),
            new( 0.08,  0.46),
            new( 0.20,  0.62),
            new( 0.10,  0.68),
            new(-0.12,  0.48),
            new(-0.28,  0.12),
            new(-0.40, -0.22)
        };

        return CreateExtrudedProfile(points, center, width, height, depth, color);
    }

    private static GeometryModel3D CreateKnightHead(Point3D center, PieceColor color)
    {
        var points = new List<Point>
        {
            new(-0.55, -0.42),
            new(-0.30, -0.30),
            new(-0.05, -0.08),
            new( 0.22,  0.18),
            new( 0.42,  0.44),
            new( 0.32,  0.62),
            new( 0.10,  0.78),
            new(-0.10,  0.72),
            new(-0.24,  0.48),
            new(-0.30,  0.15),
            new(-0.48, -0.06)
        };

        return CreateExtrudedProfile(points, center, 0.34, 0.42, 0.18, color);
    }

    private static GeometryModel3D CreateKnightEar(Point3D center, PieceColor color, double width, double height, double depth)
    {
        var points = new List<Point>
        {
            new(-0.40, -0.50),
            new( 0.00,  0.55),
            new( 0.40, -0.50)
        };

        return CreateExtrudedProfile(points, center, width, height, depth, color);
    }

    private static GeometryModel3D CreateKnightSnout(Point3D center, PieceColor color)
    {
        var points = new List<Point>
        {
            new(-0.48, -0.22),
            new( 0.20, -0.18),
            new( 0.42,  0.02),
            new( 0.20,  0.18),
            new(-0.46,  0.22)
        };

        return CreateExtrudedProfile(points, center, 0.14, 0.12, 0.16, color);
    }

    private static GeometryModel3D CreateKnightMane(Point3D center, PieceColor color)
    {
        var points = new List<Point>
        {
            new(-0.35, -0.50),
            new(-0.05, -0.42),
            new( 0.10, -0.12),
            new( 0.18,  0.20),
            new( 0.06,  0.48),
            new(-0.18,  0.55),
            new(-0.32,  0.20)
        };

        return CreateExtrudedProfile(points, center, 0.11, 0.35, 0.14, color);
    }

    private static GeometryModel3D CreateBishopMiterCut(Point3D center, PieceColor color)
    {
        var points = new List<Point>
        {
            new(-0.18, -0.52),
            new( 0.18, -0.22),
            new( 0.12,  0.52),
            new(-0.12,  0.22)
        };

        return CreateExtrudedProfile(points, center, 0.13, 0.24, 0.06, color);
    }

    private static void AddCrownBeads(
        List<GeometryModel3D> models,
        Point3D c,
        double y,
        double radius,
        int count,
        double beadRadius,
        PieceColor color)
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
                18,
                12));
        }
    }

    private static GeometryModel3D CreateLathedSolid(
        IReadOnlyList<(double Radius, double Y)> profile,
        Point3D origin,
        PieceColor color,
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

    private static GeometryModel3D CreateExtrudedProfile(
        IReadOnlyList<Point> profile,
        Point3D center,
        double width,
        double height,
        double depth,
        PieceColor color)
    {
        var mesh = new MeshGeometry3D();
        double halfDepth = depth / 2.0;

        for (int i = 0; i < profile.Count; i++)
        {
            var p = profile[i];
            mesh.Positions.Add(new Point3D(
                center.X + p.X * width,
                center.Y + p.Y * height,
                center.Z - halfDepth));
        }

        for (int i = 0; i < profile.Count; i++)
        {
            var p = profile[i];
            mesh.Positions.Add(new Point3D(
                center.X + p.X * width,
                center.Y + p.Y * height,
                center.Z + halfDepth));
        }

        int count = profile.Count;

        for (int i = 1; i < count - 1; i++)
        {
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(i);
            mesh.TriangleIndices.Add(i + 1);
        }

        for (int i = 1; i < count - 1; i++)
        {
            mesh.TriangleIndices.Add(count);
            mesh.TriangleIndices.Add(count + i + 1);
            mesh.TriangleIndices.Add(count + i);
        }

        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;

            int f0 = i;
            int f1 = next;
            int b0 = count + i;
            int b1 = count + next;

            mesh.TriangleIndices.Add(f0);
            mesh.TriangleIndices.Add(b0);
            mesh.TriangleIndices.Add(f1);

            mesh.TriangleIndices.Add(f1);
            mesh.TriangleIndices.Add(b0);
            mesh.TriangleIndices.Add(b1);
        }

        return CreateModel(mesh, color);
    }

    private static GeometryModel3D CreateBox(Point3D center, double sizeX, double sizeY, double sizeZ, PieceColor color)
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

    private static GeometryModel3D CreateSphere(Point3D center, double radius, PieceColor color, int slices, int stacks)
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

    private static GeometryModel3D CreateModel(MeshGeometry3D mesh, PieceColor pieceColor)
    {
        var material = CreatePieceMaterial(pieceColor);

        return new GeometryModel3D
        {
            Geometry = mesh,
            Material = material,
            BackMaterial = material
        };
    }

    private static Material CreatePieceMaterial(PieceColor pieceColor)
    {
        var group = new MaterialGroup();

        if (pieceColor == PieceColor.White)
        {
            group.Children.Add(new DiffuseMaterial(
                new SolidColorBrush(Color.FromRgb(232, 224, 210))));
            group.Children.Add(new SpecularMaterial(
                new SolidColorBrush(Color.FromArgb(170, 255, 248, 238)), 65));
            group.Children.Add(new EmissiveMaterial(
                new SolidColorBrush(Color.FromArgb(18, 255, 244, 230))));
        }
        else
        {
            group.Children.Add(new DiffuseMaterial(
                new SolidColorBrush(Color.FromRgb(50, 36, 28))));
            group.Children.Add(new SpecularMaterial(
                new SolidColorBrush(Color.FromArgb(145, 214, 190, 170)), 48));
            group.Children.Add(new EmissiveMaterial(
                new SolidColorBrush(Color.FromArgb(8, 90, 70, 60))));
        }

        group.Freeze();
        return group;
    }

    private static List<GeometryModel3D> CreatePawnFromObj(Point3D baseCenter, PieceColor color)
    {
        var fullPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets", "Models", "Chess", "Pawn.obj");

        var importer = new ModelImporter
        {
            DefaultMaterial = CreatePieceMaterial(color)
        };

        var loaded = importer.Load(fullPath);
        var result = new List<GeometryModel3D>();

        void Collect(Model3D model)
        {
            if (model is GeometryModel3D gm)
            {
                var clone = new GeometryModel3D
                {
                    Geometry = gm.Geometry,
                    Material = CreatePieceMaterial(color),
                    BackMaterial = CreatePieceMaterial(color)
                };

                var baseTransform = new Transform3DGroup();
                baseTransform.Children.Add(new ScaleTransform3D(0.42, 0.42, 0.42));
                baseTransform.Children.Add(new RotateTransform3D(
                    new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0)));
                baseTransform.Children.Add(new RotateTransform3D(
                    new AxisAngleRotation3D(new Vector3D(0, 1, 0), 180)));
                baseTransform.Children.Add(new TranslateTransform3D(
                    baseCenter.X - 1.86,
                    baseCenter.Y,
                    baseCenter.Z));

                var animationTranslate = new TranslateTransform3D(0, 0, 0);

                var tg = new Transform3DGroup();
                tg.Children.Add(baseTransform);
                tg.Children.Add(animationTranslate);

                clone.Transform = tg;
                result.Add(clone);
                return;
            }

            if (model is Model3DGroup group)
            {
                foreach (var child in group.Children)
                    Collect(child);
            }
        }

        Collect(loaded);
        return result;
    }
}