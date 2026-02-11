namespace FSharpSample

open System
open System.Collections.Generic
open VizDsl
open Code2Viz.Geometry
open Code2Viz.Animation
open Code2Viz.Console

/// Code2Viz Sample Project - Comprehensive F# Examples
///
/// This module demonstrates all geometry types, their properties, methods,
/// and animation capabilities using F#'s functional style and the VizDsl.
///
/// Uncomment individual examples in Main() to see them in action.
module Viz =

    // ═══════════════════════════════════════════════════════════════════════
    // VPOINT - Point in 2D Space
    // ═══════════════════════════════════════════════════════════════════════
    let PointExamples() =
        VizConsole.Log("=== VPoint Examples (F#) ===")

        // --- Creating Points ---

        // Using VizDsl function
        let p1 = point 0.0 0.0            // Origin
        let p2 = point 50.0 30.0          // Point at (50, 30)
        let p3 = point -40.0 60.0         // Negative coordinates

        // Using constructor directly
        let p4 = VPoint(25.0, -20.0)

        // --- Point Properties ---
        VizConsole.Log(sprintf "P2 coordinates: X=%.1f, Y=%.1f" p2.X p2.Y)

        // --- Point Methods ---

        // DistanceTo: Calculate distance between points
        let dist = p1.DistanceTo(p2)
        VizConsole.Log(sprintf "Distance from p1 to p2: %.2f" dist)

        // Clone: Create an independent copy
        let p5 = p2.Clone() :?> VPoint

        // --- Styling Points ---
        p1.Color <- "Red"
        p2.Color <- "Blue"
        p3.Color <- "Green"

        // --- Point with Label ---
        let labeledPoint = point 0.0 -50.0
        labeledPoint.Color <- "Purple"

        text 10.0 -55.0 "Origin"
        |> withTextColor "Purple"
        |> withHeight 12.0
        |> ignore

    // ═══════════════════════════════════════════════════════════════════════
    // VLINE - Line Segment
    // ═══════════════════════════════════════════════════════════════════════
    let LineExamples() =
        VizConsole.Log("=== VLine Examples (F#) ===")

        // --- Creating Lines with VizDsl ---

        // From coordinates: line x1 y1 x2 y2
        let line1 = line -50.0 0.0 50.0 0.0        // Horizontal line
        let line2 = line 0.0 -50.0 0.0 50.0        // Vertical line

        // From points: lineFromPoints
        let line3 = lineFromPoints (point -40.0 -40.0) (point 40.0 40.0)

        // --- Line Properties ---
        VizConsole.Log(sprintf "Line1 Start: (%.1f, %.1f)" line1.Start.X line1.Start.Y)
        VizConsole.Log(sprintf "Line1 End: (%.1f, %.1f)" line1.End.X line1.End.Y)
        VizConsole.Log(sprintf "Line1 Length: %.2f" (line1.GetLength()))
        VizConsole.Log(sprintf "Line1 Midpoint: (%.1f, %.1f)" line1.MidPoint.X line1.MidPoint.Y)

        // --- Line Methods ---

        // Evaluate: Get point at parameter t (0=start, 1=end)
        let midPoint = line1.Evaluate(0.5)
        let quarterPoint = line1.Evaluate(0.25)
        VPoint(midPoint.X, midPoint.Y).Color <- "Red"

        // --- Styling with Pipeline ---
        line1
        |> withStroke "Red"
        |> withThickness 3.0
        |> ignore

        line2
        |> withStroke "Blue"
        |> withThickness 2.0
        |> ignore
        line2.LineType <- LineType.Dashed

        line3
        |> withStroke "Green"
        |> withThickness 2.0
        |> ignore

    // ═══════════════════════════════════════════════════════════════════════
    // VCIRCLE - Circle
    // ═══════════════════════════════════════════════════════════════════════
    let CircleExamples() =
        VizConsole.Log("=== VCircle Examples (F#) ===")

        // --- Creating Circles ---

        // Using VizDsl: circle x y radius
        let circle1 = circle 0.0 0.0 50.0

        // Using circleAt: circleAt centerPoint radius
        let circle2 = circleAt (point 80.0 0.0) 30.0

        // Direct constructor
        let circle3 = VCircle(-80.0, 0.0, 25.0)

        // --- Circle Properties ---
        VizConsole.Log(sprintf "Circle1 Center: (%.1f, %.1f)" circle1.Center.X circle1.Center.Y)
        VizConsole.Log(sprintf "Circle1 Radius: %.1f" circle1.Radius)
        VizConsole.Log(sprintf "Circle1 Diameter: %.1f" (circle1.Radius * 2.0))
        VizConsole.Log(sprintf "Circle1 Circumference: %.2f" circle1.Circumference)
        VizConsole.Log(sprintf "Circle1 Area: %.2f" circle1.Area)

        // --- Circle Methods ---

        // PointAtParameter: Get point at normalized parameter (0=right, 0.25=top)
        let topPoint = circle1.PointAtParameter(0.25)
        let rightPoint = circle1.PointAtParameter(0.0)
        VPoint(topPoint.X, topPoint.Y).Color <- "Red"
        VPoint(rightPoint.X, rightPoint.Y).Color <- "Blue"

        // --- Styling with Pipeline ---
        circle1
        |> withStroke "DarkBlue"
        |> withThickness 2.0
        |> ignore

        circle2
        |> withStroke "Green"
        |> withFill "LightGreen"
        |> withOpacity 0.5
        |> ignore

        circle3
        |> withStroke "Orange"
        |> ignore
        circle3.LineType <- LineType.Dashed

    // ═══════════════════════════════════════════════════════════════════════
    // VARC - Circular Arc
    // ═══════════════════════════════════════════════════════════════════════
    let ArcExamples() =
        VizConsole.Log("=== VArc Examples (F#) ===")

        // --- Creating Arcs ---

        // arc cx cy radius startAngle endAngle (angles in degrees)
        let arc1 = arc 0.0 0.0 50.0 0.0 90.0       // Quarter circle
        let arc2 = arc 0.0 0.0 40.0 45.0 225.0     // Half circle
        let arc3 = arc 0.0 0.0 30.0 0.0 270.0      // Three-quarter circle

        // --- Arc Properties ---
        VizConsole.Log(sprintf "Arc1 Center: (%.1f, %.1f)" arc1.Center.X arc1.Center.Y)
        VizConsole.Log(sprintf "Arc1 Radius: %.1f" arc1.Radius)
        VizConsole.Log(sprintf "Arc1 Start Angle: %.0f deg" arc1.StartAngle)
        VizConsole.Log(sprintf "Arc1 End Angle: %.0f deg" arc1.EndAngle)
        VizConsole.Log(sprintf "Arc1 Length: %.2f" (arc1.GetLength()))

        // --- Arc Methods ---
        let midArc = arc1.Evaluate(0.5)
        VPoint(midArc.X, midArc.Y).Color <- "Red"

        // --- Styling ---
        arc1 |> withStroke "Blue" |> withThickness 3.0 |> ignore
        arc2 |> withStroke "Green" |> withThickness 2.0 |> ignore
        arc2.LineType <- LineType.Dotted
        arc3 |> withStroke "Orange" |> withThickness 2.0 |> ignore

    // ═══════════════════════════════════════════════════════════════════════
    // VELLIPSE - Ellipse
    // ═══════════════════════════════════════════════════════════════════════
    let EllipseExamples() =
        VizConsole.Log("=== VEllipse Examples (F#) ===")

        // --- Creating Ellipses ---

        // ellipse x y radiusX radiusY
        let ellipse1 = ellipse 0.0 0.0 60.0 30.0   // Wider than tall
        let ellipse2 = ellipse 0.0 0.0 25.0 50.0   // Taller than wide

        // --- Ellipse Properties ---
        VizConsole.Log(sprintf "Ellipse1 Center: (%.1f, %.1f)" ellipse1.Center.X ellipse1.Center.Y)
        VizConsole.Log(sprintf "Ellipse1 RadiusX: %.1f" ellipse1.RadiusX)
        VizConsole.Log(sprintf "Ellipse1 RadiusY: %.1f" ellipse1.RadiusY)
        VizConsole.Log(sprintf "Ellipse1 Area: %.2f" ellipse1.Area)
        VizConsole.Log(sprintf "Ellipse1 Circumference: %.2f" ellipse1.Circumference)

        // --- Ellipse Methods ---
        let rightPoint = ellipse1.Evaluate(0.0)
        let topPoint = ellipse1.Evaluate(0.25)
        VPoint(rightPoint.X, rightPoint.Y).Color <- "Red"
        VPoint(topPoint.X, topPoint.Y).Color <- "Blue"

        // --- Styling ---
        ellipse1
        |> withStroke "Purple"
        |> withFill "Lavender"
        |> withThickness 2.0
        |> withOpacity 0.3
        |> ignore

        ellipse2
        |> withStroke "Teal"
        |> ignore
        ellipse2.LineType <- LineType.Dashed

    // ═══════════════════════════════════════════════════════════════════════
    // VRECTANGLE - Rectangle
    // ═══════════════════════════════════════════════════════════════════════
    let RectangleExamples() =
        VizConsole.Log("=== VRectangle Examples (F#) ===")

        // --- Creating Rectangles ---

        // rectangle x y width height (x,y is corner)
        let rect1 = rectangle -40.0 -20.0 80.0 40.0

        // From corner point
        let rect2 = rectangleAt (point -40.0 40.0) 80.0 30.0

        // Using square shorthand
        let sq = square 60.0 40.0 30.0

        // --- Rectangle Properties ---
        VizConsole.Log(sprintf "Rect1 Corner: (%.1f, %.1f)" rect1.Corner.X rect1.Corner.Y)
        VizConsole.Log(sprintf "Rect1 Width: %.1f" rect1.Width)
        VizConsole.Log(sprintf "Rect1 Height: %.1f" rect1.Height)
        VizConsole.Log(sprintf "Rect1 Perimeter: %.1f" (rect1.GetLength()))

        // --- Styling ---
        rect1
        |> withStroke "DarkBlue"
        |> withThickness 2.0
        |> ignore

        rect2
        |> withStroke "DarkGreen"
        |> withFill "LightGreen"
        |> withOpacity 0.5
        |> ignore

        sq
        |> withStroke "Orange"
        |> ignore
        sq.LineType <- LineType.DashDot

    // ═══════════════════════════════════════════════════════════════════════
    // VPOLYGON - Closed Polygon
    // ═══════════════════════════════════════════════════════════════════════
    let PolygonExamples() =
        VizConsole.Log("=== VPolygon Examples (F#) ===")

        // --- Creating Polygons ---

        // Using VizDsl polygon (list of tuples)
        let triangle = polygon [
            (0.0, 40.0)
            (-35.0, -20.0)
            (35.0, -20.0)
        ]

        // Pentagon using functional generation
        let pentagonPoints =
            [0..4]
            |> List.map (fun i ->
                let angle = Math.PI / 2.0 + float i * 2.0 * Math.PI / 5.0
                (80.0 + 30.0 * cos angle, 0.0 + 30.0 * sin angle))
        let pentagon = polygon pentagonPoints

        // Hexagon using pointsOnCircle helper
        let hexPoints = pointsOnCircle (point -80.0 0.0) 25.0 6
        let hexagon = polygonFromPoints hexPoints

        // --- Polygon Properties ---
        VizConsole.Log(sprintf "Triangle Points: %d" triangle.Points.Count)
        VizConsole.Log(sprintf "Triangle Area: %.2f" triangle.Area)
        VizConsole.Log(sprintf "Triangle Perimeter: %.2f" (triangle.GetLength()))
        VizConsole.Log(sprintf "Triangle SignedArea: %.2f" triangle.SignedArea)

        // --- Styling ---
        triangle
        |> withStroke "Red"
        |> withFill "LightCoral"
        |> withThickness 2.0
        |> withOpacity 0.4
        |> ignore

        pentagon
        |> withStroke "Blue"
        |> withFill "LightBlue"
        |> withOpacity 0.4
        |> ignore

        hexagon
        |> withStroke "Green"
        |> ignore
        hexagon.LineType <- LineType.Dotted

    // ═══════════════════════════════════════════════════════════════════════
    // VPOLYLINE - Open Path of Line Segments
    // ═══════════════════════════════════════════════════════════════════════
    let PolylineExamples() =
        VizConsole.Log("=== VPolyline Examples (F#) ===")

        // --- Creating Polylines ---

        // Zigzag pattern using list
        let zigzag = polyline [
            (-60.0, -20.0)
            (-30.0, 20.0)
            (0.0, -20.0)
            (30.0, 20.0)
            (60.0, -20.0)
        ]

        // Staircase using functional generation
        let stairPoints =
            [0..5]
            |> List.collect (fun i ->
                [(float (-50 + i * 20), float (40 + i * 10))
                 (float (-50 + (i + 1) * 20), float (40 + i * 10))])
        let staircase = polyline stairPoints

        // --- Polyline Properties ---
        VizConsole.Log(sprintf "Zigzag Points: %d" zigzag.Points.Count)
        VizConsole.Log(sprintf "Zigzag Length: %.2f" (zigzag.GetLength()))

        // --- Polyline Methods ---
        let midPoint = zigzag.PointAtParameter(0.5)
        VPoint(midPoint.X, midPoint.Y).Color <- "Red"

        // --- Styling ---
        zigzag
        |> withStroke "Blue"
        |> withThickness 3.0
        |> ignore

        staircase
        |> withStroke "Orange"
        |> withThickness 2.0
        |> ignore
        staircase.LineType <- LineType.Dashed

    // ═══════════════════════════════════════════════════════════════════════
    // VBEZIER - Cubic Bezier Curve
    // ═══════════════════════════════════════════════════════════════════════
    let BezierExamples() =
        VizConsole.Log("=== VBezier Examples (F#) ===")

        // --- Creating Bezier Curves ---

        // Cubic Bezier: P0 (start), P1 (control1), P2 (control2), P3 (end)
        let bezier1 = VBezier(
            VPoint(-60.0, 0.0),    // P0
            VPoint(-30.0, 50.0),   // P1
            VPoint(30.0, -50.0),   // P2
            VPoint(60.0, 0.0)      // P3
        )

        // S-curve
        let bezier2 = VBezier(
            VPoint(-50.0, -40.0),
            VPoint(50.0, -40.0),
            VPoint(-50.0, 40.0),
            VPoint(50.0, 40.0)
        )

        // --- Bezier Properties ---
        VizConsole.Log(sprintf "Bezier1 Start: (%.1f, %.1f)" bezier1.StartPoint.X bezier1.StartPoint.Y)
        VizConsole.Log(sprintf "Bezier1 End: (%.1f, %.1f)" bezier1.EndPoint.X bezier1.EndPoint.Y)
        VizConsole.Log(sprintf "Bezier1 Length: %.2f" (bezier1.GetLength()))

        // --- Bezier Methods ---
        let midBezier = bezier1.Evaluate(0.5)
        VPoint(midBezier.X, midBezier.Y).Color <- "Red"

        // --- Visualize Control Points ---
        VPoint(bezier1.P1.X, bezier1.P1.Y).Color <- "Gray"
        VPoint(bezier1.P2.X, bezier1.P2.Y).Color <- "Gray"

        // Control handles
        let handle1 = lineFromPoints bezier1.P0 bezier1.P1
        handle1.Color <- "LightGray"
        handle1.LineType <- LineType.Dotted

        let handle2 = lineFromPoints bezier1.P3 bezier1.P2
        handle2.Color <- "LightGray"
        handle2.LineType <- LineType.Dotted

        // --- Styling ---
        bezier1.Color <- "Blue"
        bezier1.LineWeight <- 3.0

        bezier2.Color <- "Purple"
        bezier2.LineWeight <- 2.0

    // ═══════════════════════════════════════════════════════════════════════
    // VSPLINE - Smooth Curve Through Points
    // ═══════════════════════════════════════════════════════════════════════
    let SplineExamples() =
        VizConsole.Log("=== VSpline Examples (F#) ===")

        // --- Creating Splines ---

        let splinePoints = [
            VPoint(-60.0, 0.0)
            VPoint(-30.0, 40.0)
            VPoint(0.0, -20.0)
            VPoint(30.0, 30.0)
            VPoint(60.0, 0.0)
        ]
        let spline = VSpline(splinePoints)

        // Wave using functional generation
        let wavePoints =
            [0..8]
            |> List.map (fun i ->
                let x = -80.0 + float i * 20.0
                let y = 30.0 * sin(float i * Math.PI / 2.0)
                VPoint(x, y - 60.0))
        let wave = VSpline(wavePoints)

        // --- Spline Properties ---
        VizConsole.Log(sprintf "Spline ControlPoints: %d" spline.ControlPoints.Count)
        VizConsole.Log(sprintf "Spline Length: %.2f" (spline.GetLength()))

        // --- Spline Methods ---
        let quarterPoint = spline.Evaluate(0.25)
        VPoint(quarterPoint.X, quarterPoint.Y).Color <- "Red"

        // --- Visualize Control Points ---
        splinePoints
        |> List.iter (fun pt ->
            let marker = VPoint(pt.X, pt.Y)
            marker.Color <- "Gray")

        // --- Styling ---
        spline.Color <- "DarkGreen"
        spline.LineWeight <- 3.0

        wave.Color <- "Teal"
        wave.LineWeight <- 2.0

    // ═══════════════════════════════════════════════════════════════════════
    // VTEXT - Text Label
    // ═══════════════════════════════════════════════════════════════════════
    let TextExamples() =
        VizConsole.Log("=== VText Examples (F#) ===")

        // --- Creating Text ---

        // Using VizDsl: text x y content
        let text1 =
            text 0.0 50.0 "Hello, Code2Viz!"
            |> withTextColor "DarkBlue"
            |> withHeight 24.0

        // Multiline text
        let text2 =
            text 0.0 0.0 "Line 1\nLine 2\nLine 3"
            |> withTextColor "Gray"
            |> withHeight 14.0

        // Large title
        let title =
            text 0.0 80.0 "GEOMETRY DEMO"
            |> withTextColor "Purple"
            |> withHeight 32.0

        // Annotation with reference point
        let refPoint = point 10.0 -50.0
        refPoint.Color <- "Green"

        text 20.0 -50.0 "<- This is a point"
        |> withTextColor "Green"
        |> withHeight 10.0
        |> ignore

    // ═══════════════════════════════════════════════════════════════════════
    // VARROW - Arrow (Line with Arrowhead)
    // ═══════════════════════════════════════════════════════════════════════
    let ArrowExamples() =
        VizConsole.Log("=== VArrow Examples (F#) ===")

        // --- Creating Arrows ---

        // Using VizDsl: arrow x1 y1 x2 y2
        let arrow1 = arrow -50.0 0.0 50.0 0.0       // Horizontal
        let arrow2 = arrowFromPoints (point 0.0 -40.0) (point 0.0 40.0)  // Vertical
        let arrow3 = arrow -40.0 -40.0 40.0 40.0   // Diagonal
        let arrow4 = arrow 40.0 -40.0 -40.0 40.0

        // --- Arrow Properties ---
        VizConsole.Log(sprintf "Arrow1 Start: (%.1f, %.1f)" arrow1.Start.X arrow1.Start.Y)
        VizConsole.Log(sprintf "Arrow1 End: (%.1f, %.1f)" arrow1.End.X arrow1.End.Y)
        VizConsole.Log(sprintf "Arrow1 Length: %.2f" (arrow1.Start.DistanceTo(arrow1.End)))

        // --- Styling ---
        arrow1 |> withStroke "Red" |> withThickness 2.0 |> ignore
        arrow2 |> withStroke "Blue" |> withThickness 2.0 |> ignore
        arrow3 |> withStroke "Green" |> withThickness 2.0 |> ignore
        arrow3.LineType <- LineType.Dashed
        arrow4 |> withStroke "Orange" |> withThickness 2.0 |> ignore

    // ═══════════════════════════════════════════════════════════════════════
    // VDIMENSION - Dimension Line with Measurement
    // ═══════════════════════════════════════════════════════════════════════
    let DimensionExamples() =
        VizConsole.Log("=== VDimension Examples (F#) ===")

        // Create a rectangle to dimension
        let rect = rectangle -40.0 -25.0 80.0 50.0
        rect.Color <- "Gray"

        // --- Creating Dimensions ---

        // Width dimension
        let widthDim = VDimension(VPoint(-40.0, -25.0), VPoint(40.0, -25.0))
        widthDim.Offset <- 15.0

        // Height dimension
        let heightDim = VDimension(VPoint(40.0, -25.0), VPoint(40.0, 25.0))
        heightDim.Offset <- 15.0

        // --- Dimension Properties ---
        VizConsole.Log(sprintf "Width Dimension: %.2f" widthDim.Distance)
        VizConsole.Log(sprintf "Height Dimension: %.2f" heightDim.Distance)

        // --- Styling ---
        widthDim.Color <- "Blue"
        heightDim.Color <- "Blue"

    // ═══════════════════════════════════════════════════════════════════════
    // VGROUP - Group of Shapes
    // ═══════════════════════════════════════════════════════════════════════
    let GroupExamples() =
        VizConsole.Log("=== VGroup Examples (F#) ===")

        // --- Creating a Smiley Face Group ---

        let face = circle 0.0 0.0 40.0
        face.Color <- "Gold"
        face.FillColor <- "Yellow"
        face.Opacity <- 0.8

        let leftEye = circle -15.0 10.0 5.0
        leftEye.Color <- "Black"
        leftEye.FillColor <- "Black"

        let rightEye = circle 15.0 10.0 5.0
        rightEye.Color <- "Black"
        rightEye.FillColor <- "Black"

        let smile = arc 0.0 -5.0 20.0 200.0 340.0
        smile.Color <- "Black"
        smile.LineWeight <- 3.0

        // Group them (cast to Shape list for F#)
        let smiley = VGroup(ResizeArray<Shape>([face :> Shape; leftEye :> Shape; rightEye :> Shape; smile :> Shape]))

        // Add nose to group
        let nose = line 0.0 5.0 0.0 -5.0
        nose.Color <- "Black"
        smiley.Add(nose)

        VizConsole.Log(sprintf "Smiley group contains %d shapes" smiley.Shapes.Count)

        // Another group
        let circle2 = circle 80.0 0.0 20.0
        circle2.Color <- "Red"
        let sq = rectangle 65.0 -15.0 30.0 30.0
        sq.Color <- "Blue"
        let group2 = VGroup(ResizeArray<Shape>([circle2 :> Shape; sq :> Shape]))
        ()

    // ═══════════════════════════════════════════════════════════════════════
    // VGRID - Grid of Points
    // ═══════════════════════════════════════════════════════════════════════
    let GridExamples() =
        VizConsole.Log("=== VGrid Examples (F#) ===")

        // --- Creating Grid ---
        let grid1 = VGrid(VPoint(0.0, 0.0), 8, 6, 25.0, 25.0, true)

        // --- Grid Properties ---
        VizConsole.Log(sprintf "Grid XCount: %d" grid1.XCount)
        VizConsole.Log(sprintf "Grid YCount: %d" grid1.YCount)
        VizConsole.Log(sprintf "Grid XSpacing: %.1f" grid1.XSpacing)
        VizConsole.Log(sprintf "Grid YSpacing: %.1f" grid1.YSpacing)
        VizConsole.Log(sprintf "Grid Total Points: %d" grid1.Count)

        // --- Styling ---
        grid1.Color <- "LightGray"
        grid1.LineWeight <- 0.5
        grid1.ApplyStyle()

        // Access individual points
        let centerPoint = grid1.[3, 2]
        centerPoint.Color <- "Red"

        // Highlight a row
        let row = grid1.GetRow(0)
        row |> Seq.iter (fun pt -> pt.Color <- "Blue")

    // ═══════════════════════════════════════════════════════════════════════
    // BOOLEAN OPERATIONS - Union, Intersection, Difference
    // ═══════════════════════════════════════════════════════════════════════
    let BooleanOperationsExamples() =
        VizConsole.Log("=== Boolean Operations Examples (F#) ===")

        // Create two overlapping polygons
        let square1 = polygon [
            (-30.0, -30.0); (30.0, -30.0)
            (30.0, 30.0); (-30.0, 30.0)
        ]

        let square2 = polygon [
            (0.0, 0.0); (60.0, 0.0)
            (60.0, 60.0); (0.0, 60.0)
        ]

        // Show original shapes
        square1
        |> withStroke "Blue"
        |> withFill "LightBlue"
        |> withOpacity 0.3
        |> ignore

        square2
        |> withStroke "Red"
        |> withFill "LightCoral"
        |> withOpacity 0.3
        |> ignore

        // --- Union ---
        let unionResult = BooleanOps.Union(square1, square2)
        match unionResult with
        | null -> ()
        | result -> VizConsole.Log(sprintf "Union: %d points" result.Points.Count)

        // --- Intersection ---
        let intersectResults = BooleanOps.Intersect(square1, square2)
        VizConsole.Log(sprintf "Intersection: %d polygon(s)" intersectResults.Count)
        intersectResults
        |> Seq.iter (fun poly ->
            poly.Color <- "Green"
            poly.FillColor <- "LightGreen"
            poly.Opacity <- 0.5)

        // --- Difference ---
        let diffResults = BooleanOps.Difference(square1, square2)
        VizConsole.Log(sprintf "Difference: %d polygon(s)" diffResults.Count)

        // --- Offset Polygon ---
        let tri = polygon [(-100.0, -40.0); (-60.0, -40.0); (-80.0, 0.0)]
        tri.Color <- "Purple"

        let offsetResults = BooleanOps.OffsetPolygon(tri, 5.0)
        offsetResults
        |> Seq.iter (fun poly ->
            poly.Color <- "Violet"
            poly.LineType <- LineType.Dashed)

        // --- Point in Polygon ---
        let testPoint = point 15.0 15.0
        let inside = BooleanOps.PointInPolygon(square1, testPoint)
        VizConsole.Log(sprintf "Point (15,15) inside square1: %b" inside)
        testPoint.Color <- if inside then "Green" else "Red"

    // ═══════════════════════════════════════════════════════════════════════
    // ANIMATIONS - Bringing Shapes to Life
    // ═══════════════════════════════════════════════════════════════════════
    let AnimationExamples() =
        VizConsole.Log("=== Animation Examples (F#) ===")

        // Create animator
        let animator = Animator()
        animator.Repeat <- true
        animator.Speed <- 1.0

        // --- Create shapes ---
        let myCircle =
            circle -60.0 0.0 20.0
            |> withStroke "Blue"
            |> withFill "LightBlue"

        let mySquare = polygon [
            (-10.0, -10.0); (10.0, -10.0)
            (10.0, 10.0); (-10.0, 10.0)
        ]
        mySquare |> withStroke "Red" |> withFill "LightCoral" |> ignore

        let myText =
            text 40.0 0.0 "Animated!"
            |> withTextColor "Green"
            |> withHeight 16.0

        // ═══════════════════════════════════════════════════════════════════
        // DRAW ANIMATION
        // ═══════════════════════════════════════════════════════════════════
        animator.AddToAnimations(DrawAnimation(myCircle, 1.5))

        // ═══════════════════════════════════════════════════════════════════
        // MOVE ANIMATION
        // ═══════════════════════════════════════════════════════════════════
        animator.AddToAnimations(MoveAnimation(mySquare, VXYZ(50.0, 30.0, 0.0), 1.0))

        // ═══════════════════════════════════════════════════════════════════
        // ROTATE ANIMATION
        // ═══════════════════════════════════════════════════════════════════
        animator.AddToAnimations(RotateAnimation(mySquare, VPoint(0.0, 0.0), 360.0, 2.0))

        // ═══════════════════════════════════════════════════════════════════
        // FADE IN/OUT ANIMATIONS
        // ═══════════════════════════════════════════════════════════════════
        animator.AddToAnimations(FadeInAnimation(myText, 1.0))
        animator.Pause(0.5)
        animator.AddToAnimations(FadeOutAnimation(myText, 1.0))

        // ═══════════════════════════════════════════════════════════════════
        // PARALLEL ANIMATIONS - Add individually for F# compatibility
        // ═══════════════════════════════════════════════════════════════════
        animator.AddToAnimations(MoveAnimation(myCircle, VXYZ(30.0, 0.0, 0.0), 1.0))
        animator.AddToAnimations(RotateAnimation(mySquare, VPoint(0.0, 0.0), 180.0, 1.0))

        VizConsole.Log(sprintf "Total duration: %.2f seconds" animator.Duration)
        animator.Animate()

    // ═══════════════════════════════════════════════════════════════════════
    // EASING FUNCTIONS - Control Animation Timing
    // ═══════════════════════════════════════════════════════════════════════
    let EasingFunctionsExamples() =
        VizConsole.Log("=== Easing Functions Demo (F#) ===")

        let animator = Animator()
        animator.Repeat <- true

        let startX = -80.0
        let endX = 80.0
        let duration = 2.0
        let displacement = VXYZ(endX - startX, 0.0, 0.0)

        // Linear - Red circle
        let c1 = VCircle(startX, 60.0, 10.0)
        c1.Color <- "Red"
        c1.FillColor <- "Red"
        let m1 = MoveAnimation(c1, displacement, duration)
        m1.EasingFunction <- EasingFunctions.Linear
        let l1 = VText(-110.0, 60.0, "Linear")
        l1.Color <- "Gray"
        l1.Height <- 10.0

        // EaseIn - Orange circle
        let c2 = VCircle(startX, 30.0, 10.0)
        c2.Color <- "Orange"
        c2.FillColor <- "Orange"
        let m2 = MoveAnimation(c2, displacement, duration)
        m2.EasingFunction <- EasingFunctions.EaseInQuad
        let l2 = VText(-110.0, 30.0, "EaseIn")
        l2.Color <- "Gray"
        l2.Height <- 10.0

        // EaseOut - Yellow circle
        let c3 = VCircle(startX, 0.0, 10.0)
        c3.Color <- "Yellow"
        c3.FillColor <- "Yellow"
        let m3 = MoveAnimation(c3, displacement, duration)
        m3.EasingFunction <- EasingFunctions.EaseOutQuad
        let l3 = VText(-110.0, 0.0, "EaseOut")
        l3.Color <- "Gray"
        l3.Height <- 10.0

        // EaseInOut - Green circle
        let c4 = VCircle(startX, -30.0, 10.0)
        c4.Color <- "Green"
        c4.FillColor <- "Green"
        let m4 = MoveAnimation(c4, displacement, duration)
        m4.EasingFunction <- EasingFunctions.EaseInOutQuad
        let l4 = VText(-110.0, -30.0, "EaseInOut")
        l4.Color <- "Gray"
        l4.Height <- 10.0

        // Cubic - Blue circle
        let c5 = VCircle(startX, -60.0, 10.0)
        c5.Color <- "Blue"
        c5.FillColor <- "Blue"
        let m5 = MoveAnimation(c5, displacement, duration)
        m5.EasingFunction <- EasingFunctions.EaseInOutCubic
        let l5 = VText(-110.0, -60.0, "Cubic")
        l5.Color <- "Gray"
        l5.Height <- 10.0

        // Add animations one by one for F# compatibility
        animator.AddToAnimations(m1)
        animator.AddToAnimations(m2)
        animator.AddToAnimations(m3)
        animator.AddToAnimations(m4)
        animator.AddToAnimations(m5)
        animator.Animate()

    // ═══════════════════════════════════════════════════════════════════════
    // MAIN ENTRY POINT
    // ═══════════════════════════════════════════════════════════════════════
    let Main() =
        // ═══════════════════════════════════════════════════════════════════
        // CHOOSE WHICH EXAMPLE TO RUN (uncomment one at a time)
        // ═══════════════════════════════════════════════════════════════════

        PointExamples()
        // LineExamples()
        // CircleExamples()
        // ArcExamples()
        // EllipseExamples()
        // RectangleExamples()
        // PolygonExamples()
        // PolylineExamples()
        // BezierExamples()
        // SplineExamples()
        // TextExamples()
        // ArrowExamples()
        // DimensionExamples()
        // GroupExamples()
        // GridExamples()
        // BooleanOperationsExamples()
        // AnimationExamples()
        // EasingFunctionsExamples()
