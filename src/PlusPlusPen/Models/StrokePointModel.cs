using System.Windows;

namespace PlusPlusPen.Models;

public sealed class StrokePointModel
{
    public StrokePointModel(Point position, double width)
    {
        Position = position;
        Width = width;
    }

    public Point Position { get; }

    public double Width { get; }
}
