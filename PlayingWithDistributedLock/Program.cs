﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace PlayingWithDistributedLock
{
  public class Program
  {
    private static readonly Random _random = new Random();

    private const string _lockKey = "lock:eat";

    // Note: Read the description in the ExceptionToleranceLockFactory class.
    private static readonly ILockFactory _lockFactory =
      new ExceptionToleranceLockFactory(new RedisDistLockFactory());

    private static readonly Policy<ILockObject> _waitAndRetryPolicy = Policy
      .HandleResult<ILockObject>(lo => lo.IsAcquired == false) // When we did not get a lock.
      .WaitAndRetry(4, // 1 + 4 times retry.
        _ => TimeSpan.FromMilliseconds(_random.Next(1200, 1500)),
        (res, ts, ctx) => Console.WriteLine($"{ctx["person"]} is waiting for retry - {ts.TotalMilliseconds} ms."));

    /// <summary>
    /// Main
    /// </summary>
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
      Parallel.For(0, 5, x => personEat(x));

      Console.WriteLine("End of the dinner.");
    }

    private static void personEat(int personId)
    {
      string person = $"Person({personId})";
      
      // Try to acquire a lock maximum 5 times.
      ILockObject lockObject = _waitAndRetryPolicy
        .Execute(
          ctx => _lockFactory.AcquireLock(_lockKey, TimeSpan.FromSeconds(5)),
          new Dictionary<string, object> { {"person", person} });

      if (lockObject.IsAcquired)
      {
        // We got a lock.
        Console.WriteLine($"{person} begin eat food.");

        Thread.Sleep(1000);

        // Try to release the lock.
        if (_random.NextDouble() < 0.8)
        {
          lockObject.Release();

          Console.WriteLine($"{person} released the lock.");
        }
        else Console.WriteLine($"{person} did not release lock.");
      }
      else Console.WriteLine($"{person} did not get food.");
    }
  }
}
