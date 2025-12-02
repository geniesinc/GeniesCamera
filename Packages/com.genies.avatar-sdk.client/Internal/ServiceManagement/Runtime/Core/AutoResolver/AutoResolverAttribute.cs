using System;

namespace Genies.ServiceManagement
{
    public class PreserveAttribute : Attribute
    {
    }

#if UNITY_2018_4_OR_NEWER
    [JetBrains.Annotations.MeansImplicitUse(
                                               JetBrains.Annotations.ImplicitUseKindFlags.Access |
                                               JetBrains.Annotations.ImplicitUseKindFlags.Assign |
                                               JetBrains.Annotations.ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
#endif
    
    
    
    ///<summary>
    /// This attribute can be placed on any type that inherits <see cref="IGeniesInstaller"/> or <see cref="IGeniesInitializer"/>
    /// any types marked with this attribute will have their settings exposed in <see cref="AutoResolverSettings"/> and will be automatically
    /// resolved when calling <see cref="ServiceManager.InitializeAppAsync"/> unless they were configured to be disabled or auto resolving is disabled
    /// in general 
    ///</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoResolveAttribute : PreserveAttribute
    {
        public AutoResolveAttribute()
        {
        }
    }
}
