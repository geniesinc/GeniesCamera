using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Ugc
{
    public sealed class WearableRender : IWearableRender
    {
        public bool IsAlive { get; private set; }
        public GameObject Root => IsAlive ? _root : null;
        public Bounds Bounds { get { RecalculateBounds(); return _bounds; } }
        public bool RegionDebugging { get => _regionDebugging; set => SetRegionDebugging(value); }

        private readonly IWearableRenderer _renderer;
        
        private readonly GameObject _root;
        private readonly Dictionary<string, IElementRender> _elementRenders;
        private readonly HashSet<string> _renderedElementIds;
        private readonly HashSet<string> _soloElementIds;
        private readonly HashSet<string> _boundedElementIds;
        private Bounds _bounds;
        private CancellationTokenSource _renderCancellationSource = new CancellationTokenSource();
        private UniTaskCompletionSource _renderCompletionSource;
        private bool _regionDebugging;

        public WearableRender(IWearableRenderer renderer, Transform renderParent = null)
        {
            _renderer = renderer;
            _root = new GameObject("WearableRender");
            _root.transform.SetParent(renderParent, false);
            _elementRenders = new Dictionary<string, IElementRender>();
            _renderedElementIds = new HashSet<string>();
            _soloElementIds = new HashSet<string>();
            _boundedElementIds = new HashSet<string>();
            
            IsAlive = true;
        }

        public async UniTask ApplyWearableAsync(Wearable wearable)
        {
            if (!IsAlive || wearable is null)
            {
                return;
            }

            StartRender();
            
            _root.name = $"WearableRender: {wearable.TemplateId}";
            ClearRender();
            
            // render all the wearable splits (will try to reuse cached element renders)
            if (!(wearable.Splits is null))
            {
                await UniTask.WhenAll(wearable.Splits.Select(RenderSplit));
            }

            FinishRender();
        }

        public Bounds GetAlignedBounds(Quaternion rotation)
        {
            // encapsulate the aligned bounds of all rendered elements
            var bounds = new Bounds(Vector3.zero, Vector3.negativeInfinity);
            bool neverEncapsulated = true;

            foreach (IElementRender elementRender in GetAllDisplayedRenders())
            {
                bounds.Encapsulate(elementRender.GetAlignedBounds(rotation));
                neverEncapsulated = false;
            }
            
            return neverEncapsulated ? new Bounds() : bounds;
        }
        
        public void SetElementIdSoloRendered(string elementId, bool soloRendered)
        {
            if (soloRendered)
            {
                _soloElementIds.Add(elementId);
            }
            else
            {
                _soloElementIds.Remove(elementId);
            }

            RefreshSoloRenders().Forget();
        }
        
        public void SetElementIdsSoloRendered(IEnumerable<string> elementIds, bool soloRendered)
        {
            if (soloRendered)
            {
                _soloElementIds.UnionWith(elementIds);
            }
            else
            {
                _soloElementIds.ExceptWith(elementIds);
            }

            RefreshSoloRenders().Forget();
        }

        public void ClearAllSoloRenders()
        {
            _soloElementIds.Clear();
            RefreshSoloRenders().Forget();
        }

        public void PlayAnimation(string elementId, ValueAnimation animation)
        {
            if (_soloElementIds.Count > 0 && !_soloElementIds.Contains(elementId))
            {
                return;
            }

            if (_renderedElementIds.Contains(elementId) && _elementRenders.TryGetValue(elementId, out IElementRender elementRender))
            {
                elementRender.PlayAnimation(animation);
            }
        }

        public void StopAnimation(string elementId)
        {
            if (_elementRenders.TryGetValue(elementId, out IElementRender elementRender))
            {
                elementRender.StopAnimation();
            }
        }

        public void PlayRegionAnimation(string elementId, int regionIndex, ValueAnimation animation, bool playAlone = false)
        {
            if (_soloElementIds.Count > 0 && !_soloElementIds.Contains(elementId))
            {
                return;
            }

            if (_renderedElementIds.Contains(elementId) && _elementRenders.TryGetValue(elementId, out IElementRender elementRender))
            {
                elementRender.PlayRegionAnimation(regionIndex, animation, playAlone);
            }
        }

        public void StopRegionAnimation(string elementId, int regionIndex)
        {
            if (_elementRenders.TryGetValue(elementId, out IElementRender elementRender))
            {
                elementRender.StopRegionAnimation(regionIndex);
            }
        }

        public void StopAllAnimations()
        {
            foreach (IElementRender elementRender in GetAllDisplayedRenders())
            {
                elementRender.StopAnimation();
            }
        }

        public void Dispose()
        {
            DisposeAsync().Forget();
        }

        private async UniTaskVoid DisposeAsync()
        {
            await UniTask.SwitchToMainThread();
            
            if (!IsAlive)
            {
                return;
            }

            foreach (IElementRender elementRender in _elementRenders.Values)
            {
                elementRender.Dispose();
            }

            _elementRenders.Clear();
            _renderedElementIds.Clear();
            _soloElementIds.Clear();
            _boundedElementIds.Clear();
            Object.Destroy(_root);
            IsAlive = false;
        }

        private async UniTask RenderSplit(Split split)
        {
            if (split?.ElementId is null)
            {
                return;
            }

            CancellationToken cancellationToken = _renderCancellationSource.Token;

            // if we have not rendered this element before, create a new render
            if (!_elementRenders.TryGetValue(split.ElementId, out IElementRender elementRender))
            {
                elementRender = await _renderer.RenderElementAsync(split.ElementId, split.MaterialVersion);
                if (elementRender is null)
                {
                    return;
                }

                // it could happen that the same element was rendered by the next apply call
                if (cancellationToken.IsCancellationRequested && _elementRenders.TryGetValue(split.ElementId, out _))
                {
                    elementRender.Dispose();
                    return;
                }
                
                elementRender.Root.transform.SetParent(_root.transform, false);
                elementRender.RegionDebugging = _regionDebugging;
                _elementRenders[split.ElementId] = elementRender;
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // enable the render and apply the split
            bool isActive = _soloElementIds.Count == 0 || _soloElementIds.Contains(split.ElementId);
            elementRender.Root.SetActive(isActive);
            _renderedElementIds.Add(split.ElementId);
            await elementRender.ApplySplitAsync(split);
        }

        private void ClearRender()
        {
            foreach (string elementId in _renderedElementIds)
            {
                if (_elementRenders.TryGetValue(elementId, out IElementRender elementRender))
                {
                    elementRender.Root.SetActive(false);
                }
            }

            _renderedElementIds.Clear();
        }
        
        private void RecalculateBounds()
        {
            if (_boundedElementIds.SetEquals(GetAllDisplayedElementIds()))
            {
                return;
            }

            _boundedElementIds.Clear();
            _bounds = new Bounds(Vector3.zero, Vector3.negativeInfinity);
            
            foreach (string elementId in GetAllDisplayedElementIds())
            {
                if (!_elementRenders.TryGetValue(elementId, out IElementRender elementRender))
                {
                    continue;
                }

                _bounds.Encapsulate(elementRender.Bounds);
                _boundedElementIds.Add(elementId);
            }
            
            if (_boundedElementIds.Count == 0)
            {
                _bounds = new Bounds();
            }
        }

        private void StartRender()
        {
            _renderCancellationSource.Cancel();
            _renderCancellationSource = new CancellationTokenSource();
            UniTaskCompletionSource oldCompletionSource = _renderCompletionSource;
            _renderCompletionSource = new UniTaskCompletionSource();
            oldCompletionSource?.TrySetResult();
        }

        private void FinishRender()
        {
            UniTaskCompletionSource oldCompletionSource = _renderCompletionSource;
            _renderCompletionSource = null;
            oldCompletionSource?.TrySetResult();
        }

        private async UniTask WaitForRenderToFinishAsync()
        {
            while (_renderCompletionSource != null)
            {
                await _renderCompletionSource.Task;
            }
        }

        private async UniTaskVoid RefreshSoloRenders()
        {
            await WaitForRenderToFinishAsync();
            
            // hide all renders that are not on the solo collection. Or if the solo collection is empty then show all renders
            foreach (string elementId in _renderedElementIds)
            {
                if (!_elementRenders.TryGetValue(elementId, out IElementRender elementRender))
                {
                    continue;
                }

                bool isActive = _soloElementIds.Count == 0 || _soloElementIds.Contains(elementId);
                elementRender.Root.SetActive(isActive);
            }
        }
        
        // returns a collection of all currently displayed renders (that is all the solo elements that are rendered or all the renders if there are no solo elements)
        private IEnumerable<IElementRender> GetAllDisplayedRenders()
        {
            foreach (string elementId in GetAllDisplayedElementIds())
            {
                if (_elementRenders.TryGetValue(elementId, out IElementRender elementRender))
                {
                    yield return elementRender;
                }
            }
        }

        private IEnumerable<string> GetAllDisplayedElementIds()
        {
            foreach (string elementId in _renderedElementIds)
            {
                if (_soloElementIds.Count == 0 || _soloElementIds.Contains(elementId))
                {
                    yield return elementId;
                }
            }
        }

        private void SetRegionDebugging(bool value)
        {
            if (_regionDebugging == value)
            {
                return;
            }

            _regionDebugging = value;
            
            foreach (IElementRender elementRender in _elementRenders.Values)
            {
                elementRender.RegionDebugging = _regionDebugging;
            }
        }
    }
}
