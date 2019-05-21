using System;

namespace PlayingWithDistributedLock
{
  public interface ILockFactory
  {
    ILockObject AcquireLock(string key, TimeSpan expiration, int retryCount = 0, TimeSpan sleepDuration = default);
  }
}
