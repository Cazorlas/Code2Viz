using System;

namespace Code2Viz.Geometry
{
    /// <summary>
    /// Represents a 3D cone or truncated cone (frustum) solid.
    /// Follows Dynamo API conventions with factory methods.
    /// </summary>
    public class VCone : VSolid
    {
        private double _startRadius;
        private double _endRadius;
        private double _height;

        /// <summary>
        /// Gets the start point (center of bottom cap).
        /// </summary>
        public VXYZ StartPoint => CoordinateSystem.ToWorld(0, 0, -_height / 2);

        /// <summary>
        /// Gets the end point (center of top cap or apex).
        /// </summary>
        public VXYZ EndPoint => CoordinateSystem.ToWorld(0, 0, _height / 2);

        /// <summary>
        /// Gets the radius at the start (bottom).
        /// </summary>
        public double StartRadius => _startRadius;

        /// <summary>
        /// Gets the radius at the end (top). Zero for a pointed cone.
        /// </summary>
        public double EndRadius => _endRadius;

        /// <summary>
        /// Gets the height of the cone.
        /// </summary>
        public double Height => _height;

        /// <summary>
        /// Gets the ratio of end radius to start radius.
        /// </summary>
        public double RadiusRatio => _startRadius > 0 ? _endRadius / _startRadius : 0;

        /// <summary>
        /// Gets whether this is a pointed cone (end radius is zero).
        /// </summary>
        public bool IsPointed => _endRadius < 1e-10;

        /// <summary>
        /// Gets the surface area of the cone (including caps).
        /// </summary>
        public override double Area
        {
            get
            {
                var slantHeight = Math.Sqrt(_height * _height + Math.Pow(_startRadius - _endRadius, 2));
                var lateralArea = Math.PI * (_startRadius + _endRadius) * slantHeight;
                var bottomCapArea = Math.PI * _startRadius * _startRadius;
                var topCapArea = Math.PI * _endRadius * _endRadius;
                return lateralArea + bottomCapArea + topCapArea;
            }
        }

        /// <summary>
        /// Gets the volume of the cone.
        /// </summary>
        public override double Volume
        {
            get
            {
                // Frustum volume formula: V = (π * h / 3) * (r1² + r1*r2 + r2²)
                return (Math.PI * _height / 3) * (_startRadius * _startRadius + _startRadius * _endRadius + _endRadius * _endRadius);
            }
        }

        private VCone(VCoordinateSystem cs, double startRadius, double endRadius, double height)
        {
            CoordinateSystem = cs;
            _startRadius = startRadius;
            _endRadius = endRadius;
            _height = height;
        }

        /// <summary>
        /// Creates a pointed cone from two points and a start radius.
        /// </summary>
        public static VCone ByPointsRadius(VXYZ startPoint, VXYZ endPoint, double startRadius)
        {
            return ByPointsRadii(startPoint, endPoint, startRadius, 0);
        }

        /// <summary>
        /// Creates a pointed cone from two points and a start radius.
        /// </summary>
        public static VCone ByPointsRadius(VPoint startPoint, VPoint endPoint, double startRadius)
        {
            return ByPointsRadius(startPoint.AsVXYZ(), endPoint.AsVXYZ(), startRadius);
        }

        /// <summary>
        /// Creates a truncated cone (frustum) from two points and two radii.
        /// </summary>
        public static VCone ByPointsRadii(VXYZ startPoint, VXYZ endPoint, double startRadius, double endRadius)
        {
            var axis = endPoint.Subtract(startPoint);
            var height = axis.GetLength();
            var center = startPoint.Add(axis.Multiply(0.5));
            var cs = VCoordinateSystem.ByOriginZAxis(center, axis);

            return new VCone(cs, startRadius, endRadius, height);
        }

        /// <summary>
        /// Creates a truncated cone (frustum) from two points and two radii.
        /// </summary>
        public static VCone ByPointsRadii(VPoint startPoint, VPoint endPoint, double startRadius, double endRadius)
        {
            return ByPointsRadii(startPoint.AsVXYZ(), endPoint.AsVXYZ(), startRadius, endRadius);
        }

        /// <summary>
        /// Creates a pointed cone in the specified coordinate system.
        /// </summary>
        public static VCone ByCoordinateSystemHeightRadius(VCoordinateSystem cs, double height, double radius)
        {
            return new VCone(cs, radius, 0, height);
        }

        /// <summary>
        /// Creates a truncated cone in the specified coordinate system.
        /// </summary>
        public static VCone ByCoordinateSystemHeightRadii(VCoordinateSystem cs, double height, double startRadius, double endRadius)
        {
            return new VCone(cs, startRadius, endRadius, height);
        }

        /// <summary>
        /// Creates a pointed cone at the origin along the Z-axis.
        /// </summary>
        public static VCone ByRadiusHeight(double radius, double height)
        {
            return new VCone(VCoordinateSystem.Identity, radius, 0, height);
        }

        /// <summary>
        /// Creates a truncated cone at the origin along the Z-axis.
        /// </summary>
        public static VCone ByRadiiHeight(double startRadius, double endRadius, double height)
        {
            return new VCone(VCoordinateSystem.Identity, startRadius, endRadius, height);
        }

        public override VMesh GetMesh(int subdivisions = 16)
        {
            var mesh = new VMesh();
            var segments = Math.Max(8, subdivisions);

            // Bottom center
            int bottomCenter = 0;
            mesh.AddVertex(CoordinateSystem.ToWorld(0, 0, -_height / 2));

            // Bottom ring vertices
            for (int i = 0; i < segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                double x = _startRadius * Math.Cos(angle);
                double y = _startRadius * Math.Sin(angle);
                mesh.AddVertex(CoordinateSystem.ToWorld(x, y, -_height / 2));
            }

            int bottomRingStart = 1;

            if (IsPointed)
            {
                // Apex
                int apex = mesh.Vertices.Count;
                mesh.AddVertex(CoordinateSystem.ToWorld(0, 0, _height / 2));

                // Bottom cap triangles
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    mesh.AddTriangle(bottomCenter, bottomRingStart + next, bottomRingStart + i);
                }

                // Side triangles (to apex)
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    mesh.AddTriangle(bottomRingStart + i, bottomRingStart + next, apex);
                }

                // Edges
                // Bottom circle
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    mesh.AddEdge(bottomRingStart + i, bottomRingStart + next);
                }

                // Lines to apex
                int edgeCount = Math.Min(8, segments);
                int step = segments / edgeCount;
                for (int i = 0; i < segments; i += step)
                {
                    mesh.AddEdge(bottomRingStart + i, apex);
                }
            }
            else
            {
                // Truncated cone (frustum)
                // Top center
                int topCenter = mesh.Vertices.Count;
                mesh.AddVertex(CoordinateSystem.ToWorld(0, 0, _height / 2));

                // Top ring vertices
                int topRingStart = mesh.Vertices.Count;
                for (int i = 0; i < segments; i++)
                {
                    double angle = 2 * Math.PI * i / segments;
                    double x = _endRadius * Math.Cos(angle);
                    double y = _endRadius * Math.Sin(angle);
                    mesh.AddVertex(CoordinateSystem.ToWorld(x, y, _height / 2));
                }

                // Bottom cap triangles
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    mesh.AddTriangle(bottomCenter, bottomRingStart + next, bottomRingStart + i);
                }

                // Top cap triangles
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    mesh.AddTriangle(topCenter, topRingStart + i, topRingStart + next);
                }

                // Side triangles
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    int b0 = bottomRingStart + i;
                    int b1 = bottomRingStart + next;
                    int t0 = topRingStart + i;
                    int t1 = topRingStart + next;

                    mesh.AddTriangle(b0, b1, t1);
                    mesh.AddTriangle(b0, t1, t0);
                }

                // Edges
                // Bottom circle
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    mesh.AddEdge(bottomRingStart + i, bottomRingStart + next);
                }

                // Top circle
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    mesh.AddEdge(topRingStart + i, topRingStart + next);
                }

                // Vertical edges
                int edgeCount = Math.Min(8, segments);
                int step = segments / edgeCount;
                for (int i = 0; i < segments; i += step)
                {
                    mesh.AddEdge(bottomRingStart + i, topRingStart + i);
                }
            }

            return mesh;
        }

        protected override void OnScale(double factor)
        {
            _startRadius *= factor;
            _endRadius *= factor;
            _height *= factor;
        }

        public override Shape Clone()
        {
            var clone = new VCone(CoordinateSystem, _startRadius, _endRadius, _height);
            CopyStyleTo(clone);
            return clone;
        }

        public override string ToString()
        {
            if (IsPointed)
                return $"VCone(StartRadius={StartRadius}, Height={Height}, Center={CoordinateSystem.Origin})";
            return $"VCone(StartRadius={StartRadius}, EndRadius={EndRadius}, Height={Height}, Center={CoordinateSystem.Origin})";
        }
    }
}
