
# The Service Manager Framework
A framework for increasing the modularity of our codebase by introducing DI/IoC and service locator patterns for managing our services.

## Recording

---------
https://drive.google.com/file/d/18KGF71gelazJCoiFTJeIjL4NomZsr4Jy/view

## Intro

----------

Effective management of dependencies is a cornerstone in the realm of Unity app development, ensuring a seamless interplay among the various components of an App. _**There's no silver bullet to it, there are no bad patterns, good patterns or perfect patterns**_. There are always trade-offs and it will always depends on your use case.

That said there are a few approaches that can be employed to achieve this each with their own pros and cons.

## Dependency Management Patterns in Unity

----------

### GameObject + Components

-   Essentially a service locator pattern, each Gameobject is its own container and locator.

-   It requires that all your services are scoped and inherit `Monobehavior`

-   Harder to test/mock

-   Lack of control over construction

-   Performance overhead


### Singletons

Good for prototyping but bad for modularity and testing.

#### Singleton Instance Provider

-   Static class providing a single instance

-   Messy when you have too many


#### Singleton Registry

-   Singleton class that holds all your instances

-   Bloated and can lead to conflicts

-   Non modular as its context specific


#### Singleton Instance

-   Single Instance with static accessor

-   Useful when you know you will only have a single implementation (Analytics, plugins, etc..)

-   Can only have 1

-   Harder to mock and test


#### Conclusion

-   Singletons aren’t bad, but they’re not good at scale

-   Should be used on a case by case basis

-   Use for prototyping if you don’t need to implement at scale yet.


### **DI (Dependency Injection)**

DI provides classes with their dependencies, typically via constructors. Instead of a class creating its own dependencies, they're 'injected' into it.
Using a DI framework automates this process, distinguishing between Manual and Automated DI.
Below are some notes about manual DI.

**Pros:**

-   Facilitates testing.

-   Promotes modularity.

-   Simplifies maintenance.

-   Enforces clear dependencies.


**Cons:**

-   Unity's Monobehaviors can't easily use DI.

-   Frameworks might use clunky workarounds.

-   Large projects complicate dependency management.

-   Can result in cluttered code.


**Conclusion:**

While DI excels at dependency management generally but it presents specific challenges in Unity.

### Service Locator

A service locator pattern is similar to the singleton registry the main difference being that you don't hold explicit references but you register using types/ids and query for instances. It is a much better option and is more generic but not without its own issues.

#### Pros

-   Great for unity. Monobehaviors can query for services using a singleton service locator.

-   Great for modularity

-   Simple


#### Cons

-   Tends to create tight coupling (not an issue for us per se) to the service locator itself in your classes

-   Mocking objects isn't as easy as DI

-   Hidden dependencies since you rely on the constructor of an object less

-   Singleton services only, can probably introduce a more complex scoped/transient instances but requires too much boilerplate


### Conclusion

-   All the above options are viable and have their own scenarios where they can be useful!

-   There is no silver bullet that solves it all but there are bullets that cover our own use cases!


## **Our Goals**

----------

We're currently facing challenges with tightly coupled dependencies, with a heavy reliance on singletons and a makeshift service locator. Our aim is to bring about modular and testable code. Although immediate full-scale changes aren't feasible, it's essential to gradually shift toward our desired architecture.

**Current Issues:**

-   Over-reliance on tight dependencies.

-   Inadequate dependency management across packages/projects.

-   Overuse of singletons leading to no control over dependency lifetimes.

-   Over-complicated app start-up due to numerous components.


**Our Targets:**

-   Develop a straightforward dependency management framework, avoiding the pitfalls of complex third-party systems.

-   Maintain high performance.

-   Ensure adaptability in app installation and dependency resolution.

-   Scalable dependency management, especially as we expose more code to external parties. This includes nuanced control of services as our apps expand into diverse "worlds" or "zones."

-   Enhance code testability, paving the way for robust integration tests.

-   Implement a consistent method to handle dependencies throughout our client codebase.


## **DI Frameworks Explained**

----------

For scalable requirements, we'll adapt the VContainer framework—a streamlined and efficient DI tool for Unity. We're customizing it since standard DI frameworks in Unity have complexities, especially regarding Monobehavior dependencies.

**Key Concepts in DI Frameworks:**

-   **Scope/Lifetime Scope:**

    -   Primary place for registering services.

    -   Typically a Monobehavior component.

    -   Lets you link dependency interfaces to their actual forms.

    -   Dependencies within a scope are tied to its lifecycle.

    -   Hierarchical: Child scopes can use parent scope services if they lack specific bindings.

-   **Binder/Builder:**

    -   Every scope has a registration mechanism to associate services with their functions.

    -   Post-registration, scopes interpret this map to produce or access services.

    -   This process yields a container, ready to dispense services.

-   **Container/Resolver:**

    -   Active post-building, it manages different service types and their delivery methods.

    -   On demand, it supplies a pre-made or new instance.

    -   It governs service injection, often marked by the [Inject] attribute.

-   **Lifetime:**

    -   Defines a service's availability duration.

    -   **Singleton:** One-time creation, consistent provision. Hierarchical clashes might arise in some tools.

    -   **Scoped:** Like Singleton, but child scopes can have distinct instances.

    -   **Transient:** New instance per request—suitable for stateless elements, like characters.


#### Visualizing it all

![vcontainerflow.png](Images%2Fvcontainerflow.png)

Link: [![](https://excalidraw.com/favicon-16x16.png)Excalidraw — Collaborative whiteboarding made easy](https://excalidraw.com/#json=0adIaeQNnwIBRvsfjCJAq,8q-Odzmcb41X5lj9mq-hpg)

**Example**
```csharp
public interface IPathFindingService {}
public class AStartPathFindingService {}
public class Character
{
    IPathFindingService service;
    public Character(IPathFindingService injectedService)
    {
       service = injectedService;
    }
    
}

public class MyLifetimeScope : LifetimeScope
{
   public void Register(IContainerBuilder builder)
   {
      //One instance shared across all other services
      builder.Register<IPathFindingService, AStarPathFindingService>(Lifetime.Singleton);
      
      //You will probably not register this way but just for the sake of example.
      //Transient creates a new character whenever its resolved.
      builder.Register<Character>(Lifetime.Transient);
   }
}
```

## **Understanding VContainer's Quirks**

----------

VContainer and similar Unity DI frameworks come with nuances. Familiarity helps avoid pitfalls during service installation.

-   **No Optional Parameters:** Constructors in VContainer can't use optional parameters.

-   **Non-Service Parameters:** To inject non-service parameters (like strings or integers), use `WithParameter` during service registration.

    -   **Tip:** Simplify refactoring with initialization objects. Instead of multiple parameters, group them into an object and pass it with `WithParameter`.

-   **String IDs in Registration:** Unlike some tools (e.g., Zenject), VContainer doesn't use string IDs for service registration. This is problematic when a constructor needs multiple parameters of the same type.

    -   **Workarounds:**

        -   Utilize distinct types, such as `ISomePathService` or `IOtherPathService`.

        -   Pass parameters directly with `WithParameter`.

        -   Generally, steer clear of this pattern; it's too abstract.

-   **Multiple registration of the same type:** VContainer can resolve all the instances if you add `IEnumerable or IReadOnlyList` to your constructor. [Register Collection | VContainer](https://vcontainer.hadashikick.jp/registering/register-collection)


**Key Concepts to Remember:**

-   **Entry Points:** They decouple Unity's object lifecycle from Monobehaviors. [Plain C# Entry point | VContainer](https://vcontainer.hadashikick.jp/integrations/entrypoint)

-   **Registration:** To register Unity objects, typically use `RegisterInstance`.

    -   [Register Plain C# Type | VContainer](https://vcontainer.hadashikick.jp/registering/register-type)

    -   [Register using delegate | VContainer](https://vcontainer.hadashikick.jp/registering/register-using-delegate)

    -   [Registering Factories | VContainer](https://vcontainer.hadashikick.jp/registering/register-factory)

    -   Skip this: [Register MonoBehaviour | VContainer](https://vcontainer.hadashikick.jp/registering/register-monobehaviour)

-   **Resolving:** Understand how to fetch services.

    -   [Register MonoBehaviour | VContainer](https://vcontainer.hadashikick.jp/registering/register-monobehaviour)

-   **Scopes/Lifetime:** [Lifetime Overview | VContainer](https://vcontainer.hadashikick.jp/scoping/lifetime-overview).

    -   Mainly just understand the concept of enqueuing which is useful

        -   [Generate child scope via scene or prefab | VContainer](https://vcontainer.hadashikick.jp/scoping/generate-child-via-scene)

-   **Debugging:** Essential for identifying and addressing issues. [VContainer Diagnostics Window | VContainer](https://vcontainer.hadashikick.jp/diagnostics/diagnostics-window)


## **Service Manager Framework Breakdown**

----------

Our Service Manager Framework enhances VContainer's capabilities, integrating Roslyn analyzers for adaptability. Teams can utilize this framework from our [repository](https://github.com/geniesinc/UnityRoslynAnalyzers "https://github.com/geniesinc/UnityRoslynAnalyzers").

**Framework Goals:**

-   Simplify the intricacies inherent in DI frameworks.

-   Integrate Service Locator and DI, ensuring compatibility with both POCO and Monobehavior classes.

-   Despite the inherent Service Manager dependency, the aim is consistent dependency management across our packages, projects, and third-party integrations.

-   Exclude unproductive features unsuitable for Unity.

-   Facilitate an accessible learning curve.


**VContainer Modifications:**

-   Auto-injections limited strictly to constructors, thereby excluding Monobehaviors, fields, properties, or methods.

-   A Service Locator built atop VContainer simplifies service resolution using `ServiceManager.Get<T>`.

-   Direct interactions with services are streamlined via the ServiceManager class.

-   `LifetimeScope` and derivatives are restricted in the Editor to ensure consistency.

-   Workflow shifted from VContainer to ServiceManager.


**Key Features:**

-   **Grouped Service Management:** Organize services for systematic initialization. Useful when certain services (e.g., FeatureManager) need precedence.

-   **Auto Resolver:** Equipped for packages to self-resolve dependencies. Useful for initializing new projects or package defaults. Additionally, an editor window configures installer classes app-wide.


**Service Registration Varieties:**

-   **Service Locator Style:**

    -   Dynamic service registration.

    -   ID mapping allowed.

    -   Instance creation is user-controlled.

-   **DI Style:**

    -   Exclusively via `IGeniesInstaller`.

    -   Auto-injections to constructors using a container builder.

    -   No ID-based mapping, but instance creation is optional.


**Service Resolution Methods:**

-   **Service Locator Style:** Utilize `ServiceManager.Get<T>` for straightforward service retrieval. The search is structured: first, it seeks the nearest containers, then it defaults to an ordered lookup, and ultimately seeks the singleton container if necessary.

-   **DI Style:** Supervised by VContainer, available only within `IGeniesInstaller`, utilizing auto-injections.


**Framework's Core Components:**

-   **ServiceManager:** Central hub managing service registration, initialization, and resolution.

-   **IServiceContainer:** Recognized container offering parental functions, GameObject assignment for lifecycle management, and service resolution features.

-   **IGeniesInstaller:** Enables VContainer-style service installation, including group order setting.

-   **IGeniesInitializer:** Manages asynchronous service initialization and group and order settings.

-   **GeniesLifetimeScope:** Derived from VContainer's `LifetimeScope`, incorporating `IServiceContainer`, and supervised by ServiceManager.

-   **SingletonServiceContainer:** Designated for singleton service registrations, optionally with string IDs.

-   **AutoResolver:** Employs reflection for hands-free service installation and initialization. Ensures modular installs from various packages without tight installer or initializer links. To enlist a class for auto-resolved installation, tag it with the `AutoResolveAttribute` and ensure it inherits either `IGeniesInstaller` or `IGeniesInitializer`.


### Visualizing it

![servicemanager.png](Images%2Fservicemanager.png)

Link: [![](https://excalidraw.com/favicon-16x16.png)Excalidraw — Collaborative whiteboarding made easy](https://excalidraw.com/#json=moYJriRb3tmC33HS07khf,3LcB_c_M7IFW7zgFJPVhbQ)