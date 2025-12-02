# Genies UGC Package

## Overview

The Genies UGC (User-Generated Content) package provides a comprehensive system for creating, managing, and rendering customizable avatar content within Unity. This package enables developers to build rich avatar customization experiences with support for custom patterns, wearable items, styling systems, and real-time rendering.

## Key Features

- **Custom Pattern System**: Create and manage custom textures and patterns for avatar customization
- **Advanced Rendering**: Real-time rendering of wearable items with multi-region styling support  
- **Template System**: Flexible template-based content creation and configuration
- **Model Framework**: Robust data modeling with deep copying and comparison capabilities
- **Material System**: Advanced material management with region-based styling
- **Animation Support**: Built-in animation system for visual effects and transitions

---

## Core Architecture

### Service Layer

The UGC package is built around a service-oriented architecture with dependency injection support:

```csharp
// Main services are registered via installers
[AutoResolve]
public class UgcServicesInstaller : IGeniesInstaller
{
    public void Install(IContainerBuilder builder)
    {
        // Registers core UGC services
        builder.Register<IAssetsProvider<ElementContainer>, LabeledAssetsProvider<ElementContainer>>(Lifetime.Singleton);
    }
}
```

### Model System

All UGC data models implement a consistent pattern for comparison and copying:

```csharp
public interface IModel<T> : IModel, ICopyable<T>
{
    bool IsEquivalentTo(T other);  // Value-based comparison
    int ComputeHash();            // Content-based hashing
    T DeepCopy();                 // Safe copying
}
```

---

## Custom Pattern System

### ICustomPatternService

The custom pattern system allows users to create and manage personalized textures and patterns:

**Key Methods:**
- `CreateOrUpdateCustomPatternAsync()` - Create new patterns or update existing ones
- `LoadCustomPatternTextureAsync()` - Load pattern textures by ID
- `DeletePatternAsync()` - Remove patterns from the system
- `GetAllCustomPatternIdsAsync()` - Retrieve all available pattern IDs

**Example Usage:**
```csharp
// Inject the service
[Inject] private ICustomPatternService _patternService;

// Create a new custom pattern
string patternId = await _patternService.CreateOrUpdateCustomPatternAsync(
    customTexture, 
    patternConfig, 
    null // null for new pattern
);

// Load the pattern texture
var textureRef = await _patternService.LoadCustomPatternTextureAsync(patternId);
```

**Service Configuration:**
```csharp
[AutoResolve]
public class CustomPatternServiceInstaller : IGeniesInstaller
{
    public void Install(IContainerBuilder builder)
    {
        // Registers pattern services with cloud save support
        builder.Register<ICustomPatternService, CustomPatternRemoteLoaderService>(Lifetime.Singleton);
    }
}
```

---

## Rendering System

### IWearableRenderer

Factory interface for creating render instances:

```csharp
public interface IWearableRenderer
{
    UniTask<IWearableRender> RenderWearableAsync(Wearable wearable);
    UniTask<IElementRender> RenderElementAsync(string elementId, string materialVersion = null);
}
```

### IWearableRender

Manages complete wearable items containing multiple elements:

**Key Features:**
- Multi-element management within a single wearable
- Solo rendering controls for element visibility
- Animation management across all elements
- Bounds calculation for entire wearables

**Example Usage:**
```csharp
// Render a complete wearable
IWearableRender wearableRender = await renderer.RenderWearableAsync(wearableData);

// Control element visibility
wearableRender.SetElementIdSoloRendered("shirt", true); // Show only shirt
wearableRender.ClearAllSoloRenders(); // Show all elements

// Animate specific elements
wearableRender.PlayAnimation("shirt", colorAnimation);
```

### IElementRender

Handles individual UGC elements with advanced styling support:

**Key Features:**
- Region-based styling system
- Animation control per region
- Debug visualization support
- Bounds calculation and alignment

**Example Usage:**
```csharp
// Render an individual element
IElementRender elementRender = await renderer.RenderElementAsync("shirt_element");

// Apply styling to specific regions
await elementRender.ApplyStyleAsync(redStyle, regionIndex: 0);
await elementRender.ApplyStyleAsync(blueStyle, regionIndex: 1);

// Control animations
elementRender.PlayRegionAnimation(regionIndex: 0, flashAnimation, playAlone: true);
```

---

## Material System

### IMegaMaterialBuilder

Builds advanced materials supporting multiple regions and styling:

```csharp
public interface IMegaMaterialBuilder
{
    UniTask<MegaMaterial> BuildMegaMaterialAsync(Split split);
    MegaMaterial BuildMegaMaterial(Ref<UgcElementAsset> elementRef, string materialVersion = null);
}
```

### MegaMaterial

Encapsulates Unity Material instances with advanced region-based styling:

**Key Features:**
- Multi-region color and pattern support
- Projected texture compositing  
- Debug visualization modes
- Resource management and disposal

**Example Usage:**
```csharp
// Build material from split configuration
MegaMaterial megaMaterial = await builder.BuildMegaMaterialAsync(splitConfig);

// Apply to renderer
elementRender.UseDefaultColors = false;
await elementRender.ApplySplitAsync(splitConfig);
```

---

## Template System

### IUgcTemplateDataService

Manages UGC template data for content creation:

```csharp
public interface IUgcTemplateDataService
{
    UniTask<UgcTemplateData> FetchTemplateDataAsync(string templateId);
    UniTask<UgcTemplateSplitData> FetchSplitDataAsync(int splitIndex, string templateId);
    UniTask<UgcTemplateElementData> FetchElementDataAsync(string elementId);
}
```

**Usage Example:**
```csharp
// Fetch complete template
UgcTemplateData template = await templateService.FetchTemplateDataAsync("shirt_template_v1");

// Get specific split configuration  
UgcTemplateSplitData splitData = await templateService.FetchSplitDataAsync(0, "shirt_template_v1");

// Fetch element details
UgcTemplateElementData elementData = await templateService.FetchElementDataAsync("shirt_front_panel");
```

---

## Data Models

### Core Model Types

**Wearable**: Complete wearable item containing multiple elements
```csharp
public class Wearable : IModel<Wearable>
{
    public string TemplateId { get; set; }
    public List<Split> Splits { get; set; }
    public HashSet<string> Tags { get; set; }
}
```

**Split**: Individual element within a wearable with region-based styling
```csharp
public class Split : IModel<Split>
{
    public string ElementId { get; set; }
    public List<Region> Regions { get; set; }
    public bool UseDefaultColors { get; set; }
    public List<ProjectedTexture> ProjectedTextures { get; set; }
}
```

**Region**: Specific area within an element that can be styled independently
```csharp
public class Region : IModel<Region>
{
    public int RegionNumber { get; set; }
    public Style Style { get; set; }
}
```

### Utility Types

**ValueRange**: Represents bounded value ranges with clamping
```csharp
public struct ValueRange
{
    public float Default { get; }
    public float Min { get; }
    public float Max { get; }
    
    public float Clamp(float value);
    public bool IsInRange(float value);
}
```

**ValueAnimation**: CSS-inspired animation system for visual effects
```csharp
public struct ValueAnimation
{
    public float StartValue { get; set; }
    public float EndValue { get; set; }
    public float Duration { get; set; }
    public AnimationDirection Direction { get; set; }
    
    public UniTask StartAnimationAsync(Action<float> updateCallback);
}
```

---

## Getting Started

### 1. Service Setup

Ensure the UGC services are properly registered in your dependency injection container:

```csharp
// Services are auto-registered via the AutoResolve attribute
// UgcServicesInstaller and CustomPatternServiceInstaller are automatically discovered
```

### 2. Basic Wearable Rendering

```csharp
public class UgcExample : MonoBehaviour
{
    [Inject] private IWearableRenderer _renderer;
    
    public async void RenderWearable()
    {
        // Create wearable configuration
        var wearable = new Wearable
        {
            TemplateId = "shirt_template",
            Splits = new List<Split>
            {
                new Split
                {
                    ElementId = "shirt_front",
                    Regions = new List<Region>
                    {
                        new Region { RegionNumber = 0, Style = redStyle },
                        new Region { RegionNumber = 1, Style = blueStyle }
                    }
                }
            }
        };
        
        // Render the wearable
        IWearableRender render = await _renderer.RenderWearableAsync(wearable);
        
        // Position in scene
        render.Root.transform.position = Vector3.zero;
    }
}
```

### 3. Custom Pattern Creation

```csharp
public class PatternExample : MonoBehaviour
{
    [Inject] private ICustomPatternService _patternService;
    
    public async void CreateCustomPattern()
    {
        // Create pattern from texture
        Texture2D customTexture = LoadTextureFromFile();
        
        var patternConfig = new Pattern
        {
            // Configure pattern properties
        };
        
        // Create the pattern
        string patternId = await _patternService.CreateOrUpdateCustomPatternAsync(
            customTexture, 
            patternConfig
        );
        
        Debug.Log($"Created pattern: {patternId}");
    }
}
```

### 4. Animation Example

```csharp
public class AnimationExample : MonoBehaviour
{
    public void PlayColorAnimation(IElementRender elementRender)
    {
        var colorAnimation = new ValueAnimation
        {
            StartValue = 0f,
            EndValue = 1f,
            Duration = 2f,
            Direction = ValueAnimation.AnimationDirection.Alternate,
            IterationCount = -1 // Infinite
        };
        
        elementRender.PlayRegionAnimation(regionIndex: 0, colorAnimation);
    }
}
```

---

## Advanced Features

### Region Debugging

Enable visual debugging to see region boundaries:

```csharp
elementRender.RegionDebugging = true; // Shows colored region overlays
wearableRender.RegionDebugging = true; // Enables debugging for entire wearable
```

### Material Versioning

Support different material versions for backwards compatibility:

```csharp
IElementRender render = await renderer.RenderElementAsync("element_id", materialVersion: "v2.0");
```

### Projected Textures

Apply textures that are composited in UV space:

```csharp
var split = new Split
{
    ProjectedTextures = new List<ProjectedTexture>
    {
        new ProjectedTexture { /* texture configuration */ }
    }
};
```

---

## Best Practices

### Memory Management
- Always dispose render instances when done: `render.Dispose()`
- Use `ClearAllSoloRenders()` to reset visibility states
- Monitor texture reference counts in custom patterns

### Performance Optimization  
- Cache frequently used materials and textures
- Use material versioning to minimize rebuilding
- Batch styling operations when possible
- Use region debugging only during development

### Error Handling
```csharp
try
{
    var render = await renderer.RenderWearableAsync(wearable);
}
catch (System.Exception ex)
{
    Debug.LogError($"Failed to render wearable: {ex.Message}");
}
```

---

## API Reference

For detailed API documentation, refer to the XML documentation comments in the source code. All public interfaces and classes include comprehensive documentation with parameter descriptions, return value information, and usage examples.

Key namespaces:
- `Genies.Ugc` - Core UGC functionality
- `Genies.Ugc.CustomPattern` - Custom pattern system
- `Genies.Ugc.Models` - Data model definitions

---

## Support

For additional support and examples, refer to:
- Package source code with XML documentation
- Unity console for runtime error messages  
- Dependency injection container for service resolution issues
