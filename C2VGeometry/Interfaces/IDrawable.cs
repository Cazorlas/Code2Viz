namespace C2VGeometry;

/// <summary>
/// Interface for shapes that can be drawn/rendered.
/// </summary>
public interface IDrawable
{
    /// <summary>
    /// Draws the shape (registers it for rendering if a registry is set).
    /// </summary>
    void Draw();

    /// <summary>
    /// The stroke color name (e.g., "Cyan", "Red", "#FF0000").
    /// </summary>
    string Color { get; set; }

    /// <summary>
    /// The fill color name (e.g., "Transparent", "Blue").
    /// </summary>
    string FillColor { get; set; }

    /// <summary>
    /// The stroke thickness in pixels.
    /// </summary>
    double LineWeight { get; set; }

    /// <summary>
    /// The line pattern style (solid, dashed, dotted, etc.).
    /// </summary>
    LineType LineType { get; set; }

    /// <summary>
    /// Scale factor for stroke pattern (dash/gap lengths). Default is 1.0.
    /// Values greater than 1.0 create longer dashes/gaps, less than 1.0 create shorter ones.
    /// </summary>
    double LineTypeScale { get; set; }
}
