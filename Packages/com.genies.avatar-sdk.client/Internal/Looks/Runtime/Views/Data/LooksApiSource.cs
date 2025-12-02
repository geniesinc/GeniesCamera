namespace Genies.Looks.Core.Data
{
    /// <summary>
    /// Defines the source for looks API operations, specifying whether to use local or remote backend services.
    /// This enumeration allows switching between local development/testing and production backend services.
    /// </summary>
    public enum LooksApiSource
    {
        /// <summary>
        /// Use local data source for looks operations, typically for development and testing scenarios.
        /// </summary>
        Local,
        
        /// <summary>
        /// Use remote backend API for looks operations, typically for production scenarios.
        /// </summary>
        Backend,
    }
}
