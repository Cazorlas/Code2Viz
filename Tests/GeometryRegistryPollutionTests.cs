using System;
using System.Collections.Generic;
using Xunit;
using C2VGeometry;

namespace Code2Viz.Tests;

/// <summary>
/// Regression guard: shapes that build an internal curve representation (VPolygon,
/// VRectangle, Region) must NOT auto-register those edge segments. Once a live
/// IShapeRegistry is attached (as in the app, where CanvasRenderer is the registry),
/// `new VLine(...)` inside BuildCurvesFromPoints would dump phantom edge shapes onto
/// the canvas — the shape's outline would render as separate default-colored lines.
/// The fix uses the non-registering VLine.Internal factory (see CLAUDE.md #10).
/// </summary>
[Collection("CanvasState")]
public class GeometryRegistryPollutionTests : IDisposable
{
    private sealed class CountingRegistry : IShapeRegistry
    {
        public readonly List<Shape> Shapes = new();
        public void Register(Shape s) => Shapes.Add(s);
        public void Unregister(Shape s) => Shapes.Remove(s);
        public void MoveAbove(Shape s, Shape r) { }
        public void MoveBehind(Shape s, Shape r) { }
    }

    private readonly CountingRegistry _reg = new();

    public GeometryRegistryPollutionTests() => Shape.DefaultRegistry = _reg;
    public void Dispose() => Shape.DefaultRegistry = null;

    [Fact]
    public void VPolygon_RegistersOnlyItself_NotItsEdges()
    {
        _reg.Shapes.Clear();
        _ = new VPolygon(new[] { new VXYZ(0, 0), new VXYZ(10, 0), new VXYZ(5, 8) });
        Assert.Single(_reg.Shapes); // polygon only — not polygon + 3 edge VLines
    }

    [Fact]
    public void VRectangle_RegistersOnlyItself_NotItsEdges()
    {
        _reg.Shapes.Clear();
        _ = new VRectangle(new VXYZ(0, 0), 20, 10);
        Assert.Single(_reg.Shapes); // rectangle only — not rectangle + 4 edge VLines
    }

    [Fact]
    public void Region_FromPolygon_DoesNotRegisterEdges()
    {
        var poly = new VPolygon(new[] { new VXYZ(0, 0), new VXYZ(10, 0), new VXYZ(5, 8) });
        _reg.Shapes.Clear();
        _ = Region.FromPolygon(poly);
        // FromPolygon uses the non-registering ctor and VLine.Internal edges, so it
        // touches the registry not at all. Before the fix it dumped the edge VLines.
        Assert.Empty(_reg.Shapes);
    }
}
