# Playing with distributed locks using Redis

This small .NET application is an example of acquiring locks in a distributed environment.

> I prefer using [Azure Service Bus with the SessionQueue feature](https://github.com/19balazs86/AzureServiceBus) to avoid race conditions and handle messages for the same resource.

#### Resources

- [Distributed locks with Redis](https://redis.io/topics/distlock) 📓*Official Redis page* 
- Using the [Observer design pattern](https://docs.microsoft.com/en-us/dotnet/standard/events/observer-design-pattern) for to release the lock
- Using the [Polly](https://www.pollydocs.org) to wait and retry to acquire a lock
- [Improve memory allocation](https://hashnode.devindran.com/how-to-improve-memory-allocation-when-using-stackexchangeredis) 📓*Devindran Ramadass - RecyclableMemoryStreamManager and ArrayPoolBufferWriter*

#### Clients

- [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) 👤*Stack Overflow - Most popular and stable client. Interface for [IDatabase](https://github.com/StackExchange/StackExchange.Redis/blob/master/src/StackExchange.Redis/Interfaces/IDatabase.cs)*
- [RedLock.net](https://github.com/samcook/RedLock.net) 👤*Sam Cook - An implementation of a distributed lock algorithm*

#### Code snippets
```csharp
public interface ILockFactory
{
    ILockObject AcquireLock(string key, TimeSpan expiration, int retryCount = 0, TimeSpan sleepDuration = default);
    
    Task<ILockObject> AcquireLockAsync(string key, TimeSpan expiration, int retryCount = 0, TimeSpan sleepDuration = default, CancellationToken cancelToken = default);
}
```

```csharp
public interface ILockObject : IDisposable
{
    // Did I get a lock or not?
    bool IsAcquired { get; }

    // Release the lock, if it still exists and returns true, otherwise false.
    bool Release();

    Task<bool> ReleaseAsync();
}
```

#### Setup: Redis server on Windows

1. Download the redis server
   1. [Redis-windows](https://github.com/zkteco-home/redis-windows) 👤*zkteco-home*
   2. [MicrosoftArchive/redis/releases](https://github.com/MicrosoftArchive/redis/releases) 👤*Microsoft Archive*
   3. Install it from: [Chocolatey Galery](https://community.chocolatey.org/packages/redis)
2. Run the server: redis-server.exe *| Connection string: "127.0.0.1:6379"*
3. Run the client (optional): redis-cli.exe | [Redis commands](https://redis.io/commands)

#### This flowchart is represent the steps

![Flowchart](Flowchart.JPG)
