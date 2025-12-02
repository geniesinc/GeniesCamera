# Installer Requirements System

The Genies Service Management framework includes a requirement system that allows you to declare dependencies between installers using marker interfaces. The system validates dependencies at runtime and provides error messages when requirements are not satisfied.

## Overview

The requirement system uses marker interfaces to declare dependencies:

- `IRequiresInstaller<TInstaller>` - Indicates that this installer requires another specific installer to be registered first
- `IHasInstallerRequirements` - Marker interface that indicates an installer has dependencies

## Basic Usage

### 1. Declare Dependencies with Interfaces

```csharp
// Database installer - no dependencies
public class DatabaseInstaller : IGeniesInstaller
{
    public int OperationOrder => DefaultInstallationGroups.CoreDependency;

    public void Install(IContainerBuilder builder)
    {
        builder.Register<IDatabaseService, DatabaseService>(Lifetime.Singleton);
    }
}

// Auth installer - requires database
public class AuthInstaller : IGeniesInstaller, IRequiresInstaller<DatabaseInstaller>
{
    public int OperationOrder => DefaultInstallationGroups.CoreServices;

    public void Install(IContainerBuilder builder)
    {
        builder.Register<IAuthService, AuthService>(Lifetime.Singleton);
    }
}

// User service - requires both database and auth
public class UserServiceInstaller : IGeniesInstaller,
    IRequiresInstaller<DatabaseInstaller>,
    IRequiresInstaller<AuthInstaller>
{
    public int OperationOrder => DefaultInstallationGroups.PostCoreServices;

    public void Install(IContainerBuilder builder)
    {
        builder.Register<IUserService, UserService>(Lifetime.Singleton);
    }
}
```

### 2. Automatic Dependency Resolution

The framework automatically sorts installers based on their dependencies and validates requirements during installation:

```csharp
public static async void SetupServices()
{
    // Order doesn't matter - the framework automatically sorts them
    var installers = new IGeniesInstaller[]
    {
        new UserServiceInstaller(), // Requires DatabaseInstaller + AuthInstaller
        new DatabaseInstaller(),
        new AuthInstaller(),        // Requires DatabaseInstaller
    };

    try
    {
        await ServiceManager.InitializeAppAsync(installers.ToList());
    }
    catch (ServiceManagerException ex) when (ex.Message.Contains("requirement validation failed"))
    {
        Debug.LogError($"Installer setup failed: {ex.Message}");
    }
}
```

## How It Works

### 1. Automatic Dependency Resolution

The system uses topological sorting to automatically resolve dependencies:

**Dependency Analysis** - Before any installer runs:
- Analyzes all installers and their `IRequiresInstaller<T>` declarations
- Builds a dependency graph of the relationships
- Validates that all required dependencies are present in the collection

**Topological Sorting** - Orders installers automatically:
- Uses Kahn's algorithm to sort installers in dependency order
- Ensures dependencies are always installed before installers that need them
- Detects and reports circular dependencies

**Individual Validation** - Before each installer runs:
- Validates that required installers have already been processed
- Throws `ServiceManagerException` with specific error messages about missing dependencies

### 2. OperationOrder Integration

The requirement system works with the existing `OperationOrder` mechanism. Dependencies are automatically sorted regardless of OperationOrder, and the scope hierarchy handles cross-group dependencies:

```csharp
public class MyLibraryInstallationGroups
{
    // Dependencies are resolved regardless of OperationOrder
    public const int Database = DefaultInstallationGroups.CoreDependency;     // -3000
    public const int Authentication = DefaultInstallationGroups.CoreServices;  // -1000
    public const int BusinessLogic = DefaultInstallationGroups.PostCoreServices; // -500
}

// Dependencies are resolved automatically - BusinessLogic can depend on Authentication
// from any OperationOrder group. The scope hierarchy ensures availability.
```

### 3. Error Messages

The system generates different error messages for various scenarios:

**Missing Dependencies:**
```
Missing required installer dependencies. The following installers are required but not provided:
  UserServiceInstaller requires DatabaseInstaller
  UserServiceInstaller requires AuthInstaller
```

**Circular Dependencies:**
```
Circular dependency detected among installers: UserServiceInstaller, PaymentServiceInstaller.
Remove circular dependencies between IRequiresInstaller declarations.
```

**Individual Validation (if somehow dependencies aren't satisfied):**
```
UserServiceInstaller requires the following installers to be registered first. Ensure they have an earlier OperationOrder:
  - DatabaseInstaller
  - AuthInstaller
```

## Advanced Usage

### Requirement Detection

The requirement system uses static methods in `InstallerRequirementAnalyzer` to detect dependencies through interface reflection:

```csharp
// Detect requirements by type
var requiredTypes = InstallerRequirementAnalyzer.GetRequiredInstallerTypes(typeof(MyInstaller));
var hasRequirements = InstallerRequirementAnalyzer.HasRequirements(typeof(MyInstaller));

// Detect requirements by instance
var installer = new MyInstaller();
var requiredTypes2 = InstallerRequirementAnalyzer.GetRequiredInstallerTypes(installer);
var hasRequirements2 = InstallerRequirementAnalyzer.HasRequirements(installer.GetType());
```

### Multiple Requirements

An installer can require multiple other installers:

```csharp
public class ComplexInstaller : IGeniesInstaller,
    IRequiresInstaller<DatabaseInstaller>,
    IRequiresInstaller<AuthInstaller>,
    IRequiresInstaller<ConfigurationInstaller>,
    IRequiresInstaller<LoggingInstaller>
{
    public int OperationOrder => DefaultInstallationGroups.DefaultServices;

    public void Install(IContainerBuilder builder)
    {
        // Implementation
    }
}
```

### Library Setup Helper

For libraries with complex dependencies, provide a setup helper. Order doesn't matter since dependencies are automatically resolved:

```csharp
public static class MyLibrarySetup
{
    public static IEnumerable<IGeniesInstaller> GetRequiredInstallers(MyLibraryConfig config)
    {
        // Order doesn't matter - automatic dependency resolution handles it
        return new IGeniesInstaller[]
        {
            new UserServiceInstaller(config.UserService), // Will be sorted after its dependencies
            new DatabaseInstaller(config.Database),       // Will be sorted first
            new AuthInstaller(config.Auth),              // Will be sorted after database
        };
    }
}

// Usage:
await ServiceManager.InitializeAppAsync(MyLibrarySetup.GetRequiredInstallers(config).ToList());
```

## Implementation Details

1. **Automatic Dependency Resolution** - Uses topological sorting to automatically order installers
2. **Circular Dependency Detection** - Detects and reports circular dependencies using Kahn's algorithm
3. **Interface-Based Declaration** - Dependencies are declared using `IRequiresInstaller<T>` interfaces
4. **OperationOrder Integration** - Dependencies are sorted within each `OperationOrder` group
5. **Runtime Validation** - The system validates all dependencies are present before installation
6. **Static Analysis** - `InstallerRequirementAnalyzer` uses reflection to detect requirements
7. **Exception Handling** - Validation failures throw `ServiceManagerException` with specific error messages

## System Characteristics

- **Compile-time visibility** - Dependencies are declared through interfaces
- **Runtime validation** - Requirements are checked during installation
- **Error reporting** - Specific error messages identify missing dependencies
- **Performance impact** - Validation occurs only during installation phase
- **Framework compatibility** - Integrates with existing Genies framework features
