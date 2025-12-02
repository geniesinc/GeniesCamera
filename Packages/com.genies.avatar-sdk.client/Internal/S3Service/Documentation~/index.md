S3 Upload Service
============

Usage
------------

* Upload binaries (Textures, Models, Audio, etc...) to a user's bucket in S3
* Handles caching on disk and provides access to

How it works
------------

* Initialization
    * Initialize a new instance of `IGeniesS3Service`
    * Provide the `IUserAPI` and `IImageAPI` instances in the parameters
    * Provide the `DiskCacheOptions`

```csharp

void InitializationLogic()
{
    IGeniesS3Service s3Service = new GeniesS3Service(new ImageApi(), new UserApi(), DiskCacheOptions.Default);
}

```

* Uploading

```csharp
public void UploadSampleTexture(Texture2D sampleTexture)
{
    var customId = "sample";
    var bytes = sampleTexture.EncodeToJPG();
    var s3FilePath = $"{customId}.jpg"; //Will upload to root of s3 folder with this name
    var distributionUrl = await s3Service.UploadObject(s3FilePath, bytes);
    SaveUrl(distributionUrl);
}
```

* Downloading

```csharp
public Texture2D DownloadSampleTexture(string distributionUrl)
{
    var localFilePath = Path.Combie(Application.persistentDataPath, "sample.jpg");
    var response      = await s3Service.DownloadObject(distributionUrl, localFilePath);

    if (!response.wasDownloaded)
    {
        return null;
    }
    
    try
    {
        if (File.Exists(response.downloadedFilePath))
        {
            var texture = ImageLoader.LoadTexture(response.downloadedFilePath);
            return texture;
        }
    }
    catch (Exception ex)
    {
        throw ex;
    }

    return null;
}
```