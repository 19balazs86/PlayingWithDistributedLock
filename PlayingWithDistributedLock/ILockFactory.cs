﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlayingWithDistributedLock
{
  public interface ILockFactory
  {
    ILockObject AcquireLock(string key, TimeSpan expiration, int retryCount = 0, TimeSpan sleepDuration = default);
    Task<ILockObject> AcquireLockAsync(string key, TimeSpan expiration, int retryCount = 0, TimeSpan sleepDuration = default, CancellationToken cancelToken = default);
  }
}
