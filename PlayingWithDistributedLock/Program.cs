using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlayingWithDistributedLock
{
  public class Program
  {
    private static readonly Random _random = new Random();

    private const string _lockKey = "lock:eat";

    private static readonly ILockFactory _lockFactory =
      new ExceptionToleranceLockFactory(new RedisDistLockFactory());


    public static void Main(string[] args)
    {
      using (ILockObject lockObject = _lockFactory.AcquireLock(_lockKey))
      {
        Console.WriteLine($"Did I get a lock? -> {lockObject.IsAcquired}");
      }

      // The dinner is ready.
      Parallel.For(0, 5, x => personEat(x));

      Console.WriteLine("End of the dinner.");
    }

    private static void personEat(int personId)
    {
      string person = $"Person({personId})";

      int retryCount = 0;

      // Try to acquire a lock.
      ILockObject lockObject = _lockFactory.AcquireLock(_lockKey);

      // Retry to acquire a lock.
      while (!lockObject.IsAcquired && ++retryCount <= 5)
      {
        Console.WriteLine($"{person} is waiting.");

        Thread.Sleep(_random.Next(1200, 1500));

        lockObject = _lockFactory.AcquireLock(_lockKey);
      }

      if (lockObject.IsAcquired)
      {
        // Here we got a lock.
        Console.WriteLine($"{person} begin eat food.");

        Thread.Sleep(1000);

        // Try to release the lock.
        if (_random.NextDouble() < 0.8)
        {
          lockObject.Dispose();

          Console.WriteLine($"{person} released the lock.");
        }
        else Console.WriteLine($"{person} did not release lock.");
      }
      else Console.WriteLine($"{person} did not get food.");
    }
  }
}
