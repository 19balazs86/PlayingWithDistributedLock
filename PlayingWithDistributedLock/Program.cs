using System;
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
    public static void Main(string[] args)
    {
      using (ILockObject lockObject = _lockFactory.AcquireLock(_lockKey))
        Console.WriteLine($"Did I get a lock? -> {lockObject.IsAcquired}");

      // The dinner is done.
      Parallel.For(0, 5, x => personEat(x));

      Console.WriteLine("End of the dinner.");
    }

    private static void personEat(int personId)
    {
      string person = $"Person({personId})";
      
      // Try to acquire a lock maximum 5 times.
      ILockObject lockObject = _waitAndRetryPolicy
        .Execute(
          ctx => _lockFactory.AcquireLock(_lockKey),
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
