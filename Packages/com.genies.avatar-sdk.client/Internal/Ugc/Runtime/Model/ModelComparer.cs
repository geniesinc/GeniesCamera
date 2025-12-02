using System.Collections.Generic;

namespace Genies.Ugc
{
    /// <summary>
    /// Use this if you want a hashing collection to view models as what they represent rather than comparing
    /// them as references.
    /// </summary>
    public class ModelComparer : IEqualityComparer<IModel>
    {
        public static readonly ModelComparer Instance = new ModelComparer();
        
        private ModelComparer() { }
        
        public bool Equals(IModel x, IModel y)
            => x?.IsEquivalentTo(y) ?? y is null;

        public int GetHashCode(IModel obj)
            => obj.ComputeHash();
    }
}
