# Feature Flags

This package unifies our feature flag management so that all projects/packages can share the same feature flags if needed. Any shared feature flags should be added to 
[SharedFeatureFlags.cs](..%2FRuntime%2FSharedFeatureFlags.cs) a project can have its own feature flags that aren't shared, all you need to do is create a feature flag container.

NOTE: Make sure to mark it with `[FeatureFlagsContainer]` this way we can automate collecting all feature flags to make it easier to configure them.


```
[FeatureFlagsContainer()]
public sealed class MyFeatureFlags
{
    public const string MyCustomFeatureFlag = "my_custom_feature_flag";
}
```

## Creating Feature Config

- To configure feature flags locally, create a new [FeatureConfig.cs](..%2FRuntime%2FFeatureConfig.cs) 
  - Right click in the project view > Create > Genies > Feature Config

## Initializing a new instance

The [FeatureManager.cs](..%2FRuntime%2FFeatureManager.cs) class hides its constructors. To create an instance call `FeatureManager.CreateInstance` you can access this instance as a singleton using `FeatureManager.Instance` in your package/project.

## AB Testing Service

The AB Testing Service is left to the implementer for flexibility, but we usually use Statsig. 

## Notes

- The reason we didn't allow inheriting the `SharedFeatureFlags` is to ensure interoperability by default. 
  - This means that in your code you need to use either your feature flags or the shared ones as opposed to how it used to work which was a single enum
  - This might be a bit inconvenient but its not breaking enough to make us worry.
- If a feature flag needs to move from the project to shared, you will need to update the calling code.