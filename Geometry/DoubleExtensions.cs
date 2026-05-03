using System;

namespace Code2Viz.Geometry;

/// <summary>
/// Extension methods for <see cref="double"/> angle conversions.
/// </summary>
public static class DoubleExtensions
{
    /// <summary>
    /// Converts an angle from degrees to radians.
    /// </summary>
    public static double ToRadians(this double degrees) => degrees * Math.PI / 180.0;

    /// <summary>
    /// Converts an angle from radians to degrees.
    /// </summary>
    public static double ToDegrees(this double radians) => radians * 180.0 / Math.PI;
}
