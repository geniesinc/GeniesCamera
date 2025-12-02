using System;

namespace Genies.Customization.Framework.Actions
{
    [Flags]
    public enum ActionBarFlags
    {
        None = 0x0,
        Undo = 1 << 0,
        Redo = 1 << 1,
        ResetAll = 1 << 2,
        Save = 1 << 3,
        Exit = 1 << 4,
        Submit = 1 << 5,
        Share = 1 << 6,
        Create = 1 << 7
    }
}
