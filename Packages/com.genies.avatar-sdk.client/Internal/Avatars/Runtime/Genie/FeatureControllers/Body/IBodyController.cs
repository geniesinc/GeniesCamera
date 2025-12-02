using System;
using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Controls the shape of the avatar body through weighted attributes. Each attribute has a weight that must be in
    /// the range [-1, 1] where 0 is the default state of the body mesh for that attribute.
    /// <br/><br/>
    /// Attributes MUST ONLY modify body mesh geometry. This controller must never handle things like color and materials.
    /// </summary>
    public interface IBodyController
    {
        /// <summary>
        /// All attributes existing in this controller.
        /// </summary>
        IReadOnlyList<string> Attributes { get; }
        
        event Action Updated;
        
        /// <summary>
        ///  Whether the given attribute exists in this controller.
        /// </summary>
        bool HasAttribute(string name);
        
        /// <summary>
        /// Gets the weight of the given attribute. Returns 0 if the attribute doesn't exist.
        /// </summary>
        float GetAttributeWeight(string name);

        /// <summary>
        /// Sets the weight of the given attribute. Does nothing if the attribute doesn't exist.
        /// </summary>
        void SetAttributeWeight(string name, float weight);
        
        /// <summary>
        /// Sets the given attribute weights, which should be seen as a preset.
        /// </summary>
        void SetPreset(IReadOnlyDictionary<string, float> preset);
        
        /// <summary>
        /// Sets the given attribute states, which should be seen as a preset.
        /// </summary>
        void SetPreset(IEnumerable<BodyAttributeState> preset);
        
        /// <summary>
        /// Writes the current weights of all the attributes in the given <see cref="results"/> dictionary (it won't be cleared).
        /// </summary>
        void GetAllAttributeWeights(IDictionary<string, float> results);
        
        /// <summary>
        /// Returns a new dictionary with the current weights of all the attributes.
        /// </summary>
        Dictionary<string, float> GetAllAttributeWeights();

        /// <summary>
        /// Writes all current attribute states in the given <see cref="results"/> collection (it won't be cleared).
        /// </summary>
        void GetAllAttributeStates(ICollection<BodyAttributeState> results);
        
        /// <summary>
        /// Returns a new list with all current attribute states.
        /// </summary>
        List<BodyAttributeState> GetAllAttributeStates();
    }
}