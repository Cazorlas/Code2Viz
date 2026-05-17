using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Code2Viz.Geometry;

namespace Code2Viz.Tests;

public class RayCasterTests
{
    [Fact]
    public void FindIntersection_HitsClosestOfTwoCircles()
    {
        Shape.AutoRegister = false;
        var near = new VCircle(new VPoint(10, 0), 1);
        var far  = new VCircle(new VPoint(50, 0), 1);

        var rc = new RayCaster(new Shape[] { far, near });
        var hit = rc.FindIntersection(new VXYZ(0, 0, 0), new VXYZ(1, 0, 0));

        Assert.NotNull(hit);
        Assert.Same(near, hit!.Value.Shape);
        Assert.Equal(9.0, hit.Value.Point.X, 6);
        Assert.Equal(0.0, hit.Value.Point.Y, 6);
        Assert.Equal(9.0, hit.Value.Distance, 6);
    }

    [Fact]
    public void FindIntersection_MissesWhenRayPointsAway()
    {
        Shape.AutoRegister = false;
        var c = new VCircle(new VPoint(10, 0), 1);
        var rc = new RayCaster(new Shape[] { c });

        var hit = rc.FindIntersection(new VXYZ(0, 0, 0), new VXYZ(-1, 0, 0));
        Assert.Null(hit);
    }

    [Fact]
    public void FindIntersection_HitsLineSegment()
    {
        Shape.AutoRegister = false;
        var line = new VLine(new VPoint(5, -5), new VPoint(5, 5));
        var rc = new RayCaster(new Shape[] { line });

        var hit = rc.FindIntersection(new VXYZ(0, 0, 0), new VXYZ(1, 0, 0));

        Assert.NotNull(hit);
        Assert.Same(line, hit!.Value.Shape);
        Assert.Equal(5.0, hit.Value.Point.X, 6);
        Assert.Equal(0.0, hit.Value.Point.Y, 6);
        Assert.Equal(5.0, hit.Value.Distance, 6);
    }

    [Fact]
    public void FindIntersection_PrunesToCorrectShapeAmongMany()
    {
        Shape.AutoRegister = false;
        var shapes = new List<Shape>();
        for (int x = 0; x < 50; x++)
        for (int y = 0; y < 50; y++)
            shapes.Add(new VCircle(new VPoint(x * 10, y * 10), 0.4));
        // A single circle on the off-grid row (y=7) — the only shape the ray meets.
        var onlyTarget = new VCircle(new VPoint(300, 7), 0.4);
        shapes.Add(onlyTarget);

        var rc = new RayCaster(shapes);
        var hit = rc.FindIntersection(new VXYZ(-5, 7, 0), new VXYZ(1, 0, 0));

        Assert.NotNull(hit);
        Assert.Same(onlyTarget, hit!.Value.Shape);
        Assert.Equal(299.6, hit.Value.Point.X, 3);
    }

    [Fact]
    public void FindIntersection_ReturnsNullForDegenerateDirection()
    {
        Shape.AutoRegister = false;
        var rc = new RayCaster(new Shape[] { new VCircle(new VPoint(0, 0), 1) });
        Assert.Null(rc.FindIntersection(new VXYZ(5, 0, 0), new VXYZ(0, 0, 0)));
    }

    [Fact]
    public void Constructor_AcceptsEmptyCollection()
    {
        Shape.AutoRegister = false;
        var rc = new RayCaster(Array.Empty<Shape>());
        Assert.Equal(0, rc.Count);
        Assert.Null(rc.FindIntersection(new VXYZ(0, 0, 0), new VXYZ(1, 0, 0)));
    }

    [Fact]
    public void FindIntersection_RespectsArcAngleRange()
    {
        Shape.AutoRegister = false;
        var upper = new VArc(new VPoint(0, 0), 5, 0, 180);
        var rc = new RayCaster(new Shape[] { upper });

        var up = rc.FindIntersection(new VXYZ(0, 0, 0), new VXYZ(0, 1, 0));
        Assert.NotNull(up);
        Assert.Same(upper, up!.Value.Shape);
        Assert.Equal(5.0, up.Value.Point.Y, 6);

        var down = rc.FindIntersection(new VXYZ(0, 0, 0), new VXYZ(0, -1, 0));
        Assert.Null(down);
    }

    [Fact]
    public void FindIntersection_HitsRectangleEdge()
    {
        Shape.AutoRegister = false;
        var rect = new VRectangle(new VPoint(10, -5), 10, 10);
        var rc = new RayCaster(new Shape[] { rect });

        var hit = rc.FindIntersection(new VXYZ(0, 0, 0), new VXYZ(1, 0, 0));
        Assert.NotNull(hit);
        Assert.Same(rect, hit!.Value.Shape);
        Assert.Equal(10.0, hit.Value.Point.X, 6);
    }

    [Fact]
    public void FindIntersection_HitsEllipseAtMinorAxis()
    {
        Shape.AutoRegister = false;
        var ellipse = new VEllipse(new VPoint(0, 0), 10, 3);
        var rc = new RayCaster(new Shape[] { ellipse });

        var hit = rc.FindIntersection(new VXYZ(0, 0, 0), new VXYZ(0, 1, 0));
        Assert.NotNull(hit);
        Assert.Same(ellipse, hit!.Value.Shape);
        Assert.Equal(3.0, hit.Value.Point.Y, 6);
    }

    [Fact]
    public void FindIntersection_DoesNotAlterAutoRegisterState()
    {
        Shape.AutoRegister = true;
        var line = new VLine(new VPoint(5, -5), new VPoint(5, 5));
        var rc = new RayCaster(new Shape[] { line });

        Assert.True(Shape.AutoRegister);
        var _ = rc.FindIntersection(new VXYZ(0, 0, 0), new VXYZ(1, 0, 0));
        Assert.True(Shape.AutoRegister);
    }

    // -- New API: maxDistance --------------------------------------------------

    [Fact]
    public void FindIntersection_MaxDistance_RejectsFarHits()
    {
        Shape.AutoRegister = false;
        var c = new VCircle(new VPoint(100, 0), 1);
        var rc = new RayCaster(new Shape[] { c });

        Assert.Null(rc.FindIntersection(new VXYZ(0, 0, 0), new VXYZ(1, 0, 0), maxDistance: 50));
        Assert.NotNull(rc.FindIntersection(new VXYZ(0, 0, 0), new VXYZ(1, 0, 0), maxDistance: 200));
    }

    [Fact]
    public void FindIntersection_MaxDistance_PicksNearestWithinRange()
    {
        Shape.AutoRegister = false;
        var near = new VCircle(new VPoint(10, 0), 1);
        var far  = new VCircle(new VPoint(100, 0), 1);
        var rc = new RayCaster(new Shape[] { far, near });

        var hit = rc.FindIntersection(new VXYZ(0, 0, 0), new VXYZ(1, 0, 0), maxDistance: 50);
        Assert.NotNull(hit);
        Assert.Same(near, hit!.Value.Shape);
    }

    // -- New API: HasIntersection (any-hit) -----------------------------------

    [Fact]
    public void HasIntersection_ReturnsTrueWhenAnyShapeIsHit()
    {
        Shape.AutoRegister = false;
        var c = new VCircle(new VPoint(10, 0), 1);
        var rc = new RayCaster(new Shape[] { c });

        Assert.True(rc.HasIntersection(new VXYZ(0, 0, 0), new VXYZ(1, 0, 0)));
        Assert.False(rc.HasIntersection(new VXYZ(0, 0, 0), new VXYZ(-1, 0, 0)));
    }

    [Fact]
    public void HasIntersection_RespectsMaxDistance()
    {
        Shape.AutoRegister = false;
        var c = new VCircle(new VPoint(100, 0), 1);
        var rc = new RayCaster(new Shape[] { c });

        Assert.False(rc.HasIntersection(new VXYZ(0, 0, 0), new VXYZ(1, 0, 0), maxDistance: 50));
        Assert.True(rc.HasIntersection(new VXYZ(0, 0, 0), new VXYZ(1, 0, 0), maxDistance: 200));
    }

    // -- New API: batch FindIntersections -------------------------------------

    [Fact]
    public void FindIntersections_BatchReturnsResultsAlignedWithInput()
    {
        Shape.AutoRegister = false;
        var c1 = new VCircle(new VPoint(10, 0), 1);
        var c2 = new VCircle(new VPoint(0, 10), 1);
        var rc = new RayCaster(new Shape[] { c1, c2 });

        var queries = new[]
        {
            new RayQuery(new VXYZ(0, 0, 0), new VXYZ(1, 0, 0)), // hits c1
            new RayQuery(new VXYZ(0, 0, 0), new VXYZ(0, 1, 0)), // hits c2
            new RayQuery(new VXYZ(0, 0, 0), new VXYZ(-1, 0, 0)) // miss
        };

        var results = rc.FindIntersections(queries);
        Assert.Equal(3, results.Length);
        Assert.Same(c1, results[0]!.Value.Shape);
        Assert.Same(c2, results[1]!.Value.Shape);
        Assert.Null(results[2]);
    }

    [Fact]
    public void FindIntersections_ParallelMatchesSequential()
    {
        Shape.AutoRegister = false;
        var rng = new Random(42);
        var shapes = new List<Shape>();
        for (int i = 0; i < 200; i++)
        {
            double x = rng.NextDouble() * 100 - 50;
            double y = rng.NextDouble() * 100 - 50;
            shapes.Add(new VCircle(new VPoint(x, y), 0.5 + rng.NextDouble()));
        }
        var rc = new RayCaster(shapes);

        var queries = Enumerable.Range(0, 500)
            .Select(i => new RayQuery(
                new VXYZ(rng.NextDouble() * 100 - 50, rng.NextDouble() * 100 - 50, 0),
                new VXYZ(rng.NextDouble() * 2 - 1, rng.NextDouble() * 2 - 1, 0)))
            .ToList();

        var seq = rc.FindIntersections(queries, parallel: false);
        var par = rc.FindIntersections(queries, parallel: true);

        Assert.Equal(seq.Length, par.Length);
        for (int i = 0; i < seq.Length; i++)
        {
            Assert.Equal(seq[i].HasValue, par[i].HasValue);
            if (seq[i].HasValue)
            {
                Assert.Same(seq[i]!.Value.Shape, par[i]!.Value.Shape);
                Assert.Equal(seq[i]!.Value.Distance, par[i]!.Value.Distance, 9);
            }
        }
    }

    // -- New API: Refit --------------------------------------------------------

    [Fact]
    public void Refit_PicksUpShapeMovement()
    {
        Shape.AutoRegister = false;
        var circle = new VCircle(new VPoint(10, 0), 1);
        var rc = new RayCaster(new Shape[] { circle });

        var hitBefore = rc.FindIntersection(new VXYZ(0, 0, 0), new VXYZ(1, 0, 0));
        Assert.Equal(9.0, hitBefore!.Value.Point.X, 6);

        // Move the circle. Without Refit() the cached AABB is stale and the
        // ray-AABB cull will reject it; Refit() restores correctness.
        circle.Center = new VPoint(20, 0);
        rc.Refit();

        var hitAfter = rc.FindIntersection(new VXYZ(0, 0, 0), new VXYZ(1, 0, 0));
        Assert.NotNull(hitAfter);
        Assert.Same(circle, hitAfter!.Value.Shape);
        Assert.Equal(19.0, hitAfter.Value.Point.X, 6);
    }

    [Fact]
    public void Refit_HandlesShapeMovingOutOfPath()
    {
        Shape.AutoRegister = false;
        var circle = new VCircle(new VPoint(10, 0), 1);
        var rc = new RayCaster(new Shape[] { circle });

        circle.Center = new VPoint(10, 100); // way above the ray's path
        rc.Refit();

        Assert.Null(rc.FindIntersection(new VXYZ(0, 0, 0), new VXYZ(1, 0, 0)));
    }
}
