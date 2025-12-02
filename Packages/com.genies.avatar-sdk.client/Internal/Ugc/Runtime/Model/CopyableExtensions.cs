using System.Collections.Generic;

namespace Genies.Ugc
{
    public static class CopyableExtensions
    {
        /// <summary>
        /// Performs a deep copy of the copyables list.
        /// </summary>
        public static List<T> DeepCopyList<T>(this List<T> instance)
            where T : ICopyable<T>
        {
            if (instance is null)
            {
                return null;
            }

            var copy = new List<T>(instance.Capacity);
        
            foreach (T copyable in instance)
            {
                if (copyable is null)
                {
                    copy.Add(default);
                }
                else
                {
                    copy.Add(copyable.DeepCopy());
                }
            }
            
            return copy;
        }

        /// <summary>
        /// Performs a deep copy of a copyables list into the destination list.
        /// It will try to reuse as much instances from the destination as possible to create each deep copy.
        /// If the destination is null, a new list instance will be created and assigned to it.
        /// </summary>
        public static void DeepCopyList<T>(this List<T> instance, ref List<T> destination)
            where T : ICopyable<T>
        {
            if (instance is null)
            {
                destination = null;
                return;
            }
            
            destination ??= new List<T>();

            if (instance.Count == 0)
            {
                destination.Clear();
                return;
            }
            
            // remove excess of items in the destinations (if any)
            int countDiff = destination.Count - instance.Count;
            if (countDiff > 0)
            {
                destination.RemoveRange(instance.Count, countDiff);
            }

            // iterate over the instance list to deep copy its items into the destination items
            for (int i = 0; i < instance.Count; ++i)
            {
                // if destinations had less items, create new copies
                if (i >= destination.Count)
                {
                    if (instance[i] is null)
                    {
                        destination.Add(default);
                    }
                    else
                    {
                        destination.Add(instance[i].DeepCopy());
                    }

                    continue;
                }
                
                // try to reuse the destination instances for a deep copy
                var copyable = destination[i];
                DeepCopy(instance[i], ref copyable);
                destination[i] = copyable;
            }
        }
        
        /// <summary>
        /// Tries to perform a definition deep copy into the destination.
        /// If the instance is null, the destination will be set to null.
        /// If the destination is null, a new deep copy will be created and assigned to the destination.
        /// </summary>
        public static void DeepCopy<T>(this T instance, ref T destination)
            where T : ICopyable<T>
        {
            if (instance is null)
            {
                destination = default;
                return;
            }

            if (destination is null)
            {
                destination = instance.DeepCopy();
                return;
            }

            instance.DeepCopy(destination);
        }
    }
}
