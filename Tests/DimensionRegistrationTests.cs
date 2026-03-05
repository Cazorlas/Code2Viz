using Code2Viz.Geometry;

namespace Code2Viz.Tests;

public class DimensionRegistrationTests
{
    [Fact]
    public void DoubleConstructor_CreatesInternalEndpoints_NotPlaced()
    {
        var dim = new VDimension(0, 0, 10, 0);

        Assert.False(dim.Point1.IsPlaced);
        Assert.False(dim.Point2.IsPlaced);
    }

    [Fact]
    public void GetDimensionGeometry_CreatesInternalHelperPoints_NotPlaced()
    {
        var dim = new VDimension(0, 0, 10, 0);

        var geom = dim.GetDimensionGeometry();

        Assert.False(geom.dimStart.IsPlaced);
        Assert.False(geom.dimEnd.IsPlaced);
        Assert.False(geom.textPos.IsPlaced);
        Assert.False(geom.ext1Start.IsPlaced);
        Assert.False(geom.ext1End.IsPlaced);
        Assert.False(geom.ext2Start.IsPlaced);
        Assert.False(geom.ext2End.IsPlaced);
    }
}
