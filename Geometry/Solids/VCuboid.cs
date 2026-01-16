using System;

namespace Code2Viz.Geometry
{
    /// <summary>
    /// Represents a 3D cuboid (box) solid.
    /// Follows Dynamo API conventions with factory methods.
    /// </summary>
    public class VCuboid : VSolid
    {
        private double _width;
        private double _length;
        private double _height;

        /// <summary>
        /// Gets the width of the cuboid (X dimension).
        /// </summary>
        public double Width => _width;

        /// <summary>
        /// Gets the length of the cuboid (Y dimension).
        /// </summary>
        public double Length => _length;

        /// <summary>
        /// Gets the height of the cuboid (Z dimension).
        /// </summary>
        public double Height => _height;

        /// <summary>
        /// Gets the surface area of the cuboid.
        /// </summary>
        public override double Area => 2 * (_width * _length + _width * _height + _length * _height);

        /// <summary>
        /// Gets the volume of the cuboid.
        /// </summary>
        public override double Volume => _width * _length * _height;

        private VCuboid(VCoordinateSystem cs, double width, double length, double height)
        {
            CoordinateSystem = cs;
            _width = width;
            _length = length;
            _height = height;
        }

        /// <summary>
        /// Creates a cuboid from two corner points (axis-aligned bounding box).
        /// </summary>
        public static VCuboid ByCorners(VXYZ lowPoint, VXYZ highPoint)
        {
            var width = Math.Abs(highPoint.X - lowPoint.X);
            var length = Math.Abs(highPoint.Y - lowPoint.Y);
            var height = Math.Abs(highPoint.Z - lowPoint.Z);

            var center = new VXYZ(
                (lowPoint.X + highPoint.X) / 2,
                (lowPoint.Y + highPoint.Y) / 2,
                (lowPoint.Z + highPoint.Z) / 2
            );

            return new VCuboid(VCoordinateSystem.ByOrigin(center), width, length, height);
        }

        /// <summary>
        /// Creates a cuboid from two corner points (axis-aligned bounding box).
        /// </summary>
        public static VCuboid ByCorners(VPoint lowPoint, VPoint highPoint)
        {
            return ByCorners(lowPoint.AsVXYZ(), highPoint.AsVXYZ());
        }

        /// <summary>
        /// Creates a cuboid at the origin with specified dimensions.
        /// </summary>
        public static VCuboid ByLengths(double width, double length, double height)
        {
            return new VCuboid(VCoordinateSystem.Identity, width, length, height);
        }

        /// <summary>
        /// Creates a cuboid at the specified origin with given dimensions.
        /// </summary>
        public static VCuboid ByLengths(VXYZ origin, double width, double length, double height)
        {
            return new VCuboid(VCoordinateSystem.ByOrigin(origin), width, length, height);
        }

        /// <summary>
        /// Creates a cuboid at the specified origin with given dimensions.
        /// </summary>
        public static VCuboid ByLengths(VPoint origin, double width, double length, double height)
        {
            return ByLengths(origin.AsVXYZ(), width, length, height);
        }

        /// <summary>
        /// Creates a cuboid in the specified coordinate system with given dimensions.
        /// </summary>
        public static VCuboid ByLengths(VCoordinateSystem coordinateSystem, double width, double length, double height)
        {
            return new VCuboid(coordinateSystem, width, length, height);
        }

        /// <summary>
        /// Gets the 8 corner vertices of the cuboid in world coordinates.
        /// </summary>
        public VXYZ[] GetVertices()
        {
            var hw = _width / 2;
            var hl = _length / 2;
            var hh = _height / 2;

            return new[]
            {
                CoordinateSystem.ToWorld(-hw, -hl, -hh), // 0: back-left-bottom
                CoordinateSystem.ToWorld( hw, -hl, -hh), // 1: back-right-bottom
                CoordinateSystem.ToWorld( hw,  hl, -hh), // 2: front-right-bottom
                CoordinateSystem.ToWorld(-hw,  hl, -hh), // 3: front-left-bottom
                CoordinateSystem.ToWorld(-hw, -hl,  hh), // 4: back-left-top
                CoordinateSystem.ToWorld( hw, -hl,  hh), // 5: back-right-top
                CoordinateSystem.ToWorld( hw,  hl,  hh), // 6: front-right-top
                CoordinateSystem.ToWorld(-hw,  hl,  hh), // 7: front-left-top
            };
        }

        public override VMesh GetMesh(int subdivisions = 16)
        {
            var mesh = new VMesh();
            var vertices = GetVertices();

            // Add vertices
            foreach (var v in vertices)
                mesh.AddVertex(v);

            // Add faces (2 triangles per face)
            // Bottom face (0, 1, 2, 3)
            mesh.AddTriangle(0, 2, 1);
            mesh.AddTriangle(0, 3, 2);

            // Top face (4, 5, 6, 7)
            mesh.AddTriangle(4, 5, 6);
            mesh.AddTriangle(4, 6, 7);

            // Front face (3, 2, 6, 7)
            mesh.AddTriangle(3, 6, 2);
            mesh.AddTriangle(3, 7, 6);

            // Back face (0, 1, 5, 4)
            mesh.AddTriangle(0, 1, 5);
            mesh.AddTriangle(0, 5, 4);

            // Left face (0, 3, 7, 4)
            mesh.AddTriangle(0, 7, 3);
            mesh.AddTriangle(0, 4, 7);

            // Right face (1, 2, 6, 5)
            mesh.AddTriangle(1, 2, 6);
            mesh.AddTriangle(1, 6, 5);

            // Add edges (12 edges of a cuboid)
            // Bottom edges
            mesh.AddEdge(0, 1);
            mesh.AddEdge(1, 2);
            mesh.AddEdge(2, 3);
            mesh.AddEdge(3, 0);

            // Top edges
            mesh.AddEdge(4, 5);
            mesh.AddEdge(5, 6);
            mesh.AddEdge(6, 7);
            mesh.AddEdge(7, 4);

            // Vertical edges
            mesh.AddEdge(0, 4);
            mesh.AddEdge(1, 5);
            mesh.AddEdge(2, 6);
            mesh.AddEdge(3, 7);

            return mesh;
        }

        protected override void OnScale(double factor)
        {
            _width *= factor;
            _length *= factor;
            _height *= factor;
        }

        public override Shape Clone()
        {
            var clone = new VCuboid(CoordinateSystem, _width, _length, _height);
            CopyStyleTo(clone);
            return clone;
        }

        public override string ToString()
        {
            return $"VCuboid(Width={Width}, Length={Length}, Height={Height}, Center={CoordinateSystem.Origin})";
        }
    }
}
