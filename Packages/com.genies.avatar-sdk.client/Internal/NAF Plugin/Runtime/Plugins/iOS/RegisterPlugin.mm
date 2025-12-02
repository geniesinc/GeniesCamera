#import "UnityAppController.h"
#include "Unity/IUnityGraphics.h"

// -----------------------------------------------------------------------------
// Add this file under Plugins/iOS folder in your Unity project
// -----------------------------------------------------------------------------
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces);
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload();

@interface MyAppController : UnityAppController
{
}
- (void)shouldAttachRenderDelegate;
@end
@implementation MyAppController
- (void)shouldAttachRenderDelegate
{
	// On iOS, we are statically linking the plugin, so unlike desktops
    // where the plugin is a dynamic library which is automatically loaded
    // and registered, we need to do that manually.
	UnityRegisterRenderingPluginV5(&UnityPluginLoad, &UnityPluginUnload);
}

@end
IMPL_APP_CONTROLLER_SUBCLASS(MyAppController);