using System;
using System.Diagnostics;

namespace PlayingWithDistributedLock
{
  public class ExceptionToleranceLockFactory : ILockFactory
  {
    private readonly ILockFactory _externalLockFactory;

    public ExceptionToleranceLockFactory(ILockFactory externalLockFactory)
    {
      _externalLockFactory = externalLockFactory;
    }

    public ILockObject AcquireLock(string key, TimeSpan expiration)
    {
      try
      {
        return _externalLockFactory.AcquireLock(key, expiration);
      }
      catch (Exception ex)
      {
        // Log.
        Console.WriteLine($"We had an error with message: '{ex.Message}'");

        return new DummyLockObject();
      }
    }

    public ILockObject AcquireLock(string key)
    {
      // For test purpose.
      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();

      ILockObject lockObject = AcquireLock(key, TimeSpan.FromSeconds(5));

      stopwatch.Stop();

      //Console.WriteLine($"Elapsed: {stopwatch.Elapsed}");

      return lockObject;
    }

    /// <summary>
    /// DummyLockObject
    /// </summary>
    internal class DummyLockObject : ILockObject
    {
      public bool IsAcquired => true;

      public void Dispose()
      {
        Console.WriteLine("Release the DummyLockObject.");
      }
    }
  }
}
