namespace Genies.Refs
{
    /// <summary>
    /// Represents an object that is never released from memory and can have multiple generations.
    /// </summary>
    internal interface IGenerational
    {
        ulong Generation { get; }
        bool IsAlive { get; }
    }
}