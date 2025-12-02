using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Genies.Customization.MegaEditor;
using Genies.ServiceManagement;

namespace Genies.Customization.Framework.ItemPicker
{
    public class InventoryPickerCellView : GenericItemPickerCellView
    {
        // TODO FIGURE OUT WHAT TO DO ABOUT PENDING ASSET SERVICE
        //private IPendingAigcAssetUIService _PendingAigcAssetUIService => this.GetService<IPendingAigcAssetUIService>();

        [SerializeField] private TextMeshProUGUI _assetNameLabel;
        [SerializeField] private Image _progressImage;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private GameObject _loadingSpinner;
        [SerializeField] private TMP_Text _countdownText;

        private const int _estimatedTotalTime = 1200; // 20 minutes
        private CancellationTokenSource _countdownCts, _progressCts;
        [SerializeField] private Image _successImage;
        [SerializeField] private Color _pendingOrFailedAssetColor;
        [SerializeField] private Material _blurMaterial;

        public void SetAssetName(string assetName)
        {
            _assetNameLabel.text = assetName;
        }

        /// <summary>
        /// Show loading text and starts progress updates, and sets the image for
        /// the UI cell after removing its background
        /// </summary>
        /// <param name="timeCreated">When the asset was submitted to GAP</param>
        /// <param name="taskId">The task id of the asset</param>
        /// <param name="sprite">The sprite used in the UI for the asset</param>
        public void SetLoadingData(DateTime timeCreated, string taskId, Sprite sprite)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            RemoveImageBackground(taskId, sprite).Forget();

            _progressText.color = _pendingOrFailedAssetColor;
            _progressText.text = "0% Complete";
            _progressText.gameObject.SetActive(true);
            _loadingSpinner.SetActive(true);
            thumbnail.material = _blurMaterial;

            TimeSpan timeElapsed = DateTime.UtcNow - timeCreated;

            // if time is taking longer than expected, keep progress at 99%
            if (timeElapsed > TimeSpan.FromSeconds(_estimatedTotalTime))
            {
                _progressText.text = "99% Complete";
            }
            else
            {
                // otherwise, start progress updates
                CueProgressUpdates(timeCreated).Forget();
            }
        }

        /// <summary>
        /// Incrementally updates
        /// </summary>
        /// <param name="timeCreated"></param>
        private async UniTask CueProgressUpdates(DateTime timeCreated)
        {
            // Cancel any existing progress updates
            _progressCts?.Cancel();
            _progressCts?.Dispose();
            _progressCts = new CancellationTokenSource();

            while (gameObject.activeInHierarchy)
            {
                if (_progressCts.Token.IsCancellationRequested)
                {
                    return;
                }

                TimeSpan timeElapsed = DateTime.UtcNow - timeCreated;
                float percent = (float)(timeElapsed.TotalSeconds / _estimatedTotalTime);
                percent = Mathf.Clamp01(percent);
                _progressText.text = $"{(int)(percent * 100)}% Complete";

                if (timeElapsed > TimeSpan.FromSeconds(_estimatedTotalTime))
                {
                    _progressText.text = $"99% Complete";
                    return;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(10), ignoreTimeScale: true, cancellationToken: _progressCts.Token);
            }
        }

        /// <summary>
        /// Removes the background from a texture used in a sprite.
        /// </summary>
        /// <param name="taskId">The task id of the pending asset whose texture will be altered</param>
        /// <param name="sprite">The sprite currently used in the UI for that asset</param>
        private UniTask RemoveImageBackground(string taskId, Sprite sprite)
        {
            // first check for any saved textures
            /*var texture = _PendingAigcAssetUIService.GetSavedAssetTexture(taskId);
            if (texture != null)
            {
                thumbnail.sprite = CreateSpriteFromTexture(texture);
                return UniTask.CompletedTask;
            }

            // remove background and assign and save texture
            var remover = new BackgroundRemover();
            remover.ProcessTextureAsync(this,
                sprite.texture,
                (result) =>
                {
                    thumbnail.sprite = CreateSpriteFromTexture(result);
                    _PendingAigcAssetUIService.SaveAssetTexture(taskId, result);
                });*/

            return UniTask.CompletedTask;
        }

        private Sprite CreateSpriteFromTexture(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        }

        public void RemovePendingAssetIcons()
        {
            if (_progressImage != null)
            {
                _progressImage.gameObject.SetActive(false);
            }
            if (_progressText != null)
            {
                _progressText.gameObject.SetActive(false);
            }
            if (_loadingSpinner != null)
            {
                _loadingSpinner.SetActive(false);
            }

            thumbnail.material = null;
            if (_successImage != null)
            {
                _successImage.gameObject.SetActive(false);
            }


            Color c = thumbnail.color;
            c.a = 1f;
            thumbnail.color = c;
            _assetNameLabel.color = Color.black;

            _countdownCts?.Cancel();
            _progressCts?.Cancel();
        }

        public void SetFailureIcon()
        {
            _progressText.text = "Item Failed";
            _progressText.color = Color.red;
            _loadingSpinner.SetActive(false);
            thumbnail.material = null;

            Color c = thumbnail.color;
            c.a = 0.4f;
            thumbnail.color = c;
            _assetNameLabel.color = _pendingOrFailedAssetColor;

            _successImage.gameObject.SetActive(false);
            _progressImage.gameObject.SetActive(false);
            _countdownCts?.Cancel();
            _progressCts?.Cancel();
        }

        public void SetSuccessIcon()
        {
            _successImage.gameObject.SetActive(true);
            _progressImage.gameObject.SetActive(false);
            _progressText.gameObject.SetActive(false);
            _loadingSpinner.SetActive(false);
            thumbnail.material = null;
            _countdownCts?.Cancel();
            _progressCts?.Cancel();
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _countdownCts?.Cancel();
            _progressCts?.Cancel();
        }
    }
}
