using System;
using System.Collections.Generic;

namespace Genies.Utilities.UndoRedo
{
    public class UndoRedoService<T> : IUndoRedoService<T>
    {
        protected readonly int MaxSize;
        protected readonly Stack<T> UndoStack = new Stack<T>();
        protected readonly Stack<T> RedoStack = new Stack<T>();
        public event Action<bool> SetRedoAllowed, SetUndoAllowed;

        public UndoRedoService(int maxSize)
        {
            MaxSize = maxSize;
        }

        public virtual void RegisterUndo(T data)
        {
            // purge redo stack when committing a new action to the undo stack.
            RedoStack.Clear();

            // push new action onto the undo stack
            UndoStack.Push(data);

            // send events updating buttons
            SendToggleEvents();

            // make sure undo stack.count <= maxSize
            Resize();
        }

        protected void SendToggleEvents()
        {
            SetRedoAllowed?.Invoke(RedoStack.Count > 0);
            SetUndoAllowed?.Invoke(UndoStack.Count > 1);
        }

        public bool HasUndo()
        {
            return UndoStack.Count > 0;
        }

        public bool HasRedo()
        {
            return RedoStack.Count > 0;
        }

        public T Undo()
        {
            if (UndoStack.Count > 0)
            {
                var item = UndoStack.Pop();
                RedoStack.Push(item);

                SendToggleEvents();

                return item;
            }

            return default;
        }

        public T Redo()
        {
            if (RedoStack.Count > 0)
            {
                UndoStack.Push(RedoStack.Pop());

                SendToggleEvents();

                return UndoStack.Peek();
            }

            return default;
        }

        private void Resize()
        {
            // ensure undo size is never greater than maxSize.
            if (UndoStack.Count > MaxSize)
            {
                // save maxSize number of actions.
                for (int i = 0; i < MaxSize; i++)
                {
                    RedoStack.Push(UndoStack.Pop());
                }

                // clear other unneeded actions
                UndoStack.Clear();

                // restore maxSize number of actions
                for (int i = 0; i < MaxSize; i++)
                {
                    UndoStack.Push(RedoStack.Pop());
                }
            }
        }

        public virtual void ClearHistory()
        {
            UndoStack.Clear();
            RedoStack.Clear();
        }
    }
}
