using System;

namespace PlayingWithDistributedLock
{
  public interface ILockFactory
  {
    ILockObject AcquireLock(string key, TimeSpan expiration);
  }
}
