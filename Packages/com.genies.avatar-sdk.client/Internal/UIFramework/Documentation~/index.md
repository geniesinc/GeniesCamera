# Genies UIFramework Package Documentation

**Version:** 1.6.0  
**Unity Version:** 2022.3.32f1 or higher  
**Namespace:** `Genies.UIFramework`, `Genies.UI.Widgets`, `Genies.UI.Transitions`, `Genies.UI.Scroller`

## Overview

The **Genies UIFramework** package provides a comprehensive suite of UI tools and components for building modern, animated, and responsive user interfaces within the Genies ecosystem. This framework offers advanced popup management, smooth transition animations, interactive button components, scrolling utilities, and a flexible widget system that integrates seamlessly with Unity's UI system and the broader Genies platform.

## Key Features

### 🎭 **Advanced Popup Management**
- Comprehensive popup system with multiple layout types (SingleButton, DualButton, InputField, Custom)
- Configuration-driven popup creation with `PopupConfig` and predefined `PopupType` enums
- Rich styling system with `PopupStyle` and `PopupButtonStyle` for visual customization
- Stack-based popup management with hide latest/all functionality
- Support for custom button actions and event handling

### 🎬 **Smooth Transition Animations**
- Complete transition animation system via `TransitionService`
- Support for slide, scale, and fade transition types
- Async/await pattern support for seamless animation chaining
- `ITransitionable` interface for consistent animation capabilities across components
- DOTween integration for high-performance animations

### 🔘 **Interactive Button Components**
- Feature-rich `GeniesButton` with extensive animation support
- Enable/disable, click, selection, and long press animations
- Customizable animation curves and timing parameters
- Comprehensive pointer event handling (click, hover, enter, exit)
- Selection state management with visual feedback

### 📜 **Scrolling and Layout Utilities**
- `RectTransformExtensions` for advanced layout calculations
- `ScrollRectExtensions` for smooth scrolling and centering operations
- World space rect calculations and containment checks
- Normalized scroll distance calculations and positioning utilities

### 🧩 **Flexible Widget System**
- Base `Widget` class with automatic transition animation support
- `ISelectable` interface for selection-capable widgets
- Modular widget architecture with extensible lifecycle management
- Automatic RectTransform and CanvasGroup component management

### 🎮 **Interaction and Control Systems**
- Advanced `InteractionController` for drag and touch input
- `GenieInteractionController` with service management integration
- Configurable interaction parameters and velocity curves
- Pointer event handling with comprehensive gesture support

## Core Architecture

### Popup System Architecture
```csharp
IPopupSystem → PopupSystem → PopupBuilder → PopupConfigMapper
     ↓              ↓             ↓
PopupConfig → PopupStyle → PopupButtonStyle
```

### Transition System Architecture
```csharp
ITransitionable → TransitionService → TransitionType
     ↓                    ↓
Widget/GeniesButton → DOTween Animation
```

### Widget System Architecture
```csharp
Widget (Base) → ITransitionable + ISelectable
     ↓
Custom Widgets (Containers, CTAs, Scrolling, etc.)
```

## Usage Examples

### Basic Popup Creation

```csharp
public class PopupExample : MonoBehaviour
{
    private IPopupSystem _popupSystem;
    
    private void Start()
    {
        _popupSystem = new PopupSystem(transform, new List<ScriptablePopupConfig>());
        ShowExamplePopup();
    }
    
    private void ShowExamplePopup()
    {
        var config = new PopupConfig(
            popupLayout: PopupLayout.DualButton,
            header: "Confirm Action",
            content: "Are you sure you want to proceed?",
            placeholderInputFieldText: null,
            topImage: null,
            buttonLabels: new List<string> { "Cancel", "Confirm" },
            hasCloseButton: true
        );
        
        var actions = new List<UnityAction>
        {
            () => Debug.Log("Cancelled"),
            () => Debug.Log("Confirmed")
        };
        
        _popupSystem.Show(config, actions);
    }
}
```

### Transition Animations

```csharp
public class TransitionExample : MonoBehaviour, ITransitionable
{
    public RectTransform RectTransform { get; private set; }
    public CanvasGroup CanvasGroup { get; private set; }
    
    private TransitionService _transitionService;
    
    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        CanvasGroup = GetComponent<CanvasGroup>();
        _transitionService = new TransitionService();
    }
    
    public async void ShowWithTransition()
    {
        await _transitionService.DoTransitionIn(this, TransitionType.SlideUp);
    }
    
    public async void HideWithTransition()
    {
        await _transitionService.DoTransitionOut(this, TransitionType.Fade);
    }
}
```

### Advanced Button Configuration

```csharp
public class ButtonExample : MonoBehaviour
{
    [SerializeField] private GeniesButton myButton;
    
    private void Start()
    {
        ConfigureButton();
        RegisterEvents();
    }
    
    private void ConfigureButton()
    {
        myButton.AnimateOnClick = true;
        myButton.AnimateOnSelection = true;
        myButton.EnableLongPress = true;
        myButton.HoldDuration = 2f;
        myButton.OnClickAnimTime = 0.3f;
        myButton.OnClickScaleRange = new Vector2(0.9f, 1f);
    }
    
    private void RegisterEvents()
    {
        myButton.onClick.AddListener(OnButtonClicked);
        myButton.OnLongPress.AddListener(OnLongPressDetected);
    }
    
    private void OnButtonClicked()
    {
        Debug.Log("Button clicked!");
        myButton.SetButtonSelected(!myButton.IsSelected);
    }
    
    private void OnLongPressDetected()
    {
        Debug.Log("Long press detected!");
    }
}
```

### Custom Widget Development

```csharp
public class CustomWidget : Widget, ISelectable
{
    private bool _isSelected;
    public bool IsSelected 
    { 
        get => _isSelected; 
        set 
        {
            _isSelected = value;
            UpdateVisualState();
        } 
    }
    
    public override void OnWidgetInitialized()
    {
        base.OnWidgetInitialized();
        Debug.Log("Custom widget initialized");
    }
    
    private void UpdateVisualState()
    {
        var image = GetComponent<Image>();
        if (image != null)
        {
            image.color = IsSelected ? Color.yellow : Color.white;
        }
    }
}
```

### Scrolling Utilities

```csharp
public class ScrollExample : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform targetElement;
    
    public void ScrollToCenter()
    {
        float centerPosition = scrollRect.GetScrollToCenterNormalizedPosition(targetElement);
        StartCoroutine(SmoothScrollTo(centerPosition));
    }
    
    private IEnumerator SmoothScrollTo(float targetPosition)
    {
        float startPosition = scrollRect.verticalNormalizedPosition;
        float duration = 0.5f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPosition, targetPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        scrollRect.verticalNormalizedPosition = targetPosition;
    }
}
```

## Package Structure

```
Genies.UIFramework/
├── Runtime/
│   ├── PopupSystem/           # Advanced popup management
│   ├── Transitions/           # UI transition animations
│   ├── Buttons/               # Interactive button components
│   ├── Controls/              # UI interaction controllers
│   ├── Extensions/            # Utility extension methods
│   ├── Widgets/               # Modular UI widget system
│   ├── Popups/                # Specialized popup implementations
│   ├── Input Handlers/        # Input processing components
│   └── Fonts/                 # Typography assets
├── Tests/                     # Unit and integration tests
└── Samples~/                  # Example implementations
```

## Dependencies

### Core Dependencies
- **com.genies.camerasystem** (1.0.13): Camera system integration
- **com.genies.servicemanagement** (1.1.1): Service management framework
- **com.genies.thirdparty.dotween** (1.1.0): High-performance tweening
- **com.genies.utilities** (1.6.0): Utility functions and extensions
- **com.genies.thirdparty.unitask** (1.1.0): Async/await operations
- **com.genies.crashreporting** (1.0.0): Error handling and reporting

### Unity Dependencies
- **com.unity.textmeshpro** (3.0.8): Advanced text rendering
- **com.unity.ugui** (1.0.0): Unity GUI system foundation
- **com.unity.inputsystem** (1.7.0): Modern input handling

## Best Practices

### Popup Management
- Use configuration-driven design with `PopupConfig` for consistency
- Leverage `PopupStyle` and `PopupButtonStyle` for visual coherence
- Organize button actions in logical order for intuitive UX
- Use `PopupType` enum for commonly used popup configurations

### Animation Performance
- Use async/await patterns for smooth animation chaining
- Choose appropriate transition types based on UI context
- Limit concurrent animations for optimal performance
- Ensure proper state reset between transitions

### Widget Development
- Extend `Widget` base class for consistent behavior
- Implement `ISelectable` for selection-capable widgets
- Cache frequently accessed components in `OnWidgetInitialized`
- Leverage built-in transition support for smooth interactions

### Button Configuration
- Use custom animation curves for unique button personalities
- Implement proper enabled/disabled state handling
- Configure appropriate hold durations for long press actions
- Provide clear visual feedback for all interaction states

## Integration

The UIFramework seamlessly integrates with other Genies packages:

```csharp
public class UIFrameworkIntegration : MonoBehaviour
{
    private async void Start()
    {
        // Service management integration
        var popupSystem = ServiceManager.Resolve<IPopupSystem>();
        
        // Camera system integration
        var cameraSystem = ServiceManager.Resolve<ICameraSystem>();
        
        // Crash reporting integration
        CrashReporter.AddLogger(new UIFrameworkLogger());
        
        Debug.Log("UI Framework integrated with Genies ecosystem");
    }
}
```

## Conclusion

The **Genies UIFramework** provides a comprehensive, modern, and highly performant UI system that seamlessly integrates with the broader Genies ecosystem. Its combination of advanced popup management, smooth transition animations, feature-rich interactive components, and flexible widget architecture enables developers to create sophisticated, responsive, and visually appealing user interfaces.

The framework's emphasis on configuration-driven design, async/await patterns, and extensive customization options ensures that UI development is both efficient and flexible. The integration with Unity's UI system, combined with Genies-specific enhancements like service management and crash reporting, provides a robust foundation for building production-quality user interfaces.

For additional support, advanced integration scenarios, or questions about UI framework implementation, please refer to the package changelog, sample implementations, or contact the Genies engineering team.
