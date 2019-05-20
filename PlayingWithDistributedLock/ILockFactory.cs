using System;

namespace PlayingWithDistributedLock
{
  public interface ILockFactory
  {
    ILockObject AcquireLock(string key, TimeSpan expiration);

    ILockObject AcquireLock(string key); // In C# 8, you can inplement default interface method.

    //bool ReleaseLock(string key, string value);
    //bool ReleaseLock(string key);
  }
}
