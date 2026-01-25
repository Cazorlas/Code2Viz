namespace FSharpSample

open System
open VizDsl
open Code2Viz.Geometry

module Viz =
    /// Entry point - demonstrates genetic algorithm polygon slicing
    let Main() =
        printfn "=== Genetic Algorithm Polygon Slicing (F#) ==="
        printfn "Slicing an irregular polygon into 5 pieces"
        printfn "Target ratios: 25%%, 20%%, 23%%, 17%%, 15%%"
        printfn ""

        // Create an irregular L-shaped polygon
        let polygonPoints = [
            (0.0, 0.0)
            (200.0, 0.0)
            (200.0, 100.0)
            (100.0, 100.0)
            (100.0, 200.0)
            (0.0, 200.0)
        ]
        let originalPolygon = polygon polygonPoints

        // Draw original outline for reference
        polygon polygonPoints
        |> withStroke "DimGray"
        |> withFill "Transparent"
        |> withThickness 1.0
        |> draw
        |> ignore

        // Configure the genetic algorithm
        let config = {
            GeneticSlicer.defaultConfig with
                PopulationSize = 80
                MaxGenerations = 150
                MutationRate = 0.20
                EliteCount = 4
                TargetRatios = [| 0.25; 0.20; 0.23; 0.17; 0.15 |]
        }

        // Run evolution
        printfn "Starting evolution..."
        printfn ""
        let (bestSolution, centroid, maxDist) =
            GeneticSlicer.evolve originalPolygon config (Some 42) true

        // Visualize results
        GeneticSlicer.visualize config bestSolution centroid maxDist

        printfn ""
        printfn "=== Algorithm Complete ==="
