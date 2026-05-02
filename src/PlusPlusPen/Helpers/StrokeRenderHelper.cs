using System.Windows;
using System.Windows.Media;
using PlusPlusPen.Models;

namespace PlusPlusPen.Helpers;

public static class StrokeRenderHelper
{
    public static void DrawStroke(
        DrawingContext context,
        StrokeModel stroke,
        double scaleX = 1.0,
        double scaleY = 1.0,
        StrokeRenderOptions? options = null)
    {
        if (stroke.Points.Count == 0)
        {
            return;
        }

        var brush = new SolidColorBrush(stroke.Color);
        brush.Freeze();

        var resolvedOptions = options ?? StrokeRenderOptions.Default;
        var samples = BuildRenderSamples(stroke, scaleX, scaleY, resolvedOptions);
        if (samples.Count == 0)
        {
            return;
        }

        if (samples.Count == 1)
        {
            var radius = Math.Max(0.6, samples[0].Width * 0.5);
            context.DrawEllipse(brush, null, samples[0].Position, radius, radius);
            return;
        }

        ApplyTaper(samples, resolvedOptions);

        for (var i = 0; i < samples.Count - 1; i++)
        {
            var start = samples[i];
            var end = samples[i + 1];
            var width = Math.Max(1.0, (start.Width + end.Width) * 0.5);
            var pen = CreatePen(brush, width);
            context.DrawLine(pen, start.Position, end.Position);
        }

        DrawEndpoint(context, brush, samples[0]);
        DrawEndpoint(context, brush, samples[^1]);
    }

    public static StrokeRenderOptions CreateRenderOptions(AppSettingsModel settings, bool isLivePreview)
    {
        var subdivisions = settings.SmoothingPreset switch
        {
            SmoothingPresetOption.Off => 1,
            SmoothingPresetOption.Low => isLivePreview ? 1 : 2,
            SmoothingPresetOption.High => isLivePreview ? 2 : 5,
            _ => isLivePreview ? 2 : 3
        };

        return new StrokeRenderOptions(
            settings.PenStyle,
            settings.SmoothingPreset,
            settings.StrokeTaperEnabled,
            settings.VelocityBasedThicknessEnabled,
            settings.FountainPenEffectEnabled,
            isLivePreview,
            subdivisions);
    }

    private static List<RenderSample> BuildRenderSamples(
        StrokeModel stroke,
        double scaleX,
        double scaleY,
        StrokeRenderOptions options)
    {
        var points = stroke.Points
            .Select(x => new RenderSample(
                ScalePoint(x.Position, scaleX, scaleY),
                ScaleWidth(ApplyStyleWidth(x.Width, options.PenStyle, options.FountainPenEffectEnabled), scaleX, scaleY)))
            .ToList();

        if (points.Count <= 2 || options.Subdivisions <= 1 && options.SmoothingPreset == SmoothingPresetOption.Off)
        {
            return points;
        }

        var smoothed = new List<RenderSample>(points.Count * Math.Max(2, options.Subdivisions));
        for (var index = 0; index < points.Count - 1; index++)
        {
            var p0 = index == 0 ? points[index] : points[index - 1];
            var p1 = points[index];
            var p2 = points[index + 1];
            var p3 = index + 2 < points.Count ? points[index + 2] : points[index + 1];
            var stepCount = Math.Max(1, options.Subdivisions);

            for (var step = 0; step < stepCount; step++)
            {
                if (index > 0 && step == 0)
                {
                    continue;
                }

                var t = step / (double)stepCount;
                smoothed.Add(new RenderSample(
                    CatmullRom(p0.Position, p1.Position, p2.Position, p3.Position, t),
                    CatmullRom(p0.Width, p1.Width, p2.Width, p3.Width, t)));
            }
        }

        smoothed.Add(points[^1]);
        return smoothed;
    }

    private static void ApplyTaper(List<RenderSample> samples, StrokeRenderOptions options)
    {
        if (!options.StrokeTaperEnabled || samples.Count < 3)
        {
            return;
        }

        var totalLength = 0d;
        var distances = new double[samples.Count];
        for (var index = 1; index < samples.Count; index++)
        {
            totalLength += (samples[index].Position - samples[index - 1].Position).Length;
            distances[index] = totalLength;
        }

        if (totalLength <= 0.5)
        {
            return;
        }

        var taperRatio = options.PenStyle switch
        {
            PenStyleOption.FountainPen => 0.16,
            PenStyleOption.FeltTip => 0.08,
            PenStyleOption.Soft => 0.12,
            _ => 0.07
        };
        var minimumFactor = options.PenStyle switch
        {
            PenStyleOption.FountainPen => 0.24,
            PenStyleOption.FeltTip => 0.42,
            PenStyleOption.Soft => 0.3,
            _ => 0.36
        };

        for (var index = 0; index < samples.Count; index++)
        {
            var progress = distances[index] / totalLength;
            var startFactor = EvaluateTaper(progress, taperRatio, minimumFactor);
            var endFactor = EvaluateTaper(1 - progress, taperRatio, minimumFactor);
            samples[index] = samples[index] with
            {
                Width = Math.Max(0.8, samples[index].Width * Math.Min(startFactor, endFactor))
            };
        }
    }

    private static double EvaluateTaper(double progress, double taperRatio, double minimumFactor)
    {
        if (progress >= taperRatio)
        {
            return 1.0;
        }

        var normalized = Math.Clamp(progress / taperRatio, 0.0, 1.0);
        var eased = 1 - Math.Pow(1 - normalized, 2);
        return Lerp(minimumFactor, 1.0, eased);
    }

    private static void DrawEndpoint(DrawingContext context, Brush brush, RenderSample sample)
    {
        var radius = Math.Max(0.6, sample.Width * 0.5);
        context.DrawEllipse(brush, null, sample.Position, radius, radius);
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

    private static double ApplyStyleWidth(double width, PenStyleOption style, bool fountainEffectEnabled)
    {
        var multiplier = style switch
        {
            PenStyleOption.Soft => 1.04,
            PenStyleOption.FountainPen => 1.12,
            PenStyleOption.FeltTip => 1.18,
            _ => 1.0
        };

        if (fountainEffectEnabled && style != PenStyleOption.Direct)
        {
            multiplier *= 1.03;
        }

        return width * multiplier;
    }

    private static Point CatmullRom(Point p0, Point p1, Point p2, Point p3, double t)
    {
        return new Point(
            CatmullRom(p0.X, p1.X, p2.X, p3.X, t),
            CatmullRom(p0.Y, p1.Y, p2.Y, p3.Y, t));
    }

    private static double CatmullRom(double p0, double p1, double p2, double p3, double t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        return 0.5 * ((2 * p1)
            + (-p0 + p2) * t
            + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2
            + (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
    }

    private static Point ScalePoint(Point point, double scaleX, double scaleY)
    {
        return new Point(point.X * scaleX, point.Y * scaleY);
    }

    private static double ScaleWidth(double width, double scaleX, double scaleY)
    {
        return width * ((scaleX + scaleY) * 0.5);
    }

    private static double Lerp(double start, double end, double amount)
    {
        return start + (end - start) * amount;
    }

    private record struct RenderSample(Point Position, double Width);
}

public sealed record StrokeRenderOptions(
    PenStyleOption PenStyle,
    SmoothingPresetOption SmoothingPreset,
    bool StrokeTaperEnabled,
    bool VelocityBasedThicknessEnabled,
    bool FountainPenEffectEnabled,
    bool IsLivePreview,
    int Subdivisions)
{
    public static StrokeRenderOptions Default { get; } = new(
        PenStyleOption.Soft,
        SmoothingPresetOption.Medium,
        true,
        true,
        true,
        false,
        3);
}
