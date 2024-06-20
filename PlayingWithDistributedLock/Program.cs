using PlayingWithDistributedLock.EatingExemple;
using PlayingWithDistributedLock.Implementation;

namespace PlayingWithDistributedLock;

public static class Program
{
    private const string _lockKey = "lock:eat";

    private static readonly ILockFactory _lockFactory = new ExceptionToleranceLockFactory(new RedisDistLockFactory());

    public static async Task Main(string[] args)
    {
        // --> #1 AcquireLock with retry.
        using (ILockObject outerLockObject = _lockFactory.AcquireLock(_lockKey, TimeSpan.FromSeconds(3)))
        using (ILockObject innerLockObject = _lockFactory.AcquireLock(_lockKey, TimeSpan.FromSeconds(5), 2, TimeSpan.FromSeconds(2)))
            Console.WriteLine($"Did I get a lock? -> {innerLockObject.IsAcquired}");

        // --> #2 AcquireLockAsync with retry.
        using (ILockObject outerLockObject = await _lockFactory.AcquireLockAsync(_lockKey, TimeSpan.FromSeconds(3)))
        using (ILockObject innerLockObject = await _lockFactory.AcquireLockAsync(_lockKey, TimeSpan.FromSeconds(5), 2, TimeSpan.FromSeconds(2)))
            Console.WriteLine($"Did I get a lock? -> {innerLockObject.IsAcquired}");

        // --> #3 AcquireLockAsync with retry + timeout.
        try
        {
            using (CancellationTokenSource ctSource = new CancellationTokenSource(TimeSpan.FromSeconds(3.5)))
            {
                using (ILockObject outerLockObject = await _lockFactory.AcquireLockAsync(_lockKey, TimeSpan.FromSeconds(3)))
                using (ILockObject innerLockObject = await _lockFactory.AcquireLockAsync(_lockKey, TimeSpan.FromSeconds(5), 2, TimeSpan.FromSeconds(2), ctSource.Token))
                    Console.WriteLine($"Did I get a lock? -> {innerLockObject.IsAcquired}");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("I expected for an OperationCanceledException.");
        }

        // --> #4 Dinner is ready to eat
        Console.WriteLine("--- Start eating ---");
        await EatingWithRedisDistLock.Create(_lockFactory).Start();

        // --> #5 Dinner is ready to eat
        Console.WriteLine("--- Start eating with RedLock ---");

        using var redLockEating = EatingWithRedLock.Create();

        await redLockEating.Start();
    }
}
