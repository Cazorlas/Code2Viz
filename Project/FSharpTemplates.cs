namespace Code2Viz.Project;

public static class FSharpTemplates
{
    public static string GetStartVizTemplate(string projectName)
    {
        var safeName = Templates.SanitizeIdentifier(projectName);
        return $@"namespace {safeName}

open VizDsl

module Viz =
    // Entry point for the application
    let Main() =
        // Draw a styled circle using pipeline operators
        circle 0.0 0.0 100.0
        |> withFill ""#FF5733""
        |> withStrokeStyle ""White"" 2.0
        |> draw
        |> ignore

        // Draw text at center
        text 0.0 0.0 ""Hello F#""
        |> withHeight 24.0
        |> withTextColor ""White""
        |> draw
        |> ignore

        // Draw a row of colorful circles
        [0..5]
        |> List.map (fun i ->
            circle (float i * 60.0 - 150.0) 150.0 20.0
            |> withFill (Color.byIndex i))
        |> drawAll
        |> ignore
";
    }

    public static string GetEmptyModuleTemplate(string projectName, string moduleName)
    {
        var safeName = Templates.SanitizeIdentifier(projectName);
        var safeModuleName = Templates.SanitizeIdentifier(moduleName);
        return $@"namespace {safeName}

open VizDsl

module {safeModuleName} =
    // Add your code here
    let example() =
        circle 0.0 0.0 50.0
        |> withFill Color.blue
        |> draw
        |> ignore
";
    }
}
