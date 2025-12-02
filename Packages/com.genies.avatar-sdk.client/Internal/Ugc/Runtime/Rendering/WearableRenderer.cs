using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UMA;
using UnityEngine;
using Genies.Assets.Services;

namespace Genies.Ugc
{
    public class WearableRenderer : IWearableRenderer
    {
        private readonly IAssetLoader<UgcElementAsset> _elementLoader;
        private readonly IMegaMaterialBuilder _megaMaterialBuilder;

        public WearableRenderer(IAssetLoader<UgcElementAsset> elementLoader, IMegaMaterialBuilder megaMaterialBuilder)
        {
            _elementLoader = elementLoader;
            _megaMaterialBuilder = megaMaterialBuilder;
        }

        public async UniTask<IWearableRender> RenderWearableAsync(Wearable wearable)
        {
            var wearableRender = new WearableRender(this);
            await wearableRender.ApplyWearableAsync(wearable);
            return wearableRender;
        }

        public async UniTask<IElementRender> RenderElementAsync(string elementId, string materialVersion = null)
        {
            var elementRef = await _elementLoader.LoadAsync(elementId);
            if (!elementRef.IsAlive)
            {
                return null;
            }

            var elementRenderRef = await RenderElementAsync(elementRef, materialVersion);
            return elementRenderRef;
        }

        public UniTask<IElementRender> RenderElementAsync(Ref<UgcElementAsset> elementRef, string materialVersion = null)
        {
            MegaMaterial megaMaterial = _megaMaterialBuilder.BuildMegaMaterial(elementRef.New(), materialVersion);

            if (megaMaterial is null || !megaMaterial.IsAlive)
            {
                elementRef.Dispose();
                return UniTask.FromResult<IElementRender>(null);
            }

            UgcElementAsset element = elementRef.Item;

            // build the element GameObject
            var elementGo = new GameObject($"Element: {element.Id}");
            Ref<GameObject> elementGoRef = CreateRef.FromUnityObject(elementGo);
            var dependencies = new List<Ref>(element.SlotDataAssets.Length * 2);

            // build each slot
            var vertices = new List<Vector3>(element.SlotDataAssets.Length);
            Bounds bounds = element.SlotDataAssets.Length == 0 ? new Bounds() : new Bounds(Vector3.zero, Vector3.negativeInfinity);

            foreach (SlotDataAsset slotDataAsset in element.SlotDataAssets)
            {
                if (slotDataAsset is null)
                {
                    continue;
                }

                // build mesh
                Ref<Mesh> meshRef = UmaMeshBuilder.BuildMesh(slotDataAsset);

                // save mesh vertices and encapsulate its bounds
                vertices.AddRange(slotDataAsset.meshData.vertices);
                bounds.Encapsulate(meshRef.Item.bounds);

                // build slot GameObject
                var slotGo = new GameObject($"Slot: {slotDataAsset.name}");
                slotGo.transform.SetParent(elementGo.transform, false);
                slotGo.AddComponent<MeshFilter>().sharedMesh = meshRef.Item;
                slotGo.AddComponent<MeshRenderer>().sharedMaterial = megaMaterial.Material;
                Ref<GameObject> slotGoRef = CreateRef.FromUnityObject(slotGo);

                // register dependencies
                dependencies.Add(meshRef);
                dependencies.Add(slotGoRef);
            }

            // create the element render
            Ref<GameObject> groupedElementGoRef = CreateRef.FromDependentResource(elementGoRef, dependencies);
            IElementRender elementRender = new ElementRender(element.Data.Regions, groupedElementGoRef, megaMaterial, bounds, vertices);
            elementRef.Dispose();

            return UniTask.FromResult(elementRender);
        }
    }
}
