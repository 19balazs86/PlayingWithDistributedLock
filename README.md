# Playing with distributed locks using Redis

This small .Net Core application is an example to acquire locks in a distributed system.
Nowadays, you can easily scale your (microservcies) system and you can have shared resources.

The good old solution, using the lock statement is not appropriate to manage the accessibility between distributed applications.

#### Resources

- C# Corner: [Creating Distributed Lock With Redis In .NET Core](https://www.c-sharpcorner.com/article/creating-distributed-lock-with-redis-in-net-core "Creating Distributed Lock With Redis In .NET Core").
- Other .NET solutions on the [official page](https://redis.io/topics/distlock "official page"). 
- [Observer design pattern](https://docs.microsoft.com/en-us/dotnet/standard/events/observer-design-pattern "Observer design pattern") unsubscribe mechanism to release the lock.
- I used the [Polly library](https://github.com/App-vNext/Polly "Polly library") to wait and retry to acquire a lock.

>  [Distributed caching in ASP.NET Core](https://docs.microsoft.com/en-ie/aspnet/core/performance/caching/distributed?view=aspnetcore-2.2 "Distributed caching in ASP.NET Core") is another useful tool. [EasyCaching Provider.](https://www.c-sharpcorner.com/article/using-easycaching-to-handle-multiple-instance-of-caching-providers "EasyCaching Provider.")
>  You can find some inline comments in the code.

#### Setup a redis server on Windows.

1. Download the redis server (zip version) from [MicrosoftArchive/redis/releases](https://github.com/MicrosoftArchive/redis/releases "MicrosoftArchive/redis/releases")
2. Run the server: redis-server.exe
3. Run the client (optional): redis-cli.exe | [Redis commands](https://redis.io/commands "Redis commands")

[Chocolatey Galery](https://chocolatey.org/packages/redis-64 "Chocolatey Galery") another easy and fast way to get a redis.

#### This flowchart is represent the steps.

![Flowchart](Flowchart.JPG)
