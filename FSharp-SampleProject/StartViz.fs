namespace FSharpSample

open System
open VizDsl

module Viz =
    // Entry point - demonstrates functional F# patterns
    let Main() =
        Console.WriteLine("F# Multi-Module Sample Project")
        Console.WriteLine("Demonstrating functional programming patterns")

        // Test Colors module
        Console.WriteLine($"Red color: {Colors.Red}")
        Console.WriteLine($"Color at index 3: {Colors.getColorByIndex 3}")

        // Draw title using pipeline
        Shapes.createText 0.0 350.0 "F# Functional Style" Colors.White
        |> withHeight 32.0
        |> draw
        |> ignore

        // Draw a row of circles using Shapes module (functional internally)
        Console.WriteLine("Drawing circle row...")
        Shapes.drawCircleRow -250.0 200.0 6 40.0 100.0
        |> ignore

        // Draw a grid of rectangles (functional internally)
        Console.WriteLine("Drawing rectangle grid...")
        Shapes.drawRectGrid -200.0 50.0 5 3 60.0 80.0
        |> ignore

        // Draw individual shapes using pipelines
        Shapes.createCircle 0.0 -200.0 80.0 Colors.Purple
        |> draw
        |> ignore

        Shapes.createText 0.0 -200.0 "Center" Colors.White
        |> draw
        |> ignore

        // Draw connecting line with pipeline
        Shapes.createLine -250.0 200.0 150.0 200.0 Colors.Gray
        |> withThickness 1.0
        |> draw
        |> ignore

        // Demonstrate pure functional pattern: map over list and draw
        Console.WriteLine("Drawing colorful points...")
        [0..11]
        |> List.map (fun i ->
            let angle = float i * 30.0 * System.Math.PI / 180.0
            let x = 200.0 * cos angle
            let y = -200.0 + 50.0 * sin angle
            point x y
            |> withFill (Color.byIndex i)
            |> withStroke Colors.White)
        |> drawAll
        |> ignore

		AnimationSample.Run()
        Console.WriteLine("Done!")
