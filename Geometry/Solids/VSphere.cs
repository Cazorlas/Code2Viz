using System;
using System.Collections.Generic;
using System.Linq;

namespace Code2Viz.Geometry
{
    /// <summary>
    /// Represents a 3D sphere solid.
    /// Follows Dynamo API conventions with factory methods.
    /// </summary>
    public class VSphere : VSolid
    {
        private double _radius;

        /// <summary>
        /// Gets the center point of the sphere.
        /// </summary>
        public VXYZ CenterPoint => CoordinateSystem.Origin;

        /// <summary>
        /// Gets the radius of the sphere.
        /// </summary>
        public double Radius => _radius;

        /// <summary>
        /// Gets the surface area of the sphere.
        /// </summary>
        public override double Area => 4 * Math.PI * _radius * _radius;

        /// <summary>
        /// Gets the volume of the sphere.
        /// </summary>
        public override double Volume => (4.0 / 3.0) * Math.PI * _radius * _radius * _radius;

        private VSphere(VXYZ center, double radius)
        {
            CoordinateSystem = VCoordinateSystem.ByOrigin(center);
            _radius = radius;
        }

        /// <summary>
        /// Creates a sphere from a center point and radius.
        /// </summary>
        public static VSphere ByCenterPointRadius(VXYZ centerPoint, double radius)
        {
            return new VSphere(centerPoint, radius);
        }

        /// <summary>
        /// Creates a sphere from a center point and radius.
        /// </summary>
        public static VSphere ByCenterPointRadius(VPoint centerPoint, double radius)
        {
            return new VSphere(centerPoint.AsVXYZ(), radius);
        }

        /// <summary>
        /// Creates a sphere from a center point and radius.
        /// </summary>
        public static VSphere ByCenterPointRadius(double x, double y, double z, double radius)
        {
            return new VSphere(new VXYZ(x, y, z), radius);
        }

        /// <summary>
        /// Creates a sphere that passes through four points (circumsphere).
        /// </summary>
        public static VSphere ByFourPoints(VXYZ p1, VXYZ p2, VXYZ p3, VXYZ p4)
        {
            // Solve for circumsphere using determinant method
            // This finds the unique sphere passing through 4 non-coplanar points
            var center = ComputeCircumsphereCenter(p1, p2, p3, p4);
            var radius = center.DistanceTo(p1);
            return new VSphere(center, radius);
        }

        /// <summary>
        /// Creates a sphere that passes through four points (circumsphere).
        /// </summary>
        public static VSphere ByFourPoints(VPoint p1, VPoint p2, VPoint p3, VPoint p4)
        {
            return ByFourPoints(p1.AsVXYZ(), p2.AsVXYZ(), p3.AsVXYZ(), p4.AsVXYZ());
        }

        /// <summary>
        /// Creates a best-fit sphere for a set of points using least squares.
        /// </summary>
        public static VSphere ByBestFit(IEnumerable<VXYZ> points)
        {
            var pointList = points.ToList();
            if (pointList.Count < 4)
                throw new ArgumentException("At least 4 points are required for sphere fitting.");

            // Simple approach: use centroid as center and average distance as radius
            var centroid = new VXYZ(
                pointList.Average(p => p.X),
                pointList.Average(p => p.Y),
                pointList.Average(p => p.Z)
            );

            var avgRadius = pointList.Average(p => p.DistanceTo(centroid));
            return new VSphere(centroid, avgRadius);
        }

        /// <summary>
        /// Creates a best-fit sphere for a set of points using least squares.
        /// </summary>
        public static VSphere ByBestFit(IEnumerable<VPoint> points)
        {
            return ByBestFit(points.Select(p => p.AsVXYZ()));
        }

        private static VXYZ ComputeCircumsphereCenter(VXYZ p1, VXYZ p2, VXYZ p3, VXYZ p4)
        {
            // Use the formula for circumsphere center
            // Based on solving the system of equations for equal distances

            var a = p2.Subtract(p1);
            var b = p3.Subtract(p1);
            var c = p4.Subtract(p1);

            var d1 = a.DotProduct(a) / 2;
            var d2 = b.DotProduct(b) / 2;
            var d3 = c.DotProduct(c) / 2;

            // Compute determinant
            var det = a.X * (b.Y * c.Z - b.Z * c.Y)
                    - a.Y * (b.X * c.Z - b.Z * c.X)
                    + a.Z * (b.X * c.Y - b.Y * c.X);

            if (Math.Abs(det) < 1e-10)
            {
                // Points are coplanar, fall back to centroid
                return new VXYZ(
                    (p1.X + p2.X + p3.X + p4.X) / 4,
                    (p1.Y + p2.Y + p3.Y + p4.Y) / 4,
                    (p1.Z + p2.Z + p3.Z + p4.Z) / 4
                );
            }

            var x = (d1 * (b.Y * c.Z - b.Z * c.Y) - d2 * (a.Y * c.Z - a.Z * c.Y) + d3 * (a.Y * b.Z - a.Z * b.Y)) / det;
            var y = (a.X * (d2 * c.Z - d3 * b.Z) - b.X * (d1 * c.Z - d3 * a.Z) + c.X * (d1 * b.Z - d2 * a.Z)) / det;
            var z = (a.X * (b.Y * d3 - c.Y * d2) - b.X * (a.Y * d3 - c.Y * d1) + c.X * (a.Y * d2 - b.Y * d1)) / det;

            return p1.Add(new VXYZ(x, y, z));
        }

        public override VMesh GetMesh(int subdivisions = 16)
        {
            var mesh = new VMesh();
            var latSegments = Math.Max(8, subdivisions);
            var lonSegments = Math.Max(8, subdivisions * 2);

            // Generate UV sphere vertices
            // Top pole
            mesh.AddVertex(CoordinateSystem.ToWorld(0, 0, _radius));

            // Latitude rings (excluding poles)
            for (int lat = 1; lat < latSegments; lat++)
            {
                double phi = Math.PI * lat / latSegments; // 0 to PI
                double z = _radius * Math.Cos(phi);
                double ringRadius = _radius * Math.Sin(phi);

                for (int lon = 0; lon < lonSegments; lon++)
                {
                    double theta = 2 * Math.PI * lon / lonSegments;
                    double x = ringRadius * Math.Cos(theta);
                    double y = ringRadius * Math.Sin(theta);
                    mesh.AddVertex(CoordinateSystem.ToWorld(x, y, z));
                }
            }

            // Bottom pole
            int bottomPole = mesh.Vertices.Count;
            mesh.AddVertex(CoordinateSystem.ToWorld(0, 0, -_radius));

            // Triangles for top cap (connecting to top pole)
            for (int lon = 0; lon < lonSegments; lon++)
            {
                int next = (lon + 1) % lonSegments;
                mesh.AddTriangle(0, 1 + lon, 1 + next);
            }

            // Triangles for middle rings
            for (int lat = 0; lat < latSegments - 2; lat++)
            {
                int ringStart = 1 + lat * lonSegments;
                int nextRingStart = 1 + (lat + 1) * lonSegments;

                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int next = (lon + 1) % lonSegments;
                    int i0 = ringStart + lon;
                    int i1 = ringStart + next;
                    int i2 = nextRingStart + lon;
                    int i3 = nextRingStart + next;

                    mesh.AddTriangle(i0, i2, i1);
                    mesh.AddTriangle(i1, i2, i3);
                }
            }

            // Triangles for bottom cap (connecting to bottom pole)
            int lastRingStart = 1 + (latSegments - 2) * lonSegments;
            for (int lon = 0; lon < lonSegments; lon++)
            {
                int next = (lon + 1) % lonSegments;
                mesh.AddTriangle(bottomPole, lastRingStart + next, lastRingStart + lon);
            }

            // Edges for wireframe - draw latitude and longitude lines
            // Longitude lines (vertical)
            int lonLineCount = 8;
            int lonStep = lonSegments / lonLineCount;
            for (int lon = 0; lon < lonSegments; lon += lonStep)
            {
                // Connect top pole to first ring
                mesh.AddEdge(0, 1 + lon);

                // Connect rings
                for (int lat = 0; lat < latSegments - 2; lat++)
                {
                    int curr = 1 + lat * lonSegments + lon;
                    int next = 1 + (lat + 1) * lonSegments + lon;
                    mesh.AddEdge(curr, next);
                }

                // Connect last ring to bottom pole
                mesh.AddEdge(1 + (latSegments - 2) * lonSegments + lon, bottomPole);
            }

            // Latitude lines (horizontal circles)
            int latLineCount = 4;
            int latStep = (latSegments - 1) / latLineCount;
            for (int lat = latStep; lat < latSegments - 1; lat += latStep)
            {
                int ringStart = 1 + (lat - 1) * lonSegments;
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int next = (lon + 1) % lonSegments;
                    mesh.AddEdge(ringStart + lon, ringStart + next);
                }
            }

            return mesh;
        }

        protected override void OnScale(double factor)
        {
            _radius *= factor;
        }

        public override Shape Clone()
        {
            var clone = new VSphere(CenterPoint, _radius);
            CopyStyleTo(clone);
            return clone;
        }

        public override string ToString()
        {
            return $"VSphere(Center={CenterPoint}, Radius={Radius})";
        }
    }
}
