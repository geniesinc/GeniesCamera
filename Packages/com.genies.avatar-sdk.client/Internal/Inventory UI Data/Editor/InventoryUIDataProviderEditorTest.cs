#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Genies.Assets.Services;
using Genies.Refs;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Genies.Inventory.UIData
{
    public class InventoryUIDataProviderEditorTest : MonoBehaviour
    {
        [SerializeField] private Image _image;

        private InventoryUIDataProvider<ColorTaggedInventoryAsset, BasicInventoryUiData> _provider;
        private IAssetsService _assetsService;

        // Cached loaded data for inspector display
        public readonly List<BasicInventoryUiData> LoadedData = new();

        private void OnEnable()
        {
            _assetsService = new AddressableAssetsService();
            _provider = new InventoryUIDataProvider<ColorTaggedInventoryAsset, BasicInventoryUiData>(
                UIDataProviderConfigs.DefaultWearablesConfig,
                _assetsService
            );
        }

        [ContextMenu("Load UI Data")]
        public async void LoadUIDataButton()
        {
            LoadedData.Clear();

            var results = await _provider.LoadUIData();

            LoadedData.AddRange(results);

            Debug.Log($"[EditorTest] Loaded {results.Count} UI data items");
        }

        [ContextMenu("Print All Asset IDs")]
        public async void PrintAllAssetIds()
        {
            var ids = await _provider.GetAllAssetIds();

            Debug.Log("[EditorTest] Asset IDs:");
            foreach (var id in ids)
            {
                Debug.Log(id);
            }
        }

        [ContextMenu("Test Get Data For First Asset")]
        public async void TestGetDataForFirstAsset()
        {
            if (LoadedData.Count == 0)
            {
                Debug.LogWarning("[EditorTest] No data loaded. Please load UI data first.");
                return;
            }

            var assetId = LoadedData[0].AssetId;

            var data = await _provider.GetDataForAssetId(assetId);

            Debug.Log($"[EditorTest] Data for AssetId {assetId}: {data.DisplayName}");
        }

        [ContextMenu("Assign Sprite to Image")]
        public async void AssignSpriteToImage()
        {
            if (LoadedData.Count == 0)
            {
                Debug.LogWarning("[EditorTest] No data loaded. Please load UI data first.");
                return;
            }

            if (_image == null)
            {
                Debug.LogWarning("[EditorTest] No image is assigned.");
                return;
            }

            var assetId = LoadedData[0].AssetId;

            var data = await _provider.GetDataForAssetId(assetId);

            _image.sprite = data.Thumbnail.Item;
        }
    }

// Custom Editor to show buttons in inspector
    [CustomEditor(typeof(InventoryUIDataProviderEditorTest))]
    public class InventoryUIDataProviderEditorTestEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            InventoryUIDataProviderEditorTest tester = (InventoryUIDataProviderEditorTest)target;

            if (GUILayout.Button("Load UI Data"))
            {
                tester.LoadUIDataButton();
            }

            if (GUILayout.Button("Print All Asset IDs"))
            {
                tester.PrintAllAssetIds();
            }

            if (GUILayout.Button("Test Get Data For First Asset"))
            {
                tester.TestGetDataForFirstAsset();
            }

            if (GUILayout.Button("Assign Sprite to Image"))
            {
                tester.AssignSpriteToImage();
            }

            if (tester.LoadedData.Count > 0)
            {
                EditorGUILayout.LabelField($"Loaded Data Count: {tester.LoadedData.Count}");
                foreach (var item in tester.LoadedData)
                {
                    EditorGUILayout.LabelField($"- {item.DisplayName} (ID: {item.AssetId})");
                }
            }
        }
    }
}
#endif
