# API Reference

Complete API documentation for the Genies SDK Avatar package.

> **Important**: The SDK uses an event-driven architecture. Always subscribe to events (via `GeniesSdkAvatar.Events`) to handle authentication flow progression and state changes. Don't rely solely on return values or polling state properties.
>
> For complete, contextual examples, see [Authentication Examples](authentication-examples.md) and [Avatar Examples](avatar-examples.md).

## Table of Contents

- [GeniesSdkAvatar](#geniessdkavatar)
  - [Methods](#methods)
  - [Properties](#properties)
  - [Events](#events)
- [ManagedAvatar](#managedavatar)
  - [Properties](#managedavatar-properties)
  - [Methods](#managedavatar-methods)

## GeniesSdkAvatar

Main static entry point for SDK operations.

### Methods

#### InitializeAsync

```csharp
public static async UniTask<bool> InitializeAsync()
```

Initializes the Genies Avatar SDK. Calling is optional as all operations will initialize the SDK if it is not already initialized.

**Returns:** `true` if initialization succeeded, `false` otherwise.

---

#### LoadDefaultAvatarAsync

```csharp
public static async UniTask<ManagedAvatar> LoadDefaultAvatarAsync(
    string avatarName = null,
    Transform parent = null,
    RuntimeAnimatorController playerAnimationController = null)
```

Loads a default avatar with optional configuration.

**Parameters:**
- `avatarName` - Optional name for the avatar GameObject
- `parent` - Optional parent transform for the avatar
- `playerAnimationController` - Optional animation controller to apply to the avatar

**Returns:** A `ManagedAvatar` instance, or `null` if loading failed.

**Note:** Currently requires user to be logged in.

---

#### LoadUserAvatarAsync

```csharp
public static async UniTask<ManagedAvatar> LoadUserAvatarAsync(
    string avatarName = null,
    Transform parent = null,
    RuntimeAnimatorController playerAnimationController = null)
```

Loads the authenticated user's avatar with optional configuration.

**Parameters:**
- `avatarName` - Optional name for the avatar GameObject
- `parent` - Optional parent transform for the avatar
- `playerAnimationController` - Optional animation controller to apply to the avatar

**Returns:** A `ManagedAvatar` instance, or `null` if loading failed.

**Note:** Requires user to be logged in. Falls back to default avatar if user is not logged in.

**See:** [Avatar Examples](avatar-examples.md) for complete usage examples

---

#### StartLoginOtpAsync

```csharp
public static async UniTask<(bool succeeded, string failReason)> StartLoginOtpAsync(string phoneNumber)
```

Starts the OTP login flow by submitting a phone number to receive a verification code.

**Parameters:**
- `phoneNumber` - The phone number to send the OTP code to (must include country code, e.g., "+15551234567")

**Returns:** A tuple indicating success and an optional failure reason.

**Events:** Triggers `LoginOtpCodeRequestSucceeded` or `LoginOtpCodeRequestFailed`.

**See:** [Authentication Examples](authentication-examples.md#otp-login-flow) for complete usage examples

---

#### ResendOtpCodeAsync

```csharp
public static async UniTask<(bool succeeded, string failReason)> ResendOtpCodeAsync()
```

Resends the OTP verification code to the phone number from the current login flow.

**Returns:** A tuple indicating success and an optional failure reason.

**Events:** Triggers `LoginOtpCodeRequestSucceeded` or `LoginOtpCodeRequestFailed`.

---

#### SubmitOtpCodeAsync

```csharp
public static async UniTask<(bool succeeded, string failReason)> SubmitOtpCodeAsync(string code)
```

Submits the OTP verification code to complete the login flow.

**Parameters:**
- `code` - The verification code received via SMS

**Returns:** A tuple indicating success and an optional failure reason.

**Events:** Triggers `LoginOtpCodeSubmissionSucceeded` or `LoginOtpCodeSubmissionFailed`, followed by `UserLoggedIn` on success.

---

#### TryInstantLoginAsync

```csharp
public static async UniTask TryInstantLoginAsync()
```

Attempts to automatically log in using stored credentials.

**Events:** Triggers `UserLoggedIn` if successful.

**See:** [Authentication Examples](authentication-examples.md#instant-login) for complete usage examples

---

#### GetUserNameAsync

```csharp
public static async UniTask<string> GetUserNameAsync()
```

Gets the username of the currently logged in user.

**Returns:** The username, or `null` if not logged in.

---

#### LogOutAsync

```csharp
public static async UniTask LogOutAsync()
```

Logs out the current user.

**Events:** Triggers `UserLoggedOut` when complete.

**See:** [Authentication Examples](authentication-examples.md#logout) for complete usage examples

---

### Properties

#### IsLoggedIn

```csharp
public static bool IsLoggedIn { get; }
```

Gets whether a user is currently logged in.

---

#### IsAwaitingOtpCode

```csharp
public static bool IsAwaitingOtpCode { get; }
```

Gets whether the OTP login flow is awaiting code submission.

---

### Events

All events are accessed via `GeniesSdkAvatar.Events`.

**See:** [Authentication Examples - Setting Up Events](authentication-examples.md#setting-up-events) for complete examples of event subscription and handling.

#### UserLoggedIn

```csharp
public static event Action UserLoggedIn;
```

Invoked when a user successfully logs in.

---

#### UserLoggedOut

```csharp
public static event Action UserLoggedOut;
```

Invoked when a user logs out.

---

#### LoginOtpCodeRequestSucceeded

```csharp
public static event Action<string> LoginOtpCodeRequestSucceeded;
```

Invoked when an OTP code request succeeds.

**Parameter:** `phoneNumber` - The phone number the code was sent to

---

#### LoginOtpCodeRequestFailed

```csharp
public static event Action<(string phoneNumber, string failReason)> LoginOtpCodeRequestFailed;
```

Invoked when an OTP code request fails.

**Parameters:**
- `phoneNumber` - The phone number the code was attempted to be sent to
- `failReason` - The reason for failure

---

#### LoginOtpCodeSubmissionSucceeded

```csharp
public static event Action<string> LoginOtpCodeSubmissionSucceeded;
```

Invoked when an OTP code submission succeeds.

**Parameter:** `code` - The submitted verification code

---

#### LoginOtpCodeSubmissionFailed

```csharp
public static event Action<(string code, string failReason)> LoginOtpCodeSubmissionFailed;
```

Invoked when an OTP code submission fails.

**Parameters:**
- `code` - The submitted verification code
- `failReason` - The reason for failure

---

## ManagedAvatar

Wrapper class providing simplified avatar manipulation interface.

**See:** [Avatar Examples](avatar-examples.md) for complete usage examples.

### ManagedAvatar Properties

#### Root

```csharp
public GameObject Root { get; }
```

Root GameObject holding the avatar hierarchy.

---

#### ModelRoot

```csharp
public GameObject ModelRoot { get; }
```

Sub-root GameObject where the visual model is parented.

---

#### SkeletonRoot

```csharp
public Transform SkeletonRoot { get; }
```

Skeleton root transform.

---

#### Animator

```csharp
public Animator Animator { get; }
```

Animator bound to the avatar rig.

---

#### Component

```csharp
public ManagedAvatarComponent Component { get; }
```

The MonoBehaviour component attached to the avatar's root GameObject. Provides a bridge between Unity's GameObject system and this ManagedAvatar wrapper.

---

#### IsDisposed

```csharp
public bool IsDisposed { get; }
```

Gets whether this ManagedAvatar has been disposed.

---

### ManagedAvatar Methods

#### Dispose

```csharp
public void Dispose()
```

Disposes native resources and destroys the avatar GameObject.

---

## See Also

- [Authentication Examples](authentication-examples.md) - Practical examples of authentication flows
- [Avatar Examples](avatar-examples.md) - Practical examples of avatar loading and management

