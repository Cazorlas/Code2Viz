using System;
using System.Collections.Generic;

namespace Code2Viz.Geometry
{
    /// <summary>
    /// Represents a 3D cylinder solid.
    /// Follows Dynamo API conventions with factory methods.
    /// </summary>
    public class VCylinder : VSolid
    {
        private double _radius;
        private double _height;

        /// <summary>
        /// Gets the radius of the cylinder.
        /// </summary>
        public double Radius => _radius;

        /// <summary>
        /// Gets the height of the cylinder.
        /// </summary>
        public double Height => _height;

        /// <summary>
        /// Gets the start point (center of bottom cap).
        /// </summary>
        public VXYZ StartPoint => CoordinateSystem.ToWorld(0, 0, -_height / 2);

        /// <summary>
        /// Gets the end point (center of top cap).
        /// </summary>
        public VXYZ EndPoint => CoordinateSystem.ToWorld(0, 0, _height / 2);

        /// <summary>
        /// Gets the axis line of the cylinder.
        /// </summary>
        public VLine3D Axis => VLine3D.CreateBound(StartPoint, EndPoint);

        /// <summary>
        /// Gets the surface area of the cylinder (including caps).
        /// </summary>
        public override double Area => 2 * Math.PI * _radius * (_radius + _height);

        /// <summary>
        /// Gets the volume of the cylinder.
        /// </summary>
        public override double Volume => Math.PI * _radius * _radius * _height;

        private VCylinder(VCoordinateSystem cs, double radius, double height)
        {
            CoordinateSystem = cs;
            _radius = radius;
            _height = height;
        }

        /// <summary>
        /// Creates a cylinder from two end points and a radius.
        /// </summary>
        public static VCylinder ByPointsRadius(VXYZ startPoint, VXYZ endPoint, double radius)
        {
            var axis = endPoint.Subtract(startPoint);
            var height = axis.GetLength();
            var center = startPoint.Add(axis.Multiply(0.5));
            var cs = VCoordinateSystem.ByOriginZAxis(center, axis);

            return new VCylinder(cs, radius, height);
        }

        /// <summary>
        /// Creates a cylinder from two end points and a radius.
        /// </summary>
        public static VCylinder ByPointsRadius(VPoint startPoint, VPoint endPoint, double radius)
        {
            return ByPointsRadius(startPoint.AsVXYZ(), endPoint.AsVXYZ(), radius);
        }

        /// <summary>
        /// Creates a cylinder at the origin along the Z-axis with specified radius and height.
        /// </summary>
        public static VCylinder ByRadiusHeight(double radius, double height)
        {
            return new VCylinder(VCoordinateSystem.Identity, radius, height);
        }

        /// <summary>
        /// Creates a cylinder in the specified coordinate system.
        /// </summary>
        public static VCylinder ByCoordinateSystemRadiusHeight(VCoordinateSystem cs, double radius, double height)
        {
            return new VCylinder(cs, radius, height);
        }

        public override VMesh GetMesh(int subdivisions = 16)
        {
            var mesh = new VMesh();
            var segments = Math.Max(8, subdivisions);

            // Generate vertices
            var bottomCenter = 0;
            var topCenter = 1;

            mesh.AddVertex(CoordinateSystem.ToWorld(0, 0, -_height / 2)); // Bottom center
            mesh.AddVertex(CoordinateSystem.ToWorld(0, 0, _height / 2));  // Top center

            // Bottom ring vertices (starting at index 2)
            for (int i = 0; i < segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                double x = _radius * Math.Cos(angle);
                double y = _radius * Math.Sin(angle);
                mesh.AddVertex(CoordinateSystem.ToWorld(x, y, -_height / 2));
            }

            // Top ring vertices (starting at index 2 + segments)
            for (int i = 0; i < segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                double x = _radius * Math.Cos(angle);
                double y = _radius * Math.Sin(angle);
                mesh.AddVertex(CoordinateSystem.ToWorld(x, y, _height / 2));
            }

            int bottomRingStart = 2;
            int topRingStart = 2 + segments;

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

            // Edges for wireframe
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

            // Vertical edges (only draw a few for clarity)
            int verticalEdgeCount = Math.Min(8, segments);
            int step = segments / verticalEdgeCount;
            for (int i = 0; i < segments; i += step)
            {
                mesh.AddEdge(bottomRingStart + i, topRingStart + i);
            }

            return mesh;
        }

        protected override void OnScale(double factor)
        {
            _radius *= factor;
            _height *= factor;
        }

        public override Shape Clone()
        {
            var clone = new VCylinder(CoordinateSystem, _radius, _height);
            CopyStyleTo(clone);
            return clone;
        }

        public override string ToString()
        {
            return $"VCylinder(Radius={Radius}, Height={Height}, Center={CoordinateSystem.Origin})";
        }
    }
}
