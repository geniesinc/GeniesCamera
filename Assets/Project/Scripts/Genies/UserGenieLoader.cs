using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Avatars.Context;
using Genies.Utilities.Internal;
using UnityEngine;

public class UserGenieLoader : MonoBehaviour
{
    [SerializeField] private Initializer _initializer;
    [SerializeField] private RuntimeAnimatorController _animatorController;
    [SerializeField] private SerializableAvatarLoader _avatarLoader;

    private IGenie _currGenieInstance;
    private bool _isLoadingAsync = false;

    public bool IsGenieLoaded => _currGenieInstance != null && !_currGenieInstance.IsDisposed;

    // Called by a button in the UI in the Avatar Loader Demo scene.
    public void LoadUserGenie()
    {
        // .Forget() is like a "fire and forget" for async methods,
        // similar to _ = LoadUserGenieAsync() but optimized/safer.
        LoadUserGenieAsync().Forget();
    }

    // Called by the GeniesManager when the app is ready to load the user genie.
    public async UniTask LoadUserGenieAsync(Transform genieParent = null)
    {
        // Checks if an avatar is already loading.
        if (_isLoadingAsync)
        {
            Debug.LogError("[UserGenieLoader] Ignoring seemingly redundant call to LoadUserGenieAsync.");
            return;
        }

        // Track our progress
        _isLoadingAsync = true;

        if (_currGenieInstance != null && !_currGenieInstance.IsDisposed)
        {
            // remove previous Genie. This shouldn't be run unless there was another.
            Debug.Log("[UserGenieLoader] Dispose of previous Genie...");
            _currGenieInstance.Dispose();
        }

        // await for the avatars context initialization
        Debug.Log("[UserGenieLoader] Calling WaitUntilInitializedAsync...");
        await _initializer.WaitUntilInitializedAsync();

        // load avatar from the configured loader
        Debug.Log("[UserGenieLoader] Creating Genie Instance...");
        _currGenieInstance = await _avatarLoader.LoadAsync(genieParent);
        Debug.Log("[UserGenieLoader] Created Genie Instance!");

        // adds specific animator if any
        if (_currGenieInstance?.Animator)
        {
            _currGenieInstance.Animator.enabled = true;
            if (_animatorController != null)
            {
                _currGenieInstance.Animator.runtimeAnimatorController = _animatorController;
            }
        }

        // Complete Async load
        _isLoadingAsync = false;
    }

    public void StripDefaultComponents()
    {
        if (_isLoadingAsync || _currGenieInstance == null)
        {
            Debug.LogError("[UserGenieLoader] Genie not loaded or loading. Cannot strip components.");
            return;
        }

        // Remove GeniesParty components that we may not need
        Debug.Log("[UserGenieLoader] Removing GeniesParty components...");
        _currGenieInstance.Components.RemoveAll();
    }
}
