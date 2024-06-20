using Polly;

namespace PlayingWithDistributedLock;

public static class Program
{
    private const string _lockKey = "lock:eat";

    // Note: Read the description in the ExceptionToleranceLockFactory class.
    private static readonly ILockFactory _lockFactory = new ExceptionToleranceLockFactory(new RedisDistLockFactory());

    private static readonly AsyncPolicy<ILockObject> _waitAndRetryPolicy = Policy
        .HandleResult<ILockObject>(lo => lo.IsAcquired == false) // When we did not get a lock.
        .WaitAndRetryAsync(4, // 1 + 4 times retry.
            _ => TimeSpan.FromMilliseconds(Random.Shared.Next(1200, 1500)),
            (res, ts, ctx) => Console.WriteLine($"{ctx["person"]} is waiting {ts.TotalMilliseconds:N0} ms for retry."));


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

        // --> #4 Dinner is ready to eat.
        await Parallel.ForEachAsync(Enumerable.Range(1, 4), (personId, ct) => personEat(personId));

        Console.WriteLine("End of the dinner.");

        Console.WriteLine("--- Start with RedLock ---");

        using var redLockEating = new RedLockEating();

        await redLockEating.Start();
    }

    private static async ValueTask personEat(int personId)
    {
        string person = $"Person({personId})";

        // Try to acquire a lock maximum 5 times.
        ILockObject lockObject = await _waitAndRetryPolicy
            .ExecuteAsync(
                ctx => _lockFactory.AcquireLockAsync(_lockKey, TimeSpan.FromSeconds(5)),
                new Dictionary<string, object> { ["person"] = person });

        if (!lockObject.IsAcquired)
        {
            Console.WriteLine($"{person} did not get any food.");
            return;
        }

        // We got a lock
        Console.WriteLine($"{person} begin to eat.");

        await Task.Delay(1_000);

        // Try to release the lock.
        if (Random.Shared.NextDouble() < 0.8)
        {
            await lockObject.ReleaseAsync();

            Console.WriteLine($"{person} released the lock.");
        }
        else Console.WriteLine($"{person} did not release lock.");
    }
}
