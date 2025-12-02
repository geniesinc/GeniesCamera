# Refs

The Refs package provides a pattern for managing disposable resources in a safe way. Loaded resources can be shared across the app and released from memory when they are no longer used.

In this document we will review the most basic and important concepts of the package.

## Table of Contents
* [The `Ref` struct](#the-ref-struct)
  * [Disposing references](#disposing-references)
  * [Ownership of references](#ownership-of-references)
  * [Sharing references](#sharing-references)
* [Using the CreateRef static class to create references](#using-the-createref-static-class-to-create-references)
  * [From](#from)
  * [FromAny without disposal logic](#fromany-without-disposal-logic)
  * [FromAny with disposal logic](#fromany-with-disposal-logic)
  * [FromUnityObject](#fromunityobject)
  * [FromUnityResource](#fromunityresource)
  * [FromAddressable](#fromaddressable)
  * [FromDisposable](#fromdisposable)
  * [FromDependentResource with a non-disposable resource](#fromdependentresource-with-a-non-disposable-resource)
  * [FromDependentResource with a disposable resource](#fromdependentresource-with-a-disposable-resource)
  * [Code example](#code-example)

# The `Ref` struct

The `Ref` and `Ref<T>` structs are at the core of this package. They represent a **unique disposable reference** to a resource. Here is a simplified version of the code so you can see the public properties and methods that they contain:
```C#
public readonly struct Ref : IDisposable
{
    public object Item { get; }
    public bool IsAlive { get; }

    public Ref New();
    public void Dispose();
    public bool TryCast<T>(out Ref<T> reference)
}

public readonly struct Ref<T> : IDisposable
{
    public T Item { get; }
    public bool IsAlive { get; }

    public Ref<T> New();
    public void Dispose();

    // you can always cast a Ref<T> to a non generic Ref
    public static implicit operator Ref(Ref<T> reference);
}
```

Using resources through references has the following advantages:
* You don't care about who loaded the resource or how it was loaded.
* You don't care about how or where to dispose the resource.
* You don't care about side effects of disposing the resource. When you dispose your reference you will not cause the resource to be released from memory if other systems are still using the same resource, because the other systems will have their own references to it.

Also, you can always cast a generic instance `Ref<T>` down to the non-generic version `Ref`. This is very useful for some usecases like the implementation of reference caches.

## Disposing references

Disposing a reference doesn't mean disposing the resource that it references. The resource will be disposed as soon as all references to it are disposed. This means that maintaining just one reference alive will prevent the resource from being released.

You can only dispose the same reference once. Please keep in mind that even though `Ref` and `Ref<T>` are structs, sharing them through method parameters or assigning them to other variables will not create a new unique reference, which means that disposing any of them will automatically dispose the other ones.

## Ownership of references

Understanding how you own references is the most important part to keep your resources clean and safe.

Owning a reference implies two things:
* **You are responsible of disposing it**: Everytime you get a reference you should be disposing it at some point in your code, otherwise you are doing something wrong. This also implies that whenever you get a reference, you should not lose it without disposing it first (i.e.: fetching the resource it references with the Item property and forgeting about the `Ref` object).
* **You must never expose your owned reference publicly**: Sharing references is allowed, but you should never allow any external code to access a reference that you own, doing so would allow anyone to dispose your reference when you are still using the resource. A `Ref` or `Ref<T>` field should never be public unless you know what you are doing (i.e.: making it public in a private class used on an encapsulated design).

## Sharing references

What if you want to allow external code to use your resources ? You can use the `New()` method for that. The `Ref.New()` method creates a brand new reference to the same resource that can be disposed separately.

If you want to expose your internal resources through a public property/method that returns a `Ref`, you **must always** return a new reference that the caller will own. Since the caller owns the new reference, he can safely dispose it at any time without causing the resources to be released while you are still using them.

In the same way that you must never give your owned references to external code, you should assume that any external code that returns a reference will allways return one that you can safely own. This means that you should never use the `New()` method to create and own a reference when getting one through a returned value or from one of your public method parameters.

### Sharing references example

```C#
public class SomeSystem
{
    // internally owned resource
    private Ref<Texture> _texture;

    public SomeSystem()
    {
        /*
            Getting a ref from return value
        */

        /* WRONG: SomeAssetsService returned a reference for you to own.
        You would be leaving an unowned reference behind that will never be disposed. */
        _texture = SomeAssetsService.Load("some_texture_address").New();

        /* OK */
        _texture = SomeAssetsService.Load("some_texture_address");


        /*
            Sharing a ref through public method parameter
        */

        /* WRONG: Now SomeOtherSystem can dispose your reference while you are still using the resource. */
        SomeOtherSystem.SetTexture(_texture);

        /* OK */
        SomeOtherSystem.SetTexture(_texture.New())
    }

    /*
        Sharing a ref through return value
    */
    public Ref<Texture> GetTexture()
    {
        /* WRONG: Whoever got the reference can dispose it while you are still using the resource. */
        return _texture;

        /* OK */
        return _texture.New();
    }
}

public class SomeOtherSystem
{
    // internally owned resource
    private static Ref<Texture> _texture;

    /*
        Getting a ref from public method parameter
    */
    public static void SetTexture(Ref<Texture> texture)
    {
        /* WRONG: whoever called this method is providing a reference for you to own.
        You would be leaving an unowned reference behind that will never be disposed. */
        _texture = texture.New();

        /* OK */
        _texture = texture;
    }
}
```

# Using the CreateRef static class to create references

This package uses some pooling techniques to optimize heap allocations when creating references. That is why the only way to create references is through the `CreateRef` static class, which serves as a factory (it is thread-safe).

Here are the factory methods available to create references:

## From

```C#
From<T>(IResource<T> resource)
```
Creates a reference from any implementation of `IResource`. Check [The `IResource` interface](AdvancedTopics.md#the-iresource-interface) to learn more.

## FromHandle

```C#
FromHandle<T>(Handle<T> handle)
```
Creates a reference from a resource handle. Check [The `Handle` struct](AdvancedTopics.md#the-handle-struct) to learn more.

## FromAny without disposal logic

```C#
FromAny<T>(T asset)
```
Creates a dummy reference to any type of resource that will do nothing to dispose it. Useful if you have a non-disposable resource but you need to share it with code that only accepts references.

## FromAny with disposal logic

```C#
FromAny<T>(T asset, Action<T> disposeCallback)
```
Creates a reference to any type of resource where you can provide a callback containing the disposal logic.

## FromUnityObject

```C#
FromUnityObject<T>(T asset) where T : UnityEngine.Object
```
Creates a reference to any Unity Object. The object will be disposed using `UnityEngine.Object.Destroy()`.

This is a good method to use when creating assets at runtime like textures, materials, meshes, etc... Check the [Code example](#code-example)

## FromUnityResource

```C#
FromUnityResource<T>(T asset) where T : UnityEngine.Object
```
Creates a reference to any Unity Object that was loaded using the Resources API. The object will be disposed using `Resources.UnloadAsset();`. It is recommended that you use the `ResourcesUtility` static class if you want to load from the Resources API while using references.

## FromAddressable

```C#
FromAddressable<T>(AsyncOperationHandle<T> operationHandle)
```
Creates a reference to a resource loaded from the Addressables API. The given handle must have been awaited and succeeded before using it here.

It is important to note that you cannot use this method with handles that are not comming from Addressables, since the underlying implementation will use the `Addressables.Release()` when disposing the resource.

## FromDisposable

```C#
FromDisposable<T>(T disposable) where T : IDisposable
```
Creates a reference to any type of resource implementing the `System.IDisopsable` interface. The `IDisposable.Dispose()` method will be invoked for the resource disposal.

## FromDependentResource with a non-disposable resource

```C#
FromDependentResource<T>(T asset, params Ref[] dependencies)
FromDependentResource<T>(T asset, IEnumerable<Ref> dependencies)

// use this overloads if you don't want to cast the dependencies to the non-generic Ref struct by yourself
FromDependentResource<T, TOther>(T asset, params Ref<TOther>[] dependencies)
FromDependentResource<T, TOther>(T asset, IEnumerable<Ref<TOther>> dependencies)
```
Creates a reference to any type of resource that has a dependency on other resources. This dependencies are provided as a collection of references that will be owned by the created reference. Once all references are disposed, all the dependency references will be disposed too.

This overload assumes that the given resource doesn't need to be disposed. If you need to create a reference with dependencies and a resource that must be disposed, you can use [FromAny with disposal logic](#fromany-with-disposal-logic) to create a reference to the resource, and then you can use [FromDependentResource with a disposable resource](#fromdependentresource-with-a-disposable-resource) to create a reference that includes the dependencies.

## FromDependentResource with a disposable resource

```C#
FromDependentResource<T>(Ref<T> reference, params Ref[] dependencies)
FromDependentResource<T>(Ref<T> reference, IEnumerable<Ref> dependencies)

// use this overloads if you don't want to cast the dependencies to the non-generic Ref struct by yourself
FromDependentResource<T, TOther>(Ref<T> reference, params Ref<TOther>[] dependencies)
FromDependentResource<T, TOther>(Ref<T> reference, IEnumerable<Ref<TOther>> dependencies)
```
Same as [FromDependentResource with a non-disposable resource](#fromdependentresource-with-a-non-disposable-resource) but accepts a reference to the main resource instead of the resource directly.

## Code example

Let's put an example to understand how to use the `CreateRef` methods.

Imagine that we have a service that loads some resource that it is actually built at runtime from other resources. A good example for this would be creating a new Sprite from an image downloaded from a URL.

Now we want this service to use this package to return a `Ref<Sprite>` reference on the load method. So this resource can be shared across the code and properly released when no longer used.

We only want to return a reference to the sprite object, but we will also need to create a texture first, so we can create a sprite from it. This is a good case for using [FromUnityObject](#fromunityobject) and [FromDependentResource with a disposable resource](#fromdependentresource-with-a-disposable-resource).

```C#
public static class SpriteDownloaderService
{
    public static Ref<Sprite> DownloadSprite(string url)
    {
        // download the texture from imaginary service
        byte[] imageBytes = DownloaderService.Download(url);
        Texture2D texture = new Texture2D(256, 256);
        texture.LoadImage(imageBytes);
    
        // create a reference to the new texture
        Ref<Texture2D> textureRef = CreateRef.FromUnityObject(texture);
    
        // create a sprite from the texture and a reference to it
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        Ref<Sprite> spriteRef = CreateRef.FromUnityObject(sprite);

        // create a new ref for the sprite that includes its dependency with the texture. Disposing it will dispose both references
        Ref<Sprite> result = CreateRef.FromDependentResource(spriteRef, textureRef);
        return result;
    }
}
```

You can now create more refs to the created sprite using the `Ref.New()` method to share it across the code. Once all references are disposed, both the Sprite and the Texture2D instances will be destroyed.

---

If you want to learn more about the package go to the [Advanced Topics](AdvancedTopics.md) document.