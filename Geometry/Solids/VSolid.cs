using System;
using System.Collections.Generic;
using Code2Viz.Canvas;

namespace Code2Viz.Geometry
{
    /// <summary>
    /// Represents a mesh with vertices and triangle indices for 3D rendering.
    /// </summary>
    public class VMesh
    {
        /// <summary>
        /// The vertices of the mesh.
        /// </summary>
        public List<VXYZ> Vertices { get; } = new List<VXYZ>();

        /// <summary>
        /// Triangle indices (groups of 3 indices into Vertices).
        /// </summary>
        public List<int> Triangles { get; } = new List<int>();

        /// <summary>
        /// Edge indices (groups of 2 indices into Vertices) for wireframe rendering.
        /// </summary>
        public List<(int, int)> Edges { get; } = new List<(int, int)>();

        public void AddVertex(VXYZ vertex) => Vertices.Add(vertex);
        public void AddVertex(double x, double y, double z) => Vertices.Add(new VXYZ(x, y, z));

        public void AddTriangle(int i0, int i1, int i2)
        {
            Triangles.Add(i0);
            Triangles.Add(i1);
            Triangles.Add(i2);
        }

        public void AddEdge(int i0, int i1) => Edges.Add((i0, i1));

        /// <summary>
        /// Adds edges for a triangle (for wireframe rendering).
        /// </summary>
        public void AddTriangleEdges(int i0, int i1, int i2)
        {
            AddEdge(i0, i1);
            AddEdge(i1, i2);
            AddEdge(i2, i0);
        }
    }

    /// <summary>
    /// Abstract base class for all 3D solid geometry.
    /// Follows Dynamo API conventions.
    /// </summary>
    public abstract class VSolid : Shape
    {
        /// <summary>
        /// The coordinate system defining the solid's position and orientation.
        /// </summary>
        public VCoordinateSystem CoordinateSystem { get; protected set; } = VCoordinateSystem.Identity;

        /// <summary>
        /// Gets the surface area of the solid.
        /// </summary>
        public abstract double Area { get; }

        /// <summary>
        /// Gets the volume of the solid.
        /// </summary>
        public abstract double Volume { get; }

        /// <summary>
        /// Gets the centroid (center of mass) of the solid.
        /// </summary>
        public virtual VXYZ Centroid => CoordinateSystem.Origin;

        /// <summary>
        /// Gets the mesh representation of this solid for rendering.
        /// </summary>
        public abstract VMesh GetMesh(int subdivisions = 16);

        /// <summary>
        /// Gets the edges of this solid for wireframe rendering.
        /// </summary>
        public virtual List<VLine3D> GetEdges(int subdivisions = 16)
        {
            var mesh = GetMesh(subdivisions);
            var edges = new List<VLine3D>();

            foreach (var (i0, i1) in mesh.Edges)
            {
                var line = VLine3D.CreateBound(mesh.Vertices[i0], mesh.Vertices[i1]);
                line.StrokeColor = StrokeColor;
                line.StrokeThickness = StrokeThickness;
                edges.Add(line);
            }

            return edges;
        }

        /// <summary>
        /// Draws this solid as a wireframe by projecting edges to 2D.
        /// </summary>
        public override void Draw()
        {
            var edges = GetEdges();
            foreach (var edge in edges)
            {
                edge.Draw();
            }
        }

        public override Shape Clone()
        {
            throw new NotImplementedException("Clone must be implemented by derived classes.");
        }

        public override void Move(VXYZ vector)
        {
            CoordinateSystem = CoordinateSystem.Translate(vector);
        }

        public override void Rotate(VPoint pivot, double angleDegrees)
        {
            // Rotate around Z-axis at the pivot point
            var pivot3D = pivot.AsVXYZ();
            var toOrigin = CoordinateSystem.Origin.Subtract(pivot3D);
            var transform = VTransform.CreateRotation(VXYZ.BasisZ, angleDegrees);
            var newOrigin = pivot3D.Add(transform.OfVector(toOrigin));
            CoordinateSystem = VCoordinateSystem.ByOriginVectors(newOrigin, CoordinateSystem.XAxis, CoordinateSystem.YAxis, CoordinateSystem.ZAxis)
                .Rotate(VXYZ.BasisZ, angleDegrees);
        }

        public override void Flip(VLine mirrorLine)
        {
            // Create mirror plane from line (vertical plane through line)
            var startPt = mirrorLine.Start.AsVXYZ();
            var endPt = mirrorLine.End.AsVXYZ();
            var lineDir = endPt.Subtract(startPt).Normalize();
            var normal = lineDir.CrossProduct(VXYZ.BasisZ).Normalize();
            var plane = VPlane.CreateByNormalAndOrigin(normal, startPt);
            var transform = VTransform.CreateReflection(plane);

            var newOrigin = transform.OfPoint(CoordinateSystem.Origin);
            var newX = transform.OfVector(CoordinateSystem.XAxis);
            var newY = transform.OfVector(CoordinateSystem.YAxis);
            var newZ = transform.OfVector(CoordinateSystem.ZAxis);

            CoordinateSystem = VCoordinateSystem.ByOriginVectors(newOrigin, newX, newY, newZ);
        }

        public override void Scale(VPoint center, double factor)
        {
            var center3D = center.AsVXYZ();
            var toOrigin = CoordinateSystem.Origin.Subtract(center3D);
            var newOrigin = center3D.Add(toOrigin.Multiply(factor));
            CoordinateSystem = VCoordinateSystem.ByOriginVectors(newOrigin, CoordinateSystem.XAxis, CoordinateSystem.YAxis, CoordinateSystem.ZAxis);
            OnScale(factor);
        }

        /// <summary>
        /// Called when the solid is scaled. Override to scale dimensions.
        /// </summary>
        protected virtual void OnScale(double factor) { }

        public override (VPoint min, VPoint max) GetBounds()
        {
            var mesh = GetMesh(8); // Use low subdivision for bounds
            if (mesh.Vertices.Count == 0)
                return (new VPoint(0, 0), new VPoint(0, 0));

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var v in mesh.Vertices)
            {
                minX = Math.Min(minX, v.X);
                minY = Math.Min(minY, v.Y);
                maxX = Math.Max(maxX, v.X);
                maxY = Math.Max(maxY, v.Y);
            }

            return (new VPoint(minX, minY), new VPoint(maxX, maxY));
        }

        /// <summary>
        /// Gets the 3D bounding box of this solid.
        /// </summary>
        public virtual (VXYZ min, VXYZ max) GetBounds3D()
        {
            var mesh = GetMesh(8);
            if (mesh.Vertices.Count == 0)
                return (VXYZ.Zero, VXYZ.Zero);

            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

            foreach (var v in mesh.Vertices)
            {
                minX = Math.Min(minX, v.X);
                minY = Math.Min(minY, v.Y);
                minZ = Math.Min(minZ, v.Z);
                maxX = Math.Max(maxX, v.X);
                maxY = Math.Max(maxY, v.Y);
                maxZ = Math.Max(maxZ, v.Z);
            }

            return (new VXYZ(minX, minY, minZ), new VXYZ(maxX, maxY, maxZ));
        }
    }
}
