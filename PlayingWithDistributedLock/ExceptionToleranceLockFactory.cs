﻿using System;
using System.Diagnostics;

namespace PlayingWithDistributedLock
{
  /// <summary>
  /// This class is meant to be a wrapper around the RedisDistLockFactory.
  /// Use it carefully, wisely, because it can make unwanted result!
  /// If you have a problem with the Redis server. this guy gives you a dummy lock.
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

      public bool Release()
      {
        Console.WriteLine("Release the DummyLockObject.");

        return true;
      }

      public void Dispose() => Release();
    }
  }
}
