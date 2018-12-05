# Playing with distributed locks using Redis

This small .Net Core application is an example to acquire locks in a distributed system.
Nowadays, you can easily scale your (microservcies) system and you can have shared resources.

The good old solution, using the lock statement is not appropriate to manage the accessibility between distributed applications.

The idea came from a [C# Corner article](https://www.c-sharpcorner.com/article/creating-distributed-lock-with-redis-in-net-core/ "C# Corner article").
You can find other .NET solutions on the [official page](https://redis.io/topics/distlock "official page"). 

In this example, I used the [observer design pattern](https://docs.microsoft.com/en-us/dotnet/standard/events/observer-design-pattern "observer design pattern") unsubscribe mechanism to release the lock.

I used [the Polly library](https://github.com/App-vNext/Polly "the Polly library") to wait and retry to acquire a lock.

Some inline comments in the code.

##### Setup a redis server locally on Windows.
Note: If you do not have docker, this is an easy and fast way to get a redis. No need any installation process.
1. Download the redis server (zip version) from [MicrosoftArchive/redis/releases](https://github.com/MicrosoftArchive/redis/releases "MicrosoftArchive/redis/releases")
2. Run the server: redis-server.exe
3. Run the client (optional): redis-cli.exe | [Redis commands](https://redis.io/commands "Redis commands")

##### This flowchart is meant to represent the steps.

![Flowchart](Flowchart.JPG)