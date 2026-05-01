using PlusPlusPen.Models;

namespace PlusPlusPen.Services;

public sealed class DrawingHistoryService
{
    private readonly Stack<List<StrokeModel>> _undoStack = [];
    private readonly Stack<List<StrokeModel>> _redoStack = [];

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public void PushSnapshot(IEnumerable<StrokeModel> strokes)
    {
        _undoStack.Push(Clone(strokes));
        _redoStack.Clear();
    }

    public List<StrokeModel>? Undo(IEnumerable<StrokeModel> current)
    {
        if (!CanUndo)
        {
            return null;
        }

        _redoStack.Push(Clone(current));
        return _undoStack.Pop();
    }

    public List<StrokeModel>? Redo(IEnumerable<StrokeModel> current)
    {
        if (!CanRedo)
        {
            return null;
        }

        _undoStack.Push(Clone(current));
        return _redoStack.Pop();
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    private static List<StrokeModel> Clone(IEnumerable<StrokeModel> strokes)
    {
        return strokes.Select(x => x.Clone()).ToList();
    }
}
