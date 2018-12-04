using System;

namespace PlayingWithDistributedLock
{
  public interface ILockFactory
  {
    /// <summary>
    /// Acquire a lock object with the given key for the given time.
    /// </summary>
    /// <param name="key">Key to lock.</param>
    /// <param name="expiration">Expiration time.</param>
    /// <returns>Return an object either the lock is acquired or not.</returns>
    /// <exception cref="LockFactoryException"></exception>
    ILockObject AcquireLock(string key, TimeSpan expiration);

    /// <summary>
    /// Acquire a lock object with the given key.
    /// </summary>
    /// <param name="key">Key to lock.</param>
    /// <returns>Return an object either the lock is acquired or not.</returns>
    /// <exception cref="LockFactoryException"></exception>
    ILockObject AcquireLock(string key); // In C# 8, you can inplement default interface method.

    //bool ReleaseLock(string key, string value);
    //bool ReleaseLock(string key);
  }
}
