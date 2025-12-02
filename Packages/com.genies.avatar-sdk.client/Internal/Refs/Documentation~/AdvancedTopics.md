# Advanced Topics

There are some other utilities appart from the `Ref` struct and the `CreateRef` class that you can use from this package. Like the concept of resource handles and how you can create your own resource implementations for any use case.

## Table of Contents
* [The `Handle` struct](#the-handle-struct)
  * [Code example](#code-example)
* [The `IResource` interface](#the-iresource-interface)
* [Using the CreateHandle static class to create handles](#using-the-createhandle-static-class-to-create-handles)

# The `Handle` struct

Internally references use a resource handle to fetch the resource and track the reference count. It is the handle who will trigger the resource disposal once the reference count reaches zero. There are use cases where you could need to work with handles directly, and then create references from them to share.

For this, the package offers the `Handle` and `Handle<T>` structs. Here is a simplified version of the code so you can see the public properties and methods that they contain:

```C#
public readonly struct Handle : IDisposable
{
    public object Resource { get; }
    public int ReferenceCount { get; set; }
    public bool IsAlive { get; }

    public void Dispose();
    public bool TryCast<T>(out Handle<T> handle)
}

public readonly struct Handle<T> : IDisposable
{
    public T Resource { get; }
    public int ReferenceCount { get; set; }
    public bool IsAlive { get; }

    public void Dispose();

    // you can always cast a Handle<T> to a non generic Handle
    public static implicit operator Handle(Handle<T> handle);
}
```

You can manually set the reference count and the handle will automatically dispose the resource once it reaches zero or below. You can also dispose the handle directly, which will dispose the resource.

## Code example

Let's improve the `SpriteDownloaderService` implementation from the [Getting Started Code example](index.md#code-example).

You may have noticed that any time we download a sprite from a specific URL we will always download the bytes and create a new texture and sprite instances for it, even if we request the same URL multiple times.

There are multiple ways to improve this. We could cache our own reference by the URL and return a new one any time the URL is requested again. The problem with this approach is that the service will now keep the downloaded sprite in memory indefinitely, since it is keeping its own reference and will never dispose it.

To avoid the service keeping the assets in memory we can cache handles instead of references. Since handles will be automatically disposed when all references to it are disposed, we can check if the asset is still in memory when a URL is requested again. If it is the case we can create a new reference to the handle and return it. If the asset was already released, we can perform the download process again.

For simplicity, we are going to rename the service to `ImageDownloaderService` and make it to only return textures instead of sprites.

```C#
public static class ImageDownloaderService
{
    private static readonly Dictionary<string, Handle<Texture2D>> _cache = new Dictionary<string, Handle<Texture2D>>();
    
    public static Ref<Texture2D> DownloadImage(string url)
    {
        // we check the cache first
        if (_cache.TryGetValue(url, out Handle<Texture2D> handle))
        {
            // if the texture was previously downloaded and it is still in memory, create a new reference to it
            if (handle.IsAlive)
                return CreateRef.FromHandle(handle);
            
            // the handle is not alive which means that previous callers already disposed all the references
            // lets remove it from the cache and perform the download process again
            _cache.Remove(url);
        }
        
        // download the texture from imaginary service
        byte[] imageBytes = DownloaderService.Download(url);
        Texture2D texture = new Texture2D(256, 256);
        texture.LoadImage(imageBytes);

        // create a handle for the texture and cache it with the URL
        handle = CreateHandle.FromUnityObject(texture);
        _cache[url] = handle;
        
        // create a new reference to the handle and return it
        Ref<Texture2D> textureRef = CreateRef.FromHandle(handle);
        return textureRef;
    }
}
```

# The `IResource` interface

If you have a specific resource where `CreateRef` or `CreateHandle` methods won't work, you can implement the `IResource` interface to create references or handles for any use case.

```C#
public interface IResource<out TResource> : IDisposable
{
    TResource Resource { get; }
    bool IsDisposed { get; }
}
```

A resource implementation provides three things:
* Access to the resource.
* Information about the resource's disposal state.
* How to dispose the resource.

Let's create our own resource implementation for Unity materials:

```C#
public class MaterialResource : IResource<Material>
{
    public Material Resource { get; private set; }
    
    // Use UnityEngine.Object implicit bool operator to check if the material is destroyed
    public bool IsDisposed => !Resource;

    public MaterialResource(Material material)
    {
        Resource = material;
    }
    
    public void Dispose()
    {
        if (!IsDisposed)
            Object.Destroy(Resource);
        
        Resource = null;
    }
}
```

Now you can use this implementation to create references or handles to any material instance.

```C#
Material material = ...;
MaterialResource resource = new MaterialResource(material);

// create a reference
Ref<Material> materialRef = CreateRef.From(resource);

// or create a handle and a reference from it
Handle<Material> materialHandle = CreateHandle.From(resource);
Ref<Material> materialRef = CreateRef.FromHandle(materialHandle);
```

Whenever all references to the material are disposed, the material will be automatically destroyed.

**Note that instantiating the MaterialResource creates a heap allocation, you can implement your own pooling for your `IResource` implementations if you want to optimize for this. The package's internal IResource implementations all use pooling.**

# Using the CreateHandle static class to create handles

The `CreateHandle` static class is the only way to create handles. It mirrors the same factory methods from the [CreateRef](index.md/#using-the-createref-static-class-to-create-references) class except for the `CreateRef.FromHandle()` method.
