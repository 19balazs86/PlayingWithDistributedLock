namespace PlayingWithDistributedLock;

/// <summary>
/// This class is meant to be a wrapper around the RedisDistLockFactory.
/// Use it carefully, wisely, because it can make unwanted results!
/// If you have a problem with the Redis server, this class gives you a dummy lock.
/// Your business logic can continue to work, thinks that you have a valid lock.
/// </summary>
public sealed class ExceptionToleranceLockFactory : ILockFactory
{
    private readonly ILockFactory _externalLockFactory;

    public ExceptionToleranceLockFactory(ILockFactory externalLockFactory)
    {
        _externalLockFactory = externalLockFactory;
    }

    public ILockObject AcquireLock(string key, TimeSpan expiration, int retryCount = 0, TimeSpan sleepDuration = default)
    {
        // For test purpose
        //long startingTimestamp = Stopwatch.GetTimestamp();

        try
        {
            return _externalLockFactory.AcquireLock(key, expiration, retryCount, sleepDuration);
        }
        catch (LockFactoryException ex)
        {
            Console.WriteLine($"External LockFactory error: '{ex.Message}'");

            return new DummyLockObject();
        }
        finally
        {
            //Console.WriteLine($"Elapsed: {Stopwatch.GetElapsedTime(startingTimestamp)}");
        }
    }

    public async Task<ILockObject> AcquireLockAsync(
        string key,
        TimeSpan expiration,
        int retryCount                = 0,
        TimeSpan sleepDuration        = default,
        CancellationToken cancelToken = default)
    {
        // For test purpose
        //long startingTimestamp = Stopwatch.GetTimestamp();

        try
        {
            return await _externalLockFactory.AcquireLockAsync(key, expiration, retryCount, sleepDuration, cancelToken);
        }
        catch (LockFactoryException ex)
        {
            Console.WriteLine($"External LockFactory error: '{ex.Message}'");

            return new DummyLockObject();
        }
        finally
        {
            //Console.WriteLine($"Elapsed: {Stopwatch.GetElapsedTime(startingTimestamp)}");
        }
    }

    /// <summary>
    /// DummyLockObject
    /// </summary>
    internal sealed class DummyLockObject : ILockObject
    {
        public bool IsAcquired => true;

        public bool Release()
        {
            Console.WriteLine("Release the DummyLockObject.");

            return true;
        }

        public Task<bool> ReleaseAsync() => Task.FromResult(Release());

        public void Dispose() => Release();
    }
}
