# Authentication Examples

This guide demonstrates how to implement authentication using the Genies SDK Avatar's event-driven architecture.

## Table of Contents

- [Complete Authentication Manager](#complete-authentication-manager)
- [Setting Up Events](#setting-up-events)
- [OTP Login Flow](#otp-login-flow)
- [Instant Login](#instant-login)
- [Logout](#logout)
- [Handling Login State Changes](#handling-login-state-changes)

## Complete Authentication Manager

This complete example shows the recommended pattern for implementing authentication:

```csharp
using Genies.Sdk.Avatar;
using UnityEngine;

public class AuthenticationManager : MonoBehaviour
{
    private void Awake()
    {
        // Subscribe to events
        GeniesSdkAvatar.Events.UserLoggedIn += OnUserLoggedIn;
        GeniesSdkAvatar.Events.UserLoggedOut += OnUserLoggedOut;
        GeniesSdkAvatar.Events.LoginOtpCodeRequestSucceeded += OnOtpCodeRequestSucceeded;
        GeniesSdkAvatar.Events.LoginOtpCodeRequestFailed += OnOtpCodeRequestFailed;
        GeniesSdkAvatar.Events.LoginOtpCodeSubmissionSucceeded += OnOtpCodeSubmissionSucceeded;
        GeniesSdkAvatar.Events.LoginOtpCodeSubmissionFailed += OnOtpCodeSubmissionFailed;
    }

    private async void Start()
    {
        await GeniesSdkAvatar.TryInstantLoginAsync();

        if (!GeniesSdkAvatar.IsLoggedIn)
        {
            // Show login UI
        }
    }

    // Call this when user submits phone number
    public async void StartLoginWithPhoneNumber(string phoneNumber)
    {
        await GeniesSdkAvatar.StartLoginOtpAsync(phoneNumber);
        // Event will fire when complete
    }

    // Call this when user submits OTP code
    public async void SubmitOtpCode(string code)
    {
        await GeniesSdkAvatar.SubmitOtpCodeAsync(code);
        // Event will fire when complete
    }

    // Event handlers drive your UI flow
    private void OnOtpCodeRequestSucceeded(string phoneNumber)
    {
        // Show OTP code entry UI
    }

    private void OnOtpCodeRequestFailed((string phoneNumber, string failReason) response)
    {
        Debug.LogError($"Failed: {response.failReason}");
    }

    private void OnOtpCodeSubmissionSucceeded(string code)
    {
        // UserLoggedIn will fire next
    }

    private void OnOtpCodeSubmissionFailed((string code, string failReason) response)
    {
        Debug.LogError($"Invalid code: {response.failReason}");
    }

    private async void OnUserLoggedIn()
    {
        string username = await GeniesSdkAvatar.GetUserNameAsync();
        Debug.Log($"Welcome {username}!");

        // Load user's avatar
        ManagedAvatar avatar = await GeniesSdkAvatar.LoadUserAvatarAsync();
    }

    private void OnUserLoggedOut()
    {
        // Return to login screen
    }

    private void OnDestroy()
    {
        // Always unsubscribe
        GeniesSdkAvatar.Events.UserLoggedIn -= OnUserLoggedIn;
        GeniesSdkAvatar.Events.UserLoggedOut -= OnUserLoggedOut;
        GeniesSdkAvatar.Events.LoginOtpCodeRequestSucceeded -= OnOtpCodeRequestSucceeded;
        GeniesSdkAvatar.Events.LoginOtpCodeRequestFailed -= OnOtpCodeRequestFailed;
        GeniesSdkAvatar.Events.LoginOtpCodeSubmissionSucceeded -= OnOtpCodeSubmissionSucceeded;
        GeniesSdkAvatar.Events.LoginOtpCodeSubmissionFailed -= OnOtpCodeSubmissionFailed;
    }
}
```

## Setting Up Events

Always subscribe to events in `Awake()` and unsubscribe in `OnDestroy()`:

```csharp
private void Awake()
{
    // Subscribe to login/logout events
    GeniesSdkAvatar.Events.UserLoggedIn += OnUserLoggedIn;
    GeniesSdkAvatar.Events.UserLoggedOut += OnUserLoggedOut;

    // Subscribe to OTP flow events
    GeniesSdkAvatar.Events.LoginOtpCodeRequestSucceeded += OnOtpCodeRequestSucceeded;
    GeniesSdkAvatar.Events.LoginOtpCodeRequestFailed += OnOtpCodeRequestFailed;
    GeniesSdkAvatar.Events.LoginOtpCodeSubmissionSucceeded += OnOtpCodeSubmissionSucceeded;
    GeniesSdkAvatar.Events.LoginOtpCodeSubmissionFailed += OnOtpCodeSubmissionFailed;
}

private void OnDestroy()
{
    // Always unsubscribe when cleaning up
    GeniesSdkAvatar.Events.UserLoggedIn -= OnUserLoggedIn;
    GeniesSdkAvatar.Events.UserLoggedOut -= OnUserLoggedOut;
    GeniesSdkAvatar.Events.LoginOtpCodeRequestSucceeded -= OnOtpCodeRequestSucceeded;
    GeniesSdkAvatar.Events.LoginOtpCodeRequestFailed -= OnOtpCodeRequestFailed;
    GeniesSdkAvatar.Events.LoginOtpCodeSubmissionSucceeded -= OnOtpCodeSubmissionSucceeded;
    GeniesSdkAvatar.Events.LoginOtpCodeSubmissionFailed -= OnOtpCodeSubmissionFailed;
}
```

## OTP Login Flow

The OTP login flow consists of multiple steps, each triggering events:

### Step 1: Request OTP Code

```csharp
// Start the OTP login flow
private async void StartLogin()
{
    await GeniesSdkAvatar.StartLoginOtpAsync("+15551234567");
    // LoginOtpCodeRequestSucceeded or LoginOtpCodeRequestFailed events will fire
}

private void OnOtpCodeRequestSucceeded(string phoneNumber)
{
    Debug.Log($"OTP code sent to {phoneNumber}");
    // Show UI to enter the OTP code
    ShowOtpCodeEntryUI();
}

private void OnOtpCodeRequestFailed((string phoneNumber, string failReason) failResponse)
{
    Debug.LogError($"Failed to send OTP to {failResponse.phoneNumber}: {failResponse.failReason}");
    // Handle error - show error message to user
}
```

### Step 2: Submit OTP Code

```csharp
// When user enters the OTP code
private async void SubmitOtpCode(string code)
{
    await GeniesSdkAvatar.SubmitOtpCodeAsync(code);
    // LoginOtpCodeSubmissionSucceeded or LoginOtpCodeSubmissionFailed events will fire
}

private void OnOtpCodeSubmissionSucceeded(string code)
{
    Debug.Log("OTP code verified successfully");
    // UserLoggedIn event will fire next
}

private void OnOtpCodeSubmissionFailed((string code, string failReason) failResponse)
{
    Debug.LogError($"OTP code verification failed: {failResponse.failReason}");
    // Allow user to retry or resend code
}
```

### Step 3: Resend Code (Optional)

```csharp
// Resend OTP code if needed
private async void ResendCode()
{
    await GeniesSdkAvatar.ResendOtpCodeAsync();
    // LoginOtpCodeRequestSucceeded or LoginOtpCodeRequestFailed events will fire
}
```

## Instant Login

Attempt automatic login with cached credentials on startup:

```csharp
private async void Start()
{
    await GeniesSdkAvatar.InitializeAsync();

    // Attempt automatic login with cached credentials
    await GeniesSdkAvatar.TryInstantLoginAsync();

    // UserLoggedIn event will fire if successful
    // If not logged in, show login UI
    if (!GeniesSdkAvatar.IsLoggedIn)
    {
        ShowLoginUI();
    }
}
```

## Logout

Logout triggers the `UserLoggedOut` event:

```csharp
private async void Logout()
{
    await GeniesSdkAvatar.LogOutAsync();
    // UserLoggedOut event will fire when complete
}
```

## Handling Login State Changes

Events drive your application's authentication flow:

```csharp
private void OnUserLoggedIn()
{
    Debug.Log("User successfully logged in");
    // Update UI, load user avatar, etc.
    LoadUserAvatar();
}

private void OnUserLoggedOut()
{
    Debug.Log("User logged out");
    // Update UI, return to login screen, etc.
    ShowLoginUI();
}

private async void LoadUserAvatar()
{
    string username = await GeniesSdkAvatar.GetUserNameAsync();
    Debug.Log($"Welcome {username}!");

    ManagedAvatar avatar = await GeniesSdkAvatar.LoadUserAvatarAsync();
}
```

## Checking Authentication State

You can check the current state at any time:

```csharp
// Check if user is logged in
if (GeniesSdkAvatar.IsLoggedIn)
{
    // User is authenticated
}

// Check if OTP flow is awaiting code submission
if (GeniesSdkAvatar.IsAwaitingOtpCode)
{
    // Show OTP code entry UI
}
```

## Best Practices

1. **Always Use Events**: Don't rely solely on return values or polling state. Events ensure your UI stays in sync with authentication state.

2. **Unsubscribe Properly**: Always unsubscribe from events in `OnDestroy()` to prevent memory leaks.

3. **Handle All Event Cases**: Subscribe to both success and failure events to provide proper user feedback.

4. **Let Events Drive UI**: Use event handlers to transition between UI states (login screen → code entry → authenticated).

5. **Reference Sample**: See `Runtime/Samples/SdkFunctions/Scripts/LoginOtp.cs` for a complete production-ready implementation.

## See Also

- [Avatar Examples](avatar-examples.md) - Loading and managing avatars after authentication
- [API Reference](api-reference.md) - Complete API documentation

