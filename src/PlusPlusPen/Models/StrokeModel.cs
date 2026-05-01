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

    public StrokeModel Clone()
    {
        var clone = new StrokeModel(Color);
        foreach (var point in Points)
        {
            clone.Points.Add(new StrokePointModel(point.Position, point.Width));
        }

        return clone;
    }
}
