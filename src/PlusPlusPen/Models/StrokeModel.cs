using System.Windows;
using System.Windows.Media;

namespace PlusPlusPen.Models;

public sealed class StrokeModel
{
    public StrokeModel(Color color)
    {
        Color = color;
    }

    public Color Color { get; }

    public List<StrokePointModel> Points { get; } = [];

    public Rect Bounds { get; private set; } = Rect.Empty;

    public void AppendPoint(StrokePointModel point)
    {
        Points.Add(point);
        ExpandBounds(point);
    }

    public void RecalculateBounds()
    {
        Bounds = Rect.Empty;
        foreach (var point in Points)
        {
            ExpandBounds(point);
        }
    }

    public StrokeModel Clone()
    {
        var clone = new StrokeModel(Color);
        foreach (var point in Points)
        {
            clone.AppendPoint(new StrokePointModel(point.Position, point.Width));
        }

        return clone;
    }

    private void ExpandBounds(StrokePointModel point)
    {
        var halfWidth = point.Width * 0.5;
        var pointBounds = new Rect(
            point.Position.X - halfWidth,
            point.Position.Y - halfWidth,
            Math.Max(0.1, point.Width),
            Math.Max(0.1, point.Width));

        Bounds = Bounds.IsEmpty ? pointBounds : Rect.Union(Bounds, pointBounds);
    }
}
