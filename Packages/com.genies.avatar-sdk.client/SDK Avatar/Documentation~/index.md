# Genies SDK - Avatar

## Overview

The Genies SDK Avatar package provides a high-level interface for integrating Genies avatars into Unity applications. This package wraps the underlying Genies Avatars implementations, offering simplified APIs for user authentication and avatar loading.

The SDK follows an **event-driven architecture** - subscribe to events to build responsive authentication flows and handle state changes. This pattern ensures your application stays in sync with the SDK's authentication state without polling.

## Features

- **Event-Driven Architecture**: Build responsive authentication flows by subscribing to SDK events for login state changes and OTP flow progression
- **User Authentication**: OTP-based login with automatic session management and instant login support
- **Avatar Loading**: Load default avatars or authenticated user avatars with optional animation controller support

## Getting Started

### Installation

This package is typically installed as a dependency in your Unity project's package manifest.

### Initialization

The SDK initializes automatically when needed, but you can initialize it explicitly:

```csharp
using Genies.Sdk.Avatar;

await GeniesSdkAvatar.InitializeAsync();
```

### Quick Example

```csharp
using Genies.Sdk.Avatar;
using UnityEngine;

public class QuickStart : MonoBehaviour
{
    private async void Start()
    {
        await GeniesSdkAvatar.InitializeAsync();

        // Try automatic login
        await GeniesSdkAvatar.TryInstantLoginAsync();

        if (GeniesSdkAvatar.IsLoggedIn)
        {
            // Load user's avatar
            ManagedAvatar avatar = await GeniesSdkAvatar.LoadUserAvatarAsync();
        }
    }
}
```

## Documentation

- **[Authentication Examples](authentication-examples.md)** - Complete guide to implementing OTP login flows with event-driven patterns
- **[Avatar Examples](avatar-examples.md)** - Examples for loading and managing avatars
- **[API Reference](api-reference.md)** - Complete API documentation

## Sample Implementation

The package includes a sample implementation demonstrating event-driven authentication:

- `Runtime/Samples/SdkFunctions/Scripts/LoginOtp.cs` - Complete OTP login flow using events
- `Runtime/Samples/SdkFunctions/Scripts/RuntimeSdkFunctions.cs` - Integration example with avatar spawning

Import the **SdkFunctions** sample from the Package Manager to see these examples in action.

## Key Concepts

### Event-Driven Architecture

The SDK uses events to notify your application of state changes. Always subscribe to relevant events in `Awake()` and unsubscribe in `OnDestroy()`:

```csharp
private void Awake()
{
    GeniesSdkAvatar.Events.UserLoggedIn += OnUserLoggedIn;
    GeniesSdkAvatar.Events.UserLoggedOut += OnUserLoggedOut;
}

private void OnDestroy()
{
    GeniesSdkAvatar.Events.UserLoggedIn -= OnUserLoggedIn;
    GeniesSdkAvatar.Events.UserLoggedOut -= OnUserLoggedOut;
}
```

### Authentication State

Check the current authentication state:

```csharp
bool isLoggedIn = GeniesSdkAvatar.IsLoggedIn;
bool isAwaitingCode = GeniesSdkAvatar.IsAwaitingOtpCode;
```

## Dependencies

- `com.genies.avatars.sdk`: 2.0.3

## Unity Version

Requires Unity 2022.3 or later.

## Support

For support, please contact: engineering@genies.com

## License

© 2025 Genies, Inc. All rights reserved.
