using System;
using System.Collections.Generic;

namespace Genies.Avatars
{
    public interface ITattooController
    {
        IReadOnlyList<TattooSlotController> SlotControllers { get; }
        
        event Action Updated;
    }
}