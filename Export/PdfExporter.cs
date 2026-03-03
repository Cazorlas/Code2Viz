using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Code2Viz.Canvas;
using Code2Viz.Geometry;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Code2Viz.Export;

/// <summary>
/// Exports shapes to PDF format using PdfSharp.
/// </summary>
public class PdfExporter
{
    private double _margin = 20;

    // Compensates font sizes and point radii for the ScaleTransform
    // so text and markers don't get inflated. Pens scale with geometry.
    private double _sizeScale = 1.0;

    /// <summary>
    /// Exports shapes to a PDF file with auto-sized page.
    /// </summary>
    public void Export(IReadOnlyList<IDrawable> shapes, string filePath)
    {
        if (shapes.Count == 0) return;

        // Snapshot to avoid "collection was modified" during enumeration
        shapes = shapes.ToList();

        _sizeScale = 1.0;

        // Calculate bounds
        var (minPt, maxPt) = GetBounds(shapes);
        var width = maxPt.X - minPt.X + 2 * _margin;
        var height = maxPt.Y - minPt.Y + 2 * _margin;

        // Create PDF document
        var document = new PdfDocument();
        document.Info.Title = "Code2Viz Export";

        // Create a page with appropriate size
        var page = document.AddPage();
        page.Width = XUnit.FromPoint(Math.Max(width, 100));
        page.Height = XUnit.FromPoint(Math.Max(height, 100));

        using var gfx = XGraphics.FromPdfPage(page);

        // Transform: flip Y axis and translate
        gfx.TranslateTransform(_margin - minPt.X, page.Height.Point - _margin + minPt.Y);
        gfx.ScaleTransform(1, -1);

        // Draw shapes
        foreach (var drawable in shapes)
        {
            if (drawable is Shape shape && shape.IsVisible)
            {
                DrawShape(gfx, shape);
            }
        }

        // Save
        document.Save(filePath);
    }

    /// <summary>
    /// Exports shapes to a PDF file with specified page size, scale, and margins.
    /// </summary>
    /// <param name="shapes">Shapes to export.</param>
    /// <param name="filePath">Output file path.</param>
    /// <param name="pageWidthMm">Page width in mm (0 = auto-size to content).</param>
    /// <param name="pageHeightMm">Page height in mm (0 = auto-size to content).</param>
    /// <param name="scaleMmPerUnit">Scale factor: 1 drawing unit = this many mm on paper.</param>
    /// <param name="marginMm">Page margin in mm.</param>
    public void Export(IReadOnlyList<IDrawable> shapes, string filePath,
        double pageWidthMm, double pageHeightMm, double scaleMmPerUnit, double marginMm)
    {
        if (shapes.Count == 0) return;

        // Snapshot to avoid "collection was modified" during enumeration
        shapes = shapes.ToList();

        const double mmToPoints = 72.0 / 25.4;

        // Calculate content bounds in drawing units
        var (minPt, maxPt) = GetBounds(shapes);
        double contentW = maxPt.X - minPt.X;
        double contentH = maxPt.Y - minPt.Y;

        // Content size in mm
        double contentWMm = contentW * scaleMmPerUnit;
        double contentHMm = contentH * scaleMmPerUnit;

        // Determine page size in mm
        double pageW, pageH;
        if (pageWidthMm <= 0 || pageHeightMm <= 0)
        {
            // Auto-size: content + margins
            pageW = contentWMm + 2 * marginMm;
            pageH = contentHMm + 2 * marginMm;
        }
        else
        {
            pageW = pageWidthMm;
            pageH = pageHeightMm;
        }

        // Convert to PDF points
        double pageWPt = pageW * mmToPoints;
        double pageHPt = pageH * mmToPoints;
        double marginPt = marginMm * mmToPoints;
        double scalePtPerUnit = scaleMmPerUnit * mmToPoints;

        // Compensate pen widths for the scale transform so line weights
        // stay at their original point size rather than being inflated.
        _sizeScale = 1.0 / scalePtPerUnit;

        // Create PDF document
        var document = new PdfDocument();
        document.Info.Title = "Code2Viz Export";

        var page = document.AddPage();
        page.Width = XUnit.FromPoint(Math.Max(pageWPt, 10));
        page.Height = XUnit.FromPoint(Math.Max(pageHPt, 10));

        using var gfx = XGraphics.FromPdfPage(page);

        // Printable area in points
        double printableWPt = pageWPt - 2 * marginPt;
        double printableHPt = pageHPt - 2 * marginPt;

        // Content size in points (at scale)
        double contentWPt = contentW * scalePtPerUnit;
        double contentHPt = contentH * scalePtPerUnit;

        // Center content in printable area
        double offsetXPt = marginPt + (printableWPt - contentWPt) / 2;
        double offsetYPt = marginPt + (printableHPt - contentHPt) / 2;

        // Transform: translate to position content, apply scale, flip Y
        gfx.TranslateTransform(offsetXPt - minPt.X * scalePtPerUnit,
            page.Height.Point - offsetYPt + minPt.Y * scalePtPerUnit);
        gfx.ScaleTransform(scalePtPerUnit, -scalePtPerUnit);

        // Draw shapes
        foreach (var drawable in shapes)
        {
            if (drawable is Shape shape && shape.IsVisible)
            {
                DrawShape(gfx, shape);
            }
        }

        // Save
        document.Save(filePath);
    }

    private BoundingBox GetBounds(IReadOnlyList<IDrawable> shapes)
    {
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var drawable in shapes)
        {
            if (drawable is Shape shape)
            {
                var bounds = shape.GetBounds();
                minX = Math.Min(minX, bounds.Min.X);
                minY = Math.Min(minY, bounds.Min.Y);
                maxX = Math.Max(maxX, bounds.Max.X);
                maxY = Math.Max(maxY, bounds.Max.Y);
            }
        }

        if (minX == double.MaxValue)
        {
            return new BoundingBox(VPoint.Internal(0, 0), VPoint.Internal(100, 100));
        }

        return new BoundingBox(VPoint.Internal(minX, minY), VPoint.Internal(maxX, maxY));
    }

    private void DrawShape(XGraphics gfx, Shape shape)
    {
        var pen = CreatePen(shape);
        var brush = CreateBrush(shape);

        switch (shape)
        {
            case VDimension dim:
                DrawDimension(gfx, dim);
                break;
            case VPoint point:
                DrawPoint(gfx, point, pen);
                break;
            case VLine line:
                DrawLine(gfx, line, pen);
                break;
            case VCircle circle:
                DrawCircle(gfx, circle, pen, brush);
                break;
            case VArc arc:
                DrawArc(gfx, arc, pen);
                break;
            case VEllipse ellipse:
                DrawEllipse(gfx, ellipse, pen, brush);
                break;
            case VRectangle rect:
                DrawRectangle(gfx, rect, pen, brush);
                break;
            case VPolygon polygon:
                DrawPolygon(gfx, polygon, pen, brush);
                break;
            case VPolyline polyline:
                DrawPolyline(gfx, polyline, pen);
                break;
            case VBezier bezier:
                DrawBezier(gfx, bezier, pen);
                break;
            case VSpline spline:
                DrawSpline(gfx, spline, pen);
                break;
            case VArrow arrow:
                DrawArrow(gfx, arrow, pen);
                break;
            case VText text:
                DrawText(gfx, text);
                break;
        }
    }

    private XPen CreatePen(Shape shape)
    {
        var color = ParseColor(shape.Color);
        return new XPen(color, shape.LineWeight * _sizeScale);
    }

    private XPen CreatePen(string colorName, double lineWeight)
    {
        var color = ParseColor(colorName);
        return new XPen(color, lineWeight * _sizeScale);
    }

    private XBrush? CreateBrush(Shape shape)
    {
        if (string.IsNullOrEmpty(shape.FillColor) ||
            shape.FillColor.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        var color = ParseColor(shape.FillColor);
        return new XSolidBrush(color);
    }

    /// <summary>
    /// Parses a color string using WPF's ColorConverter for exact color matching
    /// with the canvas rendering, then converts to PdfSharp XColor.
    /// </summary>
    private XColor ParseColor(string colorName)
    {
        if (string.IsNullOrEmpty(colorName))
            return XColors.Black;

        // Use WPF's ColorConverter — same parser the canvas uses —
        // so named colors and hex values resolve identically.
        try
        {
            var wpfColor = (Color)ColorConverter.ConvertFromString(colorName);

            // Adapt light grayscale colors (like White, LightGray) for the white PDF background
            // by inverting them. Since we only check for grayscale (R == G == B),
            // pastel colors like LightYellow or Pink aren't affected.
            if (wpfColor.R == wpfColor.G && wpfColor.G == wpfColor.B && wpfColor.R > 200)
            {
                return XColor.FromArgb(wpfColor.A, 255 - wpfColor.R, 255 - wpfColor.G, 255 - wpfColor.B);
            }

            return XColor.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);
        }
        catch
        {
            // Fallback: should rarely happen since WPF ColorConverter
            // handles all named colors and hex formats.
            return XColors.Black;
        }
    }

    private void DrawPoint(XGraphics gfx, VPoint point, XPen pen)
    {
        double r = 2 * _sizeScale;
        gfx.DrawEllipse(pen, point.X - r, point.Y - r, r * 2, r * 2);
    }

    private void DrawLine(XGraphics gfx, VLine line, XPen pen)
    {
        gfx.DrawLine(pen, line.Start.X, line.Start.Y, line.End.X, line.End.Y);
    }

    private void DrawCircle(XGraphics gfx, VCircle circle, XPen pen, XBrush? brush)
    {
        var x = circle.Center.X - circle.Radius;
        var y = circle.Center.Y - circle.Radius;
        var size = circle.Radius * 2;

        if (brush != null)
        {
            gfx.DrawEllipse(brush, x, y, size, size);
        }
        gfx.DrawEllipse(pen, x, y, size, size);
    }

    private void DrawArc(XGraphics gfx, VArc arc, XPen pen)
    {
        var x = arc.Center.X - arc.Radius;
        var y = arc.Center.Y - arc.Radius;
        var size = arc.Radius * 2;

        // PdfSharp uses clockwise angles, so we may need to adjust
        double startAngle = -arc.StartAngle; // Negate for Y-flip
        double sweepAngle = -(arc.EndAngle - arc.StartAngle);

        gfx.DrawArc(pen, x, y, size, size, startAngle, sweepAngle);
    }

    private void DrawEllipse(XGraphics gfx, VEllipse ellipse, XPen pen, XBrush? brush)
    {
        var x = ellipse.Center.X - ellipse.RadiusX;
        var y = ellipse.Center.Y - ellipse.RadiusY;

        if (brush != null)
        {
            gfx.DrawEllipse(brush, x, y, ellipse.RadiusX * 2, ellipse.RadiusY * 2);
        }
        gfx.DrawEllipse(pen, x, y, ellipse.RadiusX * 2, ellipse.RadiusY * 2);
    }

    private void DrawRectangle(XGraphics gfx, VRectangle rect, XPen pen, XBrush? brush)
    {
        if (brush != null)
        {
            gfx.DrawRectangle(brush, rect.Corner.X, rect.Corner.Y, rect.Width, rect.Height);
        }
        gfx.DrawRectangle(pen, rect.Corner.X, rect.Corner.Y, rect.Width, rect.Height);
    }

    private void DrawPolygon(XGraphics gfx, VPolygon polygon, XPen pen, XBrush? brush)
    {
        if (polygon.Points.Count < 2) return;

        var points = new XPoint[polygon.Points.Count];
        for (int i = 0; i < polygon.Points.Count; i++)
        {
            points[i] = new XPoint(polygon.Points[i].X, polygon.Points[i].Y);
        }

        if (brush != null)
        {
            gfx.DrawPolygon(brush, points, XFillMode.Winding);
        }
        gfx.DrawPolygon(pen, points);
    }

    private void DrawPolyline(XGraphics gfx, VPolyline polyline, XPen pen)
    {
        if (polyline.Points.Count < 2) return;

        for (int i = 0; i < polyline.Points.Count - 1; i++)
        {
            gfx.DrawLine(pen,
                polyline.Points[i].X, polyline.Points[i].Y,
                polyline.Points[i + 1].X, polyline.Points[i + 1].Y);
        }
    }

    private void DrawBezier(XGraphics gfx, VBezier bezier, XPen pen)
    {
        gfx.DrawBezier(pen,
            bezier.P0.X, bezier.P0.Y,
            bezier.P1.X, bezier.P1.Y,
            bezier.P2.X, bezier.P2.Y,
            bezier.P3.X, bezier.P3.Y);
    }

    private void DrawSpline(XGraphics gfx, VSpline spline, XPen pen)
    {
        if (spline.ControlPoints.Count < 2) return;

        // Draw as polyline through control points (approximate)
        for (int i = 0; i < spline.ControlPoints.Count - 1; i++)
        {
            gfx.DrawLine(pen,
                spline.ControlPoints[i].X, spline.ControlPoints[i].Y,
                spline.ControlPoints[i + 1].X, spline.ControlPoints[i + 1].Y);
        }
    }

    private void DrawArrow(XGraphics gfx, VArrow arrow, XPen pen)
    {
        // Draw main line
        gfx.DrawLine(pen, arrow.Start.X, arrow.Start.Y, arrow.End.X, arrow.End.Y);

        // Draw arrowhead
        double dx = arrow.End.X - arrow.Start.X;
        double dy = arrow.End.Y - arrow.Start.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);

        if (length > 0)
        {
            double headSize = Math.Min(length * 0.2, arrow.HeadLength);
            double angle = Math.Atan2(dy, dx);
            double headAngleRad = arrow.HeadAngle * Math.PI / 180;

            double x1 = arrow.End.X - headSize * Math.Cos(angle - headAngleRad);
            double y1 = arrow.End.Y - headSize * Math.Sin(angle - headAngleRad);
            double x2 = arrow.End.X - headSize * Math.Cos(angle + headAngleRad);
            double y2 = arrow.End.Y - headSize * Math.Sin(angle + headAngleRad);

            gfx.DrawLine(pen, arrow.End.X, arrow.End.Y, x1, y1);
            gfx.DrawLine(pen, arrow.End.X, arrow.End.Y, x2, y2);
        }
    }

    private void DrawText(XGraphics gfx, VText text)
    {
        var color = ParseColor(text.Color);
        var brush = new XSolidBrush(color);
        var font = new XFont("Arial", Math.Max(text.Height * _sizeScale, 0.1), XFontStyleEx.Regular);

        // Text drawing with Y-flip correction
        gfx.Save();
        gfx.TranslateTransform(text.Location.X, text.Location.Y);
        gfx.ScaleTransform(1, -1); // Un-flip for text
        gfx.DrawString(text.Content ?? "", font, brush, 0, 0);
        gfx.Restore();
    }

    private void DrawDimension(XGraphics gfx, VDimension dim)
    {
        var geom = dim.GetDimensionGeometry();

        string extColor = dim.ExtensionLineColor ?? dim.Color;
        string dimLineColor = dim.DimensionLineColor ?? dim.Color;
        string textColorName = dim.TextColor ?? dim.Color;

        var extPen = CreatePen(extColor, dim.LineWeight);
        var dimPen = CreatePen(dimLineColor, dim.LineWeight);

        // Extension lines
        if (!dim.SuppressExtLine1)
            gfx.DrawLine(extPen, geom.ext1Start.X, geom.ext1Start.Y, geom.ext1End.X, geom.ext1End.Y);
        if (!dim.SuppressExtLine2)
            gfx.DrawLine(extPen, geom.ext2Start.X, geom.ext2Start.Y, geom.ext2End.X, geom.ext2End.Y);

        // Dimension line and arrowheads
        if (!dim.SuppressDimensionLine)
        {
            gfx.DrawLine(dimPen, geom.dimStart.X, geom.dimStart.Y, geom.dimEnd.X, geom.dimEnd.Y);

            // Arrowheads
            double dx = geom.dimEnd.X - geom.dimStart.X;
            double dy = geom.dimEnd.Y - geom.dimStart.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len > 0)
            {
                double angle = Math.Atan2(dy, dx);
                double arrowAngle = 15 * Math.PI / 180;
                double arrowLen = dim.ArrowSize;

                // Start arrowhead (pointing toward dimStart)
                double ax1 = geom.dimStart.X + arrowLen * Math.Cos(angle - arrowAngle);
                double ay1 = geom.dimStart.Y + arrowLen * Math.Sin(angle - arrowAngle);
                double ax2 = geom.dimStart.X + arrowLen * Math.Cos(angle + arrowAngle);
                double ay2 = geom.dimStart.Y + arrowLen * Math.Sin(angle + arrowAngle);
                gfx.DrawLine(dimPen, geom.dimStart.X, geom.dimStart.Y, ax1, ay1);
                gfx.DrawLine(dimPen, geom.dimStart.X, geom.dimStart.Y, ax2, ay2);

                // End arrowhead (pointing toward dimEnd)
                double bx1 = geom.dimEnd.X - arrowLen * Math.Cos(angle - arrowAngle);
                double by1 = geom.dimEnd.Y - arrowLen * Math.Sin(angle - arrowAngle);
                double bx2 = geom.dimEnd.X - arrowLen * Math.Cos(angle + arrowAngle);
                double by2 = geom.dimEnd.Y - arrowLen * Math.Sin(angle + arrowAngle);
                gfx.DrawLine(dimPen, geom.dimEnd.X, geom.dimEnd.Y, bx1, by1);
                gfx.DrawLine(dimPen, geom.dimEnd.X, geom.dimEnd.Y, bx2, by2);
            }
        }

        // Text
        var textColor = ParseColor(textColorName);
        var textBrush = new XSolidBrush(textColor);
        var font = new XFont("Arial", Math.Max(dim.TextHeight * _sizeScale, 0.1), XFontStyleEx.Regular);
        string displayText = dim.DisplayText;

        gfx.Save();
        gfx.TranslateTransform(geom.textPos.X, geom.textPos.Y);
        gfx.ScaleTransform(1, -1); // Un-flip for text

        if (dim.TextBackgroundOpaque)
        {
            var textSize = gfx.MeasureString(displayText, font);
            gfx.DrawRectangle(XBrushes.White,
                -textSize.Width / 2, -textSize.Height, textSize.Width, textSize.Height);
        }

        gfx.DrawString(displayText, font, textBrush, 0, 0,
            XStringFormats.TopCenter);
        gfx.Restore();
    }
}
