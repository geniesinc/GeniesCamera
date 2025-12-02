# Camera Focus
This system toggles the avatar animations based on the different configurable states. All animations triggers depend on this system.

## How to Add New Animations
1. Make the required changed to the animator controller from the Unity Editor.
2. Add the Animator Editor parameters to AnimationTransitionNameConstants.cs
3. Mapp the triggers to the corresponding state
```
CameraFocusState.CompleteAvatarFocus, new HashSet<string>()
{
    AnimationTransitionNameConstants.MALE_TRANSITION_TO_FLOATING, 
    AnimationTransitionNameConstants.FEMALE_TRANSITION_TO_FLOATING,
    AnimationTransitionNameConstants.MALE_TRANSITION_TO_IDLE2,
    AnimationTransitionNameConstants.FEMALE_TRANSITION_TO_IDLE2
});
```