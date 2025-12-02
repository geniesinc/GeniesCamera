# Data Repository Framework

This package provides a simple data access pattern framework to execute CRUD operations. The package provides out of the box solution to create your own data repositories which can be used to separate data sources from business logic. 

Out of the box, the package provides a type of repository that handles caching the operations in memory to avoid redundant API calls. Depending on your use case and if the data can be modified remotely, you might or might not want to use that.

The package also provides exception handling.
## Usage

- The user will need to implement their own `IDataRepository<T>` which will act as the data source for retrieving data (example: a cloud service or a CRUD api)
- The user can then use the decorated `MemoryCachedDataRepository<T>` to get an out of the box solution for caching and manipulating data in memory.
- Ex:

```csharp

public class UserApiDataRepository : IDataRepository<User>
{
    //Implement
}

public void InitializeUserDataRepository()
{
    IDataRepository<User> apiRepository = new UserApiDataRepository();
    IDataRepository<User> memoryCached = new MemoryCachedDataRepository(apiRepository);
    ... 
    
    //Use the memory cached repository to communicate CRUD calls with the API but also store data in memory.
}

```
