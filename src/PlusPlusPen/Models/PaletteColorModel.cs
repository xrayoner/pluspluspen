using System.Windows.Media;

namespace PlusPlusPen.Models;

public sealed class PaletteColorModel
{
    public PaletteColorModel(string name, Color color)
    {
        Name = name;
        Color = color;
    }

    public string Name { get; }

    public Color Color { get; }
}
