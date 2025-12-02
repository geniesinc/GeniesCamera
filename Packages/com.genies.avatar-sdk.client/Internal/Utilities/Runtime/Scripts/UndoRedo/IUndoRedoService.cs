using System;

namespace Genies.Utilities.UndoRedo
{
    public interface IUndoRedoService<T>
    {
        void RegisterUndo(T undoRedoData);
        T Redo();
        T Undo();
        bool HasUndo();
        bool HasRedo();

        void ClearHistory();

        event Action<bool> SetRedoAllowed, SetUndoAllowed;
    }
}
