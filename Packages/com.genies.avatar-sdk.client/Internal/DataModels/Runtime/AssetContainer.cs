using System.Collections.Generic;
using System.IO;
using Genies.Components.ShaderlessTools;
using UMA;
using UMA.CharacterSystem;
using UnityEditor;
using UnityEngine;

namespace Genies.Models
{
    public class AssetContainer : ScriptableObject, IShaderlessAsset
    {
        public string assetId = "";
        public string Slot => Recipe.wardrobeSlot;
        public UMAWardrobeRecipe Recipe;
        public List<OverlayDataAsset> Overlays = new List<OverlayDataAsset>();
        public List<SlotDataAsset> Slots = new List<SlotDataAsset>();
        public string Subcategory;
        public CollisionData CollisionData;
        public List<Object> Extras = new();

        [SerializeField] private ShaderlessMaterials shaderlessMaterials;
        public ShaderlessMaterials ShaderlessMaterials
        {
            get => shaderlessMaterials;
            set => shaderlessMaterials = value;
        }

        public WardrobeSlot GetSlot()
        {
            return WardrobeSlotExtensions.FromString(Slot);
        }

#if UNITY_EDITOR
#if GENIES_INTERNAL
        [MenuItem("Window/Genies/DataModels/Editor Utilities/Wardrobe/Create Container From Wardrobe Recipe", true)]
#endif
        public static bool CreateFromRecipeValidation()
        {
            return Selection.activeObject is UMAWardrobeRecipe;
        }

#if GENIES_INTERNAL
        [MenuItem("Window/Genies/DataModels/Editor Utilities/Wardrobe/Create Container From Wardrobe Recipe")]
#endif
        public static void CreateFromSelectedRecipe()
        {
            var recipe = Selection.activeObject as UMAWardrobeRecipe;
            var container = CreateFromRecipe(recipe, "None");
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = container;
        }

        public static AssetContainer CreateFromRecipe(UMAWardrobeRecipe recipe, string subcategory)
        {
            var recipeContainer = ScriptableObject.CreateInstance<AssetContainer>();
            recipeContainer.Subcategory = subcategory;
            recipeContainer.Recipe = recipe;

            if (UMAContextBase.Instance == null)
            {
                UMAContextBase.CreateEditorContext();
            }

            var cachedRecipe = recipe.GetCachedRecipe(UMAGlobalContext.Instance);
            var slotData = cachedRecipe.slotDataList;
            foreach (var slot in slotData)
            {
                var slotDataAsset = slot.asset;
                if (!recipeContainer.Slots.Contains(slotDataAsset))
                {
                    recipeContainer.Slots.Add(slotDataAsset);
                }

                var overlays = slot.GetOverlayList();
                foreach (var overlay in overlays)
                {
                    var overlayAsset = overlay.asset;
                    if (!recipeContainer.Overlays.Contains(overlayAsset))
                    {
                        recipeContainer.Overlays.Add(overlayAsset);
                    }
                }
            }

            var path = AssetDatabase.GetAssetPath(recipe);
            var directory = Path.GetDirectoryName(path);

            AssetDatabase.CreateAsset(recipeContainer, $"{directory}/{recipe.name}_Container.asset");
            EditorUtility.SetDirty(recipeContainer);
            AssetDatabase.SaveAssets();
            return recipeContainer;
        }
#endif
    }
}
