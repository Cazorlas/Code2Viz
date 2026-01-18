using System;
using System.Collections.Generic;
using Code2Viz.Geometry;
using Code2Viz.Animation;
using Code2Viz.Console;

namespace CSharpSample
{
    public class Viz
    {
        public static void Main()
        {
            VizConsole.Log("C# Animation Sample - Points and Shapes");

            // Create a timeline for animations
            Timeline tl = new Timeline();
            tl.Duration = 8;
            tl.Repeat = true;

            // Animate a grid of points spreading out from origin
            int gridSize = 8;
            double spacing = 30;
            double offset = (gridSize - 1) * spacing / 2;

            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    // Create point at origin
                    VPoint p = new VPoint(0, 0);
                    p.FillColor = GetRainbowColor(x + y * gridSize, gridSize * gridSize);

                    // Target position in grid
                    double targetX = x * spacing - offset;
                    double targetY = y * spacing - offset;
                    VXYZ displacement = new VXYZ(targetX, targetY, 0);

                    // Stagger start time based on distance from center
                    double distance = Math.Sqrt(targetX * targetX + targetY * targetY);
                    double startTime = distance / 100;

                    // Add move animation
                    var anim = new MoveAnimation(p, displacement, startTime, 2);
                    anim.EasingFunction = EasingFunctions.EaseInQuad;
                    tl.AddAnimation(anim);
                }
            }

            // Add a rotating circle in the center
            VCircle centerCircle = new VCircle(0, 0, 20);
            centerCircle.StrokeColor = "White";
            centerCircle.StrokeThickness = 3;

            var rotateAnim = new RotateAnimation(centerCircle, new VPoint(0, 0), 360, 0, 8);
            tl.AddAnimation(rotateAnim);

            // Add pulsing circles at corners
            double cornerDist = offset + spacing / 2;
            var corners = new[] {
                new VPoint(-cornerDist, -cornerDist),
                new VPoint(cornerDist, -cornerDist),
                new VPoint(-cornerDist, cornerDist),
                new VPoint(cornerDist, cornerDist)
            };

            for (int i = 0; i < corners.Length; i++)
            {
                VCircle corner = new VCircle(corners[i], 15);
                corner.StrokeColor = GetRainbowColor(i * 16, 64);
                corner.FillColor = "Transparent";
                corner.StrokeThickness = 2;

               
            }

            VizConsole.Log($"Created {gridSize * gridSize} animated points");
            VizConsole.Log("Animation will loop continuously");

            tl.Play();
        }

        // Generate rainbow colors
        static string GetRainbowColor(int index, int total)
        {
            double hue = (double)index / total * 360;
            return HslToHex(hue, 0.8, 0.6);
        }

        // Convert HSL to hex color
        static string HslToHex(double h, double s, double l)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = l - c / 2;

            double r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            int ri = (int)((r + m) * 255);
            int gi = (int)((g + m) * 255);
            int bi = (int)((b + m) * 255);

            return $"#{ri:X2}{gi:X2}{bi:X2}";
        }
    }
}
