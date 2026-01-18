namespace FSharpSample

open VizDsl
open Code2Viz.Geometry

/// Helper module for creating common shapes using functional style
module Shapes =
    /// Create a colored circle at the specified position
    let createCircle x y radius fillColor =
        circle x y radius
        |> withFill fillColor
        |> withStrokeStyle Colors.White 2.0

    /// Create a rectangle at the specified position
    let createRect x y width height fillColor =
        rectangle x y width height
        |> withFill fillColor
        |> withStrokeStyle Colors.White 2.0

    /// Create a line between two points
    let createLine x1 y1 x2 y2 color =
        line x1 y1 x2 y2
        |> withStroke color
        |> withThickness 2.0

    /// Create labeled text at a position
    let createText x y content color =
        text x y content
        |> withHeight 20.0
        |> withTextColor color

    /// Draw a row of circles (functional style with List.mapi)
    let drawCircleRow startX y count radius spacing =
        [0..count-1]
        |> List.map (fun i ->
            let x = startX + float i * spacing
            createCircle x y radius (Colors.getColorByIndex i))
        |> drawAll

    /// Draw a grid of rectangles (functional style using list comprehension)
    let drawRectGrid startX startY cols rows size spacing =
        [for row in 0..rows-1 do
            for col in 0..cols-1 ->
                let x = startX + float col * spacing
                let y = startY - float row * spacing
                let colorIndex = row * cols + col
                createRect x y size size (Colors.getColorByIndex colorIndex)]
        |> drawAll
