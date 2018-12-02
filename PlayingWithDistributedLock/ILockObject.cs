using System;

namespace PlayingWithDistributedLock
{
  public interface ILockObject : IDisposable
  {
    bool IsAcquired { get; }
  }
}
