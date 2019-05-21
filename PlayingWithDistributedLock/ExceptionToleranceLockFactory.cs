using System;
using System.Diagnostics;

namespace PlayingWithDistributedLock
{
  /// <summary>
  /// This class is meant to be a wrapper around the RedisDistLockFactory.
  /// Use it carefully, wisely, because it can make unwanted results!
  /// If you have a problem with the Redis server, this class gives you a dummy lock.
  /// Your business logic can continue to work, thinks that you have a valid lock.
  /// </summary>
  public class ExceptionToleranceLockFactory : ILockFactory
  {
    private readonly ILockFactory _externalLockFactory;

    public ExceptionToleranceLockFactory(ILockFactory externalLockFactory)
    {
      _externalLockFactory = externalLockFactory;
    }

    public ILockObject AcquireLock(string key, TimeSpan expiration)
      => AcquireLock(key, expiration, 0, TimeSpan.Zero);

    public ILockObject AcquireLock(string key, TimeSpan expiration, int retryCount, TimeSpan sleepDuration)
    {
      // For test purpose.
      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();

      try
      {
        return _externalLockFactory.AcquireLock(key, expiration, retryCount, sleepDuration);
      }
      catch (LockFactoryException ex)
      {
        // Log.
        Console.WriteLine($"External LockFactory error: '{ex.Message}'");

        return new DummyLockObject();
      }
      finally
      {
        stopwatch.Stop();

        //Console.WriteLine($"Elapsed: {stopwatch.Elapsed}");
      }
    }

    /// <summary>
    /// DummyLockObject
    /// </summary>
    internal class DummyLockObject : ILockObject
    {
      public bool IsAcquired => true;

      public bool Release()
      {
        Console.WriteLine("Release the DummyLockObject.");

        return true;
      }

      public void Dispose() => Release();
    }
  }
}
