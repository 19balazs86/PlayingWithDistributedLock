using System;
using System.Threading.Tasks;

namespace PlayingWithDistributedLock
{
  public interface ILockObject : IDisposable
  {
    /// <summary>
    /// Did I get a lock or not?
    /// </summary>
    bool IsAcquired { get; }

    /// <summary>
    /// Release the lock, if our one is still exists.
    /// </summary>
    /// <returns>Return true, if we own the existing lock. Otherwise false.</returns>
    bool Release();

    Task<bool> ReleaseAsync();
  }
}
