using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Genies.Utilities;
using Newtonsoft.Json.Utilities;
using UnityEngine.Scripting;

/*
 *   If changes are made to this file, please advance the JsonVersion number, and
 *   make the corresponding changes to the WearableDefinitionSchema.json
 */

namespace Genies.Ugc
{
    /// <summary>
    /// Represents a complete wearable item that can be applied to an avatar.
    /// A wearable consists of multiple splits (elements) that can be styled independently
    /// with colors, patterns, and textures. Implements the IModel pattern for value comparison,
    /// hashing, and deep copying operations.
    /// </summary>
    public class Wearable : IModel<Wearable>
    {
        // this line prevents the HashSet<string> constructor to be stripped by Unity when building the app. It fixes the deserialization errors for the Tags field
        // more info at https://github.com/jilleJr/Newtonsoft.Json-for-Unity/wiki/Fix-AOT-using-AotHelper
        [Preserve] private static void DeserializationFix() => AotHelper.EnsureList<string>();
        
        /// <summary>
        /// The current schema version for wearable serialization compatibility.
        /// </summary>
        public const string CurrentVersion = "1-0-0";
        
        /// <summary>
        /// The JSON schema version used for serialization and compatibility checking.
        /// This field is read-only and automatically set to the current version.
        /// </summary>
        [JsonProperty("JsonVersion", Required = Required.Always)]
        public readonly string JsonVersion = CurrentVersion;
        
        /// <summary>
        /// The unique identifier for the template this wearable is based on.
        /// Templates define the structure and available customization options for the wearable.
        /// </summary>
        [JsonProperty("TemplateId", Required = Required.Always)]
        public string TemplateId = string.Empty;
        
        /// <summary>
        /// Optional tags associated with this wearable for categorization and filtering purposes.
        /// Tags can be used for grouping, searching, or applying special behaviors to wearables.
        /// </summary>
        [JsonProperty("Tags")]
        public HashSet<string> Tags;
        
        /// <summary>
        /// The collection of individual elements (splits) that make up this wearable.
        /// Each split represents a separate mesh element with its own styling and region configuration.
        /// </summary>
        [JsonProperty("SplitElements", Required = Required.Always)]
        public List<Split> Splits;

        /// <summary>
        /// Determines whether this wearable is equivalent to another wearable by comparing all properties.
        /// Two wearables are considered equivalent if they have the same JsonVersion, TemplateId, Tags, and Splits.
        /// Null collections are treated the same as empty collections for comparison purposes.
        /// </summary>
        /// <param name="other">The other wearable to compare against.</param>
        /// <returns>True if the wearables are equivalent, false otherwise.</returns>
        public bool IsEquivalentTo(Wearable other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null || GetType() != other.GetType())
            {
                return false;
            }

            // for comparison purposes we will consider a null collection the same as an empty collection
            if (ModelUtils.IsNullOrEmpty(Tags) != ModelUtils.IsNullOrEmpty(other.Tags))
            {
                return false;
            }

            if (ModelUtils.IsNullOrEmpty(Splits) != ModelUtils.IsNullOrEmpty(other.Splits))
            {
                return false;
            }

            return
                ModelUtils.AreEqual(JsonVersion, other.JsonVersion) &&
                ModelUtils.AreEqual(TemplateId, other.TemplateId) &&
                (Tags?.SetEquals(other.Tags) ?? true) && // at this point if Tags is null then other.Tags is also null or empty
                (Splits?.SequenceEqual(other.Splits, ModelComparer.Instance) ?? true); // same here
        }

        /// <summary>
        /// Computes a hash code for this wearable based on all its properties.
        /// The hash includes JsonVersion, TemplateId, Tags, and Splits to ensure
        /// that equivalent wearables produce the same hash code.
        /// </summary>
        /// <returns>A hash code representing the current state of this wearable.</returns>
        public int ComputeHash()
        {
            return HashingUtils.GetCombinedHashCode(
                JsonVersion,
                TemplateId,
                Tags.GetCombinedHashCode(),
                Splits.ComputeModelsCollectionHash()
            );
        }

        /// <summary>
        /// Creates a deep copy of this wearable, including all splits, tags, and nested properties.
        /// The returned wearable is completely independent of the original and can be modified
        /// without affecting the source wearable.
        /// </summary>
        /// <returns>A new Wearable instance that is a deep copy of this wearable.</returns>
        public Wearable DeepCopy()
        {
            return new Wearable()
            {
                TemplateId = TemplateId,
                Tags = Tags is null ? null : new HashSet<string>(Tags),
                Splits = Splits.DeepCopyList()
            };
        }
        
        /// <summary>
        /// Performs a deep copy of this wearable's properties into the specified destination wearable.
        /// This method updates the destination wearable's properties to match this wearable,
        /// including deep copying of all splits, tags, and nested objects.
        /// </summary>
        /// <param name="destination">The target wearable to copy properties into. If null, no operation is performed.</param>
        public void DeepCopy(Wearable destination)
        {
            if (destination is null)
            {
                return;
            }

            destination.TemplateId = TemplateId;
            Splits.DeepCopyList(ref destination.Splits);

            if (Tags is null)
            {
                destination.Tags = null;
            }
            else
            {
                destination.Tags ??= new HashSet<string>();
                destination.Tags.Clear();
                destination.Tags.UnionWith(Tags);
            }
        }

        bool IModel.IsEquivalentTo(object other)
            => other is Wearable wearable && IsEquivalentTo(wearable);

        object ICopyable.DeepCopy()
            => DeepCopy();

        void ICopyable.DeepCopy(object destination)
            => DeepCopy(destination as Wearable);
    }
}
