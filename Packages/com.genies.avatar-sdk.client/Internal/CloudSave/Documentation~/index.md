# Cloud Save

This package provides an out of the box solution for storing Json data to our servers.

## Limitations

- Both client and server need to share a Feature Type, if the feature type isn't defined, the calls will fail. This is done to ensure that we are only writing the correct data to our servers.
- The current API implementation only exposes a GET endpoint that will fetch every record, it will not fetch it per feature type.


## Usage

- The implementer will need to provide the implementation for `ICloudJsonSerializer`, the server doesn't not have a schema so the client will handle both serialization and data integrity.
- Ex:

```csharp

public void InitializeStyleSaveService()
{
    IGeniesLogin accountService = new AccountService();
    ICloudSaveJsonSerializer styleDataSerializer = new StyleDataJsonSerializer();
    ICloudFeatureSaveService<StyleData> styleSaveService = new CloudFeatureSaveService(GameFeatureTypeEnum.Styles,
                                                                                       accountService,
                                                                                       styleDataSerializer);
}

```

- Based on the above example, you can now use the defined save service to create, update, retrieve and delete new style save data.
