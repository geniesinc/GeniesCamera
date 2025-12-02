# Avatar Examples

This guide demonstrates how to load and manage avatars using the Genies SDK Avatar.

## Table of Contents

- [Loading Avatars](#loading-avatars)
- [Loading User Avatars](#loading-user-avatars)
- [Loading Default Avatars](#loading-default-avatars)
- [Avatar Properties](#avatar-properties)
- [Cleanup](#cleanup)

## Loading Avatars

The SDK provides methods to load both user-specific and default avatars.

## Loading User Avatars

Load the authenticated user's avatar. Requires the user to be logged in.

### Basic Usage

```csharp
using Genies.Sdk.Avatar;
using UnityEngine;

public class AvatarLoader : MonoBehaviour
{
    private ManagedAvatar _avatar;

    private async void Start()
    {
        // Ensure user is logged in
        if (!GeniesSdkAvatar.IsLoggedIn)
        {
            Debug.LogWarning("User must be logged in to load their avatar");
            return;
        }

        // Load user's avatar
        _avatar = await GeniesSdkAvatar.LoadUserAvatarAsync();

        if (_avatar != null)
        {
            Debug.Log("User avatar loaded successfully");
        }
    }

    private void OnDestroy()
    {
        _avatar?.Dispose();
    }
}
```

### With Custom Name

```csharp
private async void LoadAvatar()
{
    ManagedAvatar avatar = await GeniesSdkAvatar.LoadUserAvatarAsync(
        avatarName: "PlayerAvatar"
    );

    // The avatar's root GameObject will be named "PlayerAvatar"
    Debug.Log($"Avatar loaded: {avatar.Root.name}");
}
```

### With Parent Transform

```csharp
public class AvatarSpawner : MonoBehaviour
{
    [SerializeField] private Transform _spawnPoint;

    private async void SpawnAvatar()
    {
        ManagedAvatar avatar = await GeniesSdkAvatar.LoadUserAvatarAsync(
            parent: _spawnPoint
        );

        // Avatar will be parented to the spawn point
        Debug.Log($"Avatar spawned at {_spawnPoint.position}");
    }
}
```

### With Animation Controller

```csharp
public class AnimatedAvatarLoader : MonoBehaviour
{
    [SerializeField] private RuntimeAnimatorController _animatorController;
    [SerializeField] private Transform _spawnLocation;

    private async void LoadAnimatedAvatar()
    {
        ManagedAvatar avatar = await GeniesSdkAvatar.LoadUserAvatarAsync(
            avatarName: "PlayerCharacter",
            parent: _spawnLocation,
            playerAnimationController: _animatorController
        );

        // Avatar is loaded with custom animation controller applied
        Debug.Log($"Animated avatar loaded: {avatar.Animator.runtimeAnimatorController.name}");
    }
}
```

### Complete Example with Event-Driven Loading

```csharp
public class UserAvatarManager : MonoBehaviour
{
    [SerializeField] private Transform _avatarParent;
    [SerializeField] private RuntimeAnimatorController _animatorController;

    private ManagedAvatar _currentAvatar;

    private void Awake()
    {
        GeniesSdkAvatar.Events.UserLoggedIn += OnUserLoggedIn;
        GeniesSdkAvatar.Events.UserLoggedOut += OnUserLoggedOut;
    }

    private async void OnUserLoggedIn()
    {
        // Load user's avatar when they log in
        string username = await GeniesSdkAvatar.GetUserNameAsync();
        Debug.Log($"Loading avatar for {username}");

        _currentAvatar = await GeniesSdkAvatar.LoadUserAvatarAsync(
            avatarName: $"{username}_Avatar",
            parent: _avatarParent,
            playerAnimationController: _animatorController
        );

        if (_currentAvatar != null)
        {
            Debug.Log("User avatar loaded and ready");
        }
    }

    private void OnUserLoggedOut()
    {
        // Clean up avatar when user logs out
        if (_currentAvatar != null)
        {
            _currentAvatar.Dispose();
            _currentAvatar = null;
        }
    }

    private void OnDestroy()
    {
        GeniesSdkAvatar.Events.UserLoggedIn -= OnUserLoggedIn;
        GeniesSdkAvatar.Events.UserLoggedOut -= OnUserLoggedOut;
        _currentAvatar?.Dispose();
    }
}
```

## Loading Default Avatars

Load a default avatar. Currently requires the user to be logged in.

### Basic Usage

```csharp
private async void LoadDefaultAvatar()
{
    // User must be logged in
    if (!GeniesSdkAvatar.IsLoggedIn)
    {
        Debug.LogWarning("User must be logged in");
        return;
    }

    ManagedAvatar avatar = await GeniesSdkAvatar.LoadDefaultAvatarAsync();

    if (avatar != null)
    {
        Debug.Log("Default avatar loaded");
    }
}
```

### With Configuration

```csharp
public class DefaultAvatarSpawner : MonoBehaviour
{
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private RuntimeAnimatorController _animatorController;

    private async void SpawnDefaultAvatar()
    {
        ManagedAvatar avatar = await GeniesSdkAvatar.LoadDefaultAvatarAsync(
            avatarName: "DefaultCharacter",
            parent: _spawnPoint,
            playerAnimationController: _animatorController
        );

        Debug.Log("Default avatar spawned and configured");
    }
}
```

## Avatar Properties

Access various properties of the loaded avatar:

```csharp
private void InspectAvatar(ManagedAvatar avatar)
{
    // Access avatar GameObjects
    GameObject root = avatar.Root;
    GameObject modelRoot = avatar.ModelRoot;

    // Access avatar transforms
    Transform skeletonRoot = avatar.SkeletonRoot;

    // Access the Animator component
    Animator animator = avatar.Animator;

    // Access the MonoBehaviour component
    ManagedAvatarComponent component = avatar.Component;

    // Check if disposed
    bool isDisposed = avatar.IsDisposed;

    Debug.Log($"Avatar Root: {root.name}");
    Debug.Log($"Skeleton Root: {skeletonRoot.name}");
    Debug.Log($"Has Animator: {animator != null}");
}
```

## Cleanup

Always dispose of avatars when they're no longer needed:

### Manual Cleanup

```csharp
private ManagedAvatar _avatar;

private async void LoadAvatar()
{
    _avatar = await GeniesSdkAvatar.LoadUserAvatarAsync();
}

private void DestroyAvatar()
{
    if (_avatar != null)
    {
        _avatar.Dispose(); // Disposes resources and destroys GameObject
        _avatar = null;
    }
}

private void OnDestroy()
{
    // Always clean up in OnDestroy
    _avatar?.Dispose();
}
```

### Managing Multiple Avatars

```csharp
public class MultiAvatarManager : MonoBehaviour
{
    private List<ManagedAvatar> _avatars = new List<ManagedAvatar>();

    public async void SpawnAvatar(Transform location)
    {
        ManagedAvatar avatar = await GeniesSdkAvatar.LoadUserAvatarAsync(
            parent: location
        );

        if (avatar != null)
        {
            _avatars.Add(avatar);
        }
    }

    public void DestroyAllAvatars()
    {
        foreach (var avatar in _avatars)
        {
            avatar.Dispose();
        }
        _avatars.Clear();
    }

    private void OnDestroy()
    {
        DestroyAllAvatars();
    }
}
```

## Best Practices

1. **Check Login State**: Always verify `GeniesSdkAvatar.IsLoggedIn` before loading user avatars.

2. **Always Dispose**: Call `Dispose()` on avatars when they're no longer needed to free resources.

3. **Cleanup in OnDestroy**: Ensure avatars are disposed in `OnDestroy()` to prevent memory leaks.

4. **Use Parent Transforms**: Parent avatars to appropriate transforms for easier scene management.

5. **Null Checks**: Always check if the returned `ManagedAvatar` is not null before using it.

## See Also

- [Authentication Examples](authentication-examples.md) - Setting up authentication before loading avatars
- [API Reference](api-reference.md) - Complete API documentation

