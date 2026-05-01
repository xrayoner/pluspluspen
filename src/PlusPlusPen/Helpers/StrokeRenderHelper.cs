using System.Windows;
using System.Windows.Media;
using PlusPlusPen.Models;

namespace PlusPlusPen.Helpers;

public static class StrokeRenderHelper
{
    public static void DrawStroke(DrawingContext context, StrokeModel stroke, double scaleX = 1.0, double scaleY = 1.0)
    {
        if (stroke.Points.Count == 0)
        {
            return;
        }

        var brush = new SolidColorBrush(stroke.Color);
        brush.Freeze();

        if (stroke.Points.Count == 1)
        {
            var point = stroke.Points[0];
            var center = ScalePoint(point.Position, scaleX, scaleY);
            var radius = ScaleWidth(point.Width, scaleX, scaleY) * 0.5;
            context.DrawEllipse(brush, null, center, radius, radius);
            return;
        }

        for (var i = 0; i < stroke.Points.Count - 1; i++)
        {
            var current = stroke.Points[i];
            var next = stroke.Points[i + 1];
            var start = ScalePoint(current.Position, scaleX, scaleY);
            var end = ScalePoint(next.Position, scaleX, scaleY);
            var width = ScaleWidth((current.Width + next.Width) * 0.5, scaleX, scaleY);
            var pen = CreatePen(brush, width);
            context.DrawLine(pen, start, end);
            var radius = width * 0.5;
            context.DrawEllipse(brush, null, end, radius, radius);
        }
    }

    private static Pen CreatePen(Brush brush, double width)
    {
        var pen = new Pen(brush, Math.Max(1.0, width))
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        pen.Freeze();
        return pen;
    }

    private static Point ScalePoint(Point point, double scaleX, double scaleY)
    {
        return new Point(point.X * scaleX, point.Y * scaleY);
    }

    private static double ScaleWidth(double width, double scaleX, double scaleY)
    {
        return width * ((scaleX + scaleY) * 0.5);
    }
}
