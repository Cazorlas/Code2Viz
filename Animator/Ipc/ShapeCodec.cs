using System.IO;
using C2VGeometry;

namespace Animator.Ipc;

/// <summary>
/// Serializes the shapes a frame produces for transport child-&gt;parent, and reconstructs them
/// on the parent so <c>AnimCanvas.SetShapes</c> can render them unchanged.
///
/// Only the shape types AnimCanvas actually draws are encoded (point/line/circle/ellipse/arc/
/// polyline/polygon); everything else is dropped, exactly as AnimCanvas drops it today. VRectangle
/// is a VPolygon subclass, so it rides the polygon path (rendered identically via its Points).
/// </summary>
public static class ShapeCodec
{
    private enum Kind : byte { Point = 1, Line, Circle, Ellipse, Arc, Polyline, Polygon }

    /// <summary>True if the shape is one AnimCanvas renders (and thus worth sending).</summary>
    public static bool IsRenderable(Shape s) =>
        s is VPoint or VLine or VCircle or VEllipse or VArc or VPolyline or VPolygon;

    /// <summary>Encodes one shape. Caller guarantees <see cref="IsRenderable"/> first.</summary>
    public static void Encode(BinaryWriter w, Shape s)
    {
        switch (s)
        {
            case VPoint p:
                w.Write((byte)Kind.Point); WriteCommon(w, s);
                w.Write(p.X); w.Write(p.Y);
                break;
            case VLine l:
                w.Write((byte)Kind.Line); WriteCommon(w, s);
                w.Write(l.Start.X); w.Write(l.Start.Y); w.Write(l.End.X); w.Write(l.End.Y);
                break;
            case VCircle c:
                w.Write((byte)Kind.Circle); WriteCommon(w, s);
                w.Write(c.Center.X); w.Write(c.Center.Y); w.Write(c.Radius);
                break;
            case VEllipse e:
                w.Write((byte)Kind.Ellipse); WriteCommon(w, s);
                w.Write(e.Center.X); w.Write(e.Center.Y); w.Write(e.RadiusX); w.Write(e.RadiusY);
                break;
            case VArc a:
                w.Write((byte)Kind.Arc); WriteCommon(w, s);
                w.Write(a.Center.X); w.Write(a.Center.Y); w.Write(a.Radius);
                w.Write(a.StartAngle); w.Write(a.EndAngle);
                break;
            case VPolyline pl:
                w.Write((byte)Kind.Polyline); WriteCommon(w, s);
                WritePoints(w, pl.Points);
                break;
            case VPolygon pg: // also catches VRectangle
                w.Write((byte)Kind.Polygon); WriteCommon(w, s);
                WritePoints(w, pg.Points);
                break;
            default:
                // Unreachable when callers honor IsRenderable; be defensive.
                throw new System.InvalidOperationException($"Not a renderable shape: {s.GetType().Name}");
        }
    }

    public static Shape Decode(BinaryReader r)
    {
        var kind = (Kind)r.ReadByte();
        var common = ReadCommon(r);

        Shape s = kind switch
        {
            Kind.Point => new VPoint(r.ReadDouble(), r.ReadDouble()),
            Kind.Line => new VLine(r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble()),
            Kind.Circle => new VCircle(r.ReadDouble(), r.ReadDouble(), r.ReadDouble()),
            Kind.Ellipse => new VEllipse(r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble()),
            Kind.Arc => new VArc(r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble()),
            Kind.Polyline => new VPolyline(ReadPoints(r)),
            Kind.Polygon => new VPolygon(ReadPoints(r)),
            _ => throw new InvalidDataException($"Unknown shape kind {kind}"),
        };

        s.Color = common.Color;
        s.FillColor = common.Fill;
        s.LineWeight = common.LineWeight;
        s.LineType = (LineType)common.LineType;
        s.Opacity = common.Opacity;
        s.OffsetX = common.OffsetX;
        s.OffsetY = common.OffsetY;
        return s;
    }

    private static void WriteCommon(BinaryWriter w, Shape s)
    {
        w.Write(s.Color ?? "");
        w.Write(s.FillColor ?? "");
        w.Write(s.LineWeight);
        w.Write((int)s.LineType);
        w.Write(s.Opacity);
        w.Write(s.OffsetX);
        w.Write(s.OffsetY);
    }

    private readonly record struct Common(
        string Color, string Fill, double LineWeight, int LineType, double Opacity, double OffsetX, double OffsetY);

    private static Common ReadCommon(BinaryReader r) => new(
        r.ReadString(), r.ReadString(), r.ReadDouble(), r.ReadInt32(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble());

    private static void WritePoints(BinaryWriter w, System.Collections.Generic.List<VXYZ> pts)
    {
        w.Write(pts.Count);
        foreach (var p in pts) { w.Write(p.X); w.Write(p.Y); }
    }

    private static VXYZ[] ReadPoints(BinaryReader r)
    {
        int n = r.ReadInt32();
        var pts = new VXYZ[n];
        for (int i = 0; i < n; i++) pts[i] = new VXYZ(r.ReadDouble(), r.ReadDouble());
        return pts;
    }
}
