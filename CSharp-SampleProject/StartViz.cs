using System;
using Code2Viz.Geometry;
using Code2Viz.Canvas;

namespace CSharpSample
{
    public class Viz
    {
        public static void Main()
        {
            Console.WriteLine("3D Solids Demo - Wireframe Rendering");
            Console.WriteLine("=====================================");

            // Create a cuboid (box)
            var box = VCuboid.ByLengths(60, 40, 30);
            box.StrokeColor = "Cyan";
            box.Draw();
            Console.WriteLine($"Cuboid: Volume={box.Volume:F1}, Area={box.Area:F1}");

            // Create a sphere
            var sphere = VSphere.ByCenterPointRadius(new VXYZ(120, 0, 0), 30);
            sphere.StrokeColor = "LimeGreen";
            sphere.Draw();
            Console.WriteLine($"Sphere: Volume={sphere.Volume:F1}, Area={sphere.Area:F1}");

            // Create a cylinder
            var cylinder = VCylinder.ByPointsRadius(
                new VXYZ(-100, 0, -30),
                new VXYZ(-100, 0, 50),
                25);
            cylinder.StrokeColor = "Orange";
            cylinder.Draw();
            Console.WriteLine($"Cylinder: Volume={cylinder.Volume:F1}, Area={cylinder.Area:F1}");

            // Create a cone
            var cone = VCone.ByPointsRadius(
                new VXYZ(0, 100, 0),
                new VXYZ(0, 100, 70),
                35);
            cone.StrokeColor = "Magenta";
            cone.Draw();
            Console.WriteLine($"Cone: Volume={cone.Volume:F1}, Area={cone.Area:F1}");

            // Create a truncated cone (frustum)
            var frustum = VCone.ByPointsRadii(
                new VXYZ(-100, 100, 0),
                new VXYZ(-100, 100, 60),
                40, 15);
            frustum.StrokeColor = "Yellow";
            frustum.Draw();
            Console.WriteLine($"Frustum: Volume={frustum.Volume:F1}, Area={frustum.Area:F1}");

            // Create another cuboid positioned using coordinate system
            var cs = VCoordinateSystem.ByOriginZAxis(
                new VXYZ(120, 100, 0),
                new VXYZ(1, 1, 1).Normalize());  // Tilted axis
            var tiltedBox = VCuboid.ByLengths(cs, 30, 30, 50);
            tiltedBox.StrokeColor = "White";
            tiltedBox.Draw();

            Console.WriteLine("\nAll solids rendered as wireframes.");
        }
    }
}
