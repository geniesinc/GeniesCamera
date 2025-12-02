using Cysharp.Threading.Tasks;
using Genies.Looks.Models;
using Genies.Looks.Core.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Looks.Core
{
    /// <summary>
    /// Factory class for creating different types of look views.
    /// This static class provides convenience methods for instantiating and initializing look view components.
    /// </summary>
    public static class LooksViewFactory
    {
        private const string Path = "Looks/LooksRealtimeView";

        /// <summary>
        /// Creates and initializes a new real-time look view instance.
        /// This method loads the look view prefab from resources, instantiates it, and initializes it with the provided look data.
        /// </summary>
        /// <param name="look">The look data to configure the view with.</param>
        /// <param name="looksDependencies">The dependencies required for look functionality.</param>
        /// <returns>A task that completes with the initialized real-time look view instance.</returns>
        public static async UniTask<IRealtimeLookView> CreateRealtimeLook(LookData look, LooksDependencies looksDependencies)
        {
            var lookRealtimeView = Resources.Load<LookRealtimeView>(Path);
            var lookRealtimeViewInstance = (IRealtimeLookView) Object.Instantiate(lookRealtimeView);
            await lookRealtimeViewInstance.Initialize(look, looksDependencies);
            return lookRealtimeViewInstance;
        }
    }
}
