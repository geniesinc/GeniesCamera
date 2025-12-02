
# How To Use Service Manager

## DI Style Registration

----------

> 📝
> 
> Should be the default way of registering/initialization your app services as it allows us to fully control the lifetime.
> 
> When you do registration this way, VContainer will handle auto creation and injection of dependencies into constructors.
> 
> -   Use OperationOrder to control when installation/initialization happens between multiple installers/initializers
> 
> -   For OperationOrder use const values instead of your inline integers. You can also just use the
> 
>     DefaultInstallationGroups
> 
> 
> Notes
> 
> -   If you type inherits IDisposable when the scope dies, Dispose will be called on the instances it is managing


### Creating Installer

```
public interface IBaseCoreService {}
public interface ICoreService : IBaseCoreService {}
public interface CoreServiceImp : ICoreService {}

public MyCoreServicesInstaller : IGeniesInstaller
{
   public int OperationOrder => MyInstallationGroups.CoreServices;
   public void Install(IContainerBuilder builder)
        {
            builder.Register<ICoreService, CoreServiceImp>(Lifetime.Singleton)
                   .AsSelf() //Make sure it can be resolved as CoreServiceImp
                   .As<IBaseCoreService>(); //Make sure it can be resolved as IBaseCoreService
        }
}
```

> 📝 Follow VContainer guide for more registration options [https://vcontainer.hadashikick.jp/registering/register-type](https://vcontainer.hadashikick.jp/registering/register-type)

### Creating Initializers

```
public interface IBaseCoreService { void Initialize() }
public interface ICoreService : IBaseCoreService {}
public interface CoreServiceImp : ICoreService 
{ 
  void Initialize() { Debug.Log("Initialized") }
}

public MyCoreServicesInitializer : IGeniesInitializer
{
   public int OperationOrder => MyInstallationGroups.CoreServices;
   
    public async UniTask Initialize()
    {
        ServiceManager.Get<ICoreService>().Initialize();
    }
}
```

> 📝 You can have a single class that does installation and initialization, you don’t need to split them.

### Creating Scopes

Once installers/initializers are created

#### Creating Scopes at app root level

This should be called once in the app lifetime in the beginning

```
async void Awake()
{
    await ServiceManager.InitializeAppAsync(new List<IGeniesInstaller>()
    {
        new MyCoreServicesInstaller(),
    }, new List<IGeniesInitializer>() 
    {
       new MyCoreServicesInitializer()
    });
}
```

> 📝 InitializeAppAsync will also install Auto Resolver types

#### Creating scopes after app initialization

```
async void InitializeMyClass()
{
    await ServiceManager.CreateScopeAsync(myObj, 
    new List<IGeniesInstaller>()
    {
       new MyCoreServicesInstaller(),
    }, 
    new List<IGeniesInitializer>() 
    {
       new MyCoreServicesInitializer()
    });
}
```

> 📝 If you want the scope to be global/not destroyed on scene load, pass dontDestroyOnLoad: false

### Creating Auto Resolvers

```
public interface IBaseCoreService 
{ 
    void Initialize(MyCoreServiceConfiguration config); 
}
public interface ICoreService : IBaseCoreService {}
public interface CoreServiceImp : ICoreService 
{
    void Initialize(MyCoreServiceConfiguration config)
    {
        Debug.Log("Initialized")
    }
}

[AutoResolve]
public MyCoreServicesInstaller : IGeniesInstaller
{
   [SerializedField]
   private MyCoreServiceConfiguration config;
   
   public int OperationOrder => MyInstallationGroups.CoreServices;
   
   public void Install(IContainerBuilder builder)
        {
            builder.Register<ICoreService, CoreServiceImp>(Lifetime.Singleton)
                   .AsSelf() //Make sure it can be resolved as CoreServiceImp
                   .As<IBaseCoreService>(); //Make sure it can be resolved as IBaseCoreService
        }
}
```

> 📝 Classes marked with [AutoResolve] that inherit IGeniesInstaller, IGeniesInitializer or both will be auto managed when we call ServiceManager.InitAppAsync

### Configuring Auto Resolvers

> 📝 You can enable/disable installer/initializer for auto resolvers as needed.
> If the window bugs out just re-open it.

![AutoResolverSettings.png](Images%2FAutoResolverSettings.png)

## Service Locator Style Registration

----------

Simple to use, you just need an instance.

> 📝
>
> Use cases
>
> -   App level configuration
>
>-   Long lived instances
>
>-   If you need to specify an ID for your registrations
>
>
>Notes
>
>-   You can’t unregister, once registered the instance will stay.
>
>-   If you instance inherits IDisposable it will be called when we dispose the whole container holding that instance.


### Registration

```
async void InitializeMyClass()
{
    ServiceManager.RegisterService<MyClass>(this, myOptionalId)
                  .As<MyBaseClass>();
}
```

## Resolving Services

----------

### Different Types of Resolving

```
async void ResolveMyDependencies()
{
    ServiceManager.Get<T>(this, optionalId);
    ServiceManager.GetLazy<T>(this, optionalId);
    ServiceManager.GetCollection<T>(this, optionalId);
    ServiceManager.GetLazyCollection<T>(this, optionalId);
}
```

> 📝
> 
> -   Use GetLazy and GetLazyCollection when you want to delay accessing the service
> 
> -   GetCollection is used to get all the registrations of a type. Will return a single instance if there aren’t more registrations. Most of the times you won’t need it