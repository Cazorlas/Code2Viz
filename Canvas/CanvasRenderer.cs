using Code2Viz.Geometry;
using Code2Viz.Animation;
using Code2Viz.Services;

namespace Code2Viz.Canvas;

public class CanvasRenderer : ICanvasRenderer
{
    private static CanvasRenderer? _instance;
    private static readonly object _lock = new();

    private readonly List<IDrawable> _shapes = new();

    /// <summary>
    /// The currently active timeline for animation playback.
    /// Internal use only - users should use the Animator class.
    /// </summary>
    internal Timeline? ActiveTimeline { get; set; }

    public static CanvasRenderer Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new CanvasRenderer();
                }
            }
            return _instance;
        }
    }

    private CanvasRenderer() { }

    public void AddShape(IDrawable shape)
    {
        // Prevent duplicate adds - check if shape is already placed
        if (shape is Shape s)
        {
            if (s.IsPlaced) return;
            s.IsPlaced = true;
        }
        _shapes.Add(shape);
    }

    /// <summary>
    /// Removes a shape from the canvas.
    /// </summary>
    public void RemoveShape(IDrawable shape)
    {
        if (shape is Shape s)
        {
            s.IsPlaced = false;
        }
        _shapes.Remove(shape);
    }

    /// <summary>
    /// Removes multiple shapes from the canvas efficiently.
    /// </summary>
    public void RemoveShapes(IEnumerable<IDrawable> shapes)
    {
        var shapeSet = new HashSet<IDrawable>(shapes);
        foreach (var shape in shapeSet)
        {
            if (shape is Shape s)
            {
                s.IsPlaced = false;
            }
        }
        _shapes.RemoveAll(s => shapeSet.Contains(s));
    }

    /// <summary>
    /// Moves a shape so it renders above (after) the reference shape in the draw order.
    /// </summary>
    public void MoveShapeAbove(IDrawable shape, IDrawable referenceShape)
    {
        if (shape == referenceShape) return;
        int refIndex = _shapes.IndexOf(referenceShape);
        if (refIndex < 0) return;
        if (!_shapes.Remove(shape)) return;
        // Re-find reference index after removal (it may have shifted)
        refIndex = _shapes.IndexOf(referenceShape);
        _shapes.Insert(refIndex + 1, shape);
    }

    /// <summary>
    /// Moves a shape so it renders behind (before) the reference shape in the draw order.
    /// </summary>
    public void MoveShapeBehind(IDrawable shape, IDrawable referenceShape)
    {
        if (shape == referenceShape) return;
        int refIndex = _shapes.IndexOf(referenceShape);
        if (refIndex < 0) return;
        if (!_shapes.Remove(shape)) return;
        refIndex = _shapes.IndexOf(referenceShape);
        _shapes.Insert(refIndex, shape);
    }

    public IReadOnlyList<IDrawable> GetShapes() => _shapes.AsReadOnly();

    public void Clear()
    {
        // Reset IsPlaced for all shapes so they can be re-added in next run
        foreach (var shape in _shapes)
        {
            if (shape is Shape s)
            {
                s.IsPlaced = false;
            }
        }
        _shapes.Clear();
        Shape.ResetIdCounter();
        ActiveTimeline?.Stop();
        ActiveTimeline = null;
    }

    public void RenderTo(RenderCanvas canvas)
    {
        canvas.Render(_shapes);
        if (Code2Viz.ApplicationSettings.Instance.ZoomToFitOnRun)
        {
            canvas.ZoomExtents(_shapes);
        }
    }
}
