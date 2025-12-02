using System.Collections.Generic;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// A data model for a group of installers/initializers that will be
    /// resolved together.
    /// </summary>
    internal class ResolveGroup
    {
        public string GroupName { get; set; } = null;
        public int GroupNumber { get; set; }
        public List<IGeniesInstaller> Installers { get; set; } = new List<IGeniesInstaller>();
        public List<IGeniesInitializer> Initializers { get; set; } = new List<IGeniesInitializer>();
    }
}
