using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using System.Net;

namespace PlayingWithDistributedLock.EatingExemple;

public sealed class EatingWithRedLock : IDisposable
{
    private readonly TimeSpan _expiryTime = TimeSpan.FromSeconds(2);
    private readonly TimeSpan _waitTime = TimeSpan.FromSeconds(15);
    private readonly TimeSpan _retryTime = TimeSpan.FromSeconds(1);

    private const string _lockKey = "RedLockEating";

    private readonly RedLockFactory _redLockFactory;

    public EatingWithRedLock()
    {
        var redisEndpoints = new List<RedLockEndPoint> { new DnsEndPoint("127.0.0.1", 6379) };

        _redLockFactory = RedLockFactory.Create(redisEndpoints);
    }

    public static EatingWithRedLock Create()
    {
        return new EatingWithRedLock();
    }

    public async Task Start()
    {
        await Parallel.ForEachAsync(Enumerable.Range(1, 9), (personId, ct) => personEat(personId));
    }

    private async ValueTask personEat(int personId)
    {
        string person = $"Person({personId})";

        using IRedLock lockObject = await _redLockFactory.CreateLockAsync(_lockKey, _expiryTime, _waitTime, _retryTime);

        if (!lockObject.IsAcquired)
        {
            Console.WriteLine($"{person} did not get any food.");

            return;
        }

        // Note: _expiryTime is 2 seconds. It will be extended automatically if needed.
        int millisecondsDelay = Random.Shared.Next(1_000, 3_000);

        Console.WriteLine($"{person} begin to eat and wait {millisecondsDelay:N0} ms.");

        await Task.Delay(millisecondsDelay);

        Console.WriteLine($"{person} is done.");
    }

    public void Dispose() => _redLockFactory.Dispose();
}
