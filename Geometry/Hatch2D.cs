using System;
using System.Collections.Generic;
using Code2Viz.Canvas;

namespace Code2Viz.Geometry;

/// <summary>
/// A hatch fill shape that applies a pattern within a closed boundary.
/// The boundary is defined by a polygon (list of points).
/// The pattern is defined by a HatchType.
/// </summary>
public class VHatch : Shape
{
    private List<VPoint> _boundary;
    private HatchType _pattern;
    private double _patternScale;
    private double _patternAngle;

    /// <summary>The closed boundary polygon points.</summary>
    public List<VPoint> Boundary
    {
        get => _boundary;
        set => _boundary = value ?? new List<VPoint>();
    }

    /// <summary>The hatch pattern definition.</summary>
    public HatchType Pattern
    {
        get => _pattern;
        set => _pattern = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Scale factor applied to the pattern. Default 1.0.</summary>
    public double PatternScale
    {
        get => _patternScale;
        set => _patternScale = value;
    }

    /// <summary>Additional rotation angle in degrees applied to the entire pattern. Default 0.</summary>
    public double PatternAngle
    {
        get => _patternAngle;
        set => _patternAngle = value;
    }

    /// <summary>
    /// Creates a hatch from a built-in pattern enum applied to a polygon boundary.
    /// </summary>
    public VHatch(VPolygon boundary, BuiltInHatch pattern, double scale = 1.0, double angle = 0.0)
        : this(boundary.Points, HatchType.GetBuiltIn(pattern), scale, angle) { }

    /// <summary>
    /// Creates a hatch from a built-in pattern name applied to a polygon boundary.
    /// </summary>
    public VHatch(VPolygon boundary, string patternName, double scale = 1.0, double angle = 0.0)
        : this(boundary.Points, HatchType.GetBuiltIn(patternName), scale, angle) { }

    /// <summary>
    /// Creates a hatch from a HatchType applied to a polygon boundary.
    /// </summary>
    public VHatch(VPolygon boundary, HatchType pattern, double scale = 1.0, double angle = 0.0)
        : this(boundary.Points, pattern, scale, angle) { }

    /// <summary>
    /// Creates a hatch from a built-in pattern enum applied to boundary points.
    /// </summary>
    public VHatch(List<VPoint> boundary, BuiltInHatch pattern, double scale = 1.0, double angle = 0.0)
        : this(boundary, HatchType.GetBuiltIn(pattern), scale, angle) { }

    /// <summary>
    /// Creates a hatch from a built-in pattern name applied to boundary points.
    /// </summary>
    public VHatch(List<VPoint> boundary, string patternName, double scale = 1.0, double angle = 0.0)
        : this(boundary, HatchType.GetBuiltIn(patternName), scale, angle) { }

    /// <summary>
    /// Creates a hatch from a custom HatchType applied to boundary points.
    /// </summary>
    public VHatch(List<VPoint> boundary, HatchType pattern, double scale = 1.0, double angle = 0.0)
    {
        _boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
        _pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        _patternScale = scale;
        _patternAngle = angle;
        Color = ShapeDefaults.GlobalColor ?? "Cyan";
        LineWeight = 1.0;
    }

    /// <summary>
    /// Creates a hatch using a custom pattern definition string in AutoCAD .pat format.
    /// </summary>
    public static VHatch FromDefinition(VPolygon boundary, string patDefinition, double scale = 1.0, double angle = 0.0)
    {
        var pattern = HatchType.Parse(patDefinition);
        return new VHatch(boundary.Points, pattern, scale, angle);
    }

    /// <summary>
    /// Creates a hatch using a custom pattern definition string applied to boundary points.
    /// </summary>
    public static VHatch FromDefinition(List<VPoint> boundary, string patDefinition, double scale = 1.0, double angle = 0.0)
    {
        var pattern = HatchType.Parse(patDefinition);
        return new VHatch(boundary, pattern, scale, angle);
    }

    /// <summary>
    /// Generates the hatch line segments clipped to the boundary.
    /// </summary>
    public List<(VPoint Start, VPoint End)> GenerateLines()
    {
        return HatchGenerator.Generate(_boundary, _pattern, _patternScale, _patternAngle);
    }

    public override Shape Clone()
    {
        var clonedBoundary = new List<VPoint>();
        foreach (var pt in _boundary)
            clonedBoundary.Add(pt.Clone());
        var clone = new VHatch(clonedBoundary, _pattern, _patternScale, _patternAngle);
        CopyStyleTo(clone);
        return clone;
    }

    public override void Move(VXYZ vector)
    {
        foreach (var pt in _boundary)
            pt.Move(vector);
    }

    public override void Rotate(VPoint pivot, double angleDegrees)
    {
        foreach (var pt in _boundary)
            pt.Rotate(pivot, angleDegrees);
        _patternAngle += angleDegrees;
    }

    public override void Flip(VLine mirrorLine)
    {
        foreach (var pt in _boundary)
            pt.Flip(mirrorLine);
    }

    public override void Scale(VPoint center, double factor)
    {
        foreach (var pt in _boundary)
            pt.Scale(center, factor);
        _patternScale *= Math.Abs(factor);
    }

    public override BoundingBox GetBounds()
    {
        if (_boundary.Count == 0)
            return new BoundingBox(VPoint.Internal(0, 0), VPoint.Internal(0, 0));

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var pt in _boundary)
        {
            if (pt.X < minX) minX = pt.X;
            if (pt.Y < minY) minY = pt.Y;
            if (pt.X > maxX) maxX = pt.X;
            if (pt.Y > maxY) maxY = pt.Y;
        }

        return new BoundingBox(VPoint.Internal(minX, minY), VPoint.Internal(maxX, maxY));
    }

    public override List<ControlPoint> GetControlPoints()
    {
        var bounds = GetBounds();
        return new List<ControlPoint>
        {
            new ControlPoint(ControlPointType.Move,
                (bounds.Min.X + bounds.Max.X) / 2,
                (bounds.Min.Y + bounds.Max.Y) / 2,
                "Center")
        };
    }

    public override bool Contains(VPoint point)
    {
        return IsPointInPolygon(point, _boundary);
    }

    private static bool IsPointInPolygon(VPoint point, List<VPoint> polygon)
    {
        bool inside = false;
        int j = polygon.Count - 1;
        for (int i = 0; i < polygon.Count; i++)
        {
            if ((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y) &&
                point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X)
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }

    public override string ToString() => $"VHatch({_pattern.Name}, Scale:{_patternScale}, Angle:{_patternAngle})";
}
