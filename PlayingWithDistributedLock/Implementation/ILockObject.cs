namespace PlayingWithDistributedLock.Implementation;

public interface ILockObject : IDisposable
{
    /// <summary>
    /// Did I get a lock or not?
    /// </summary>
    bool IsAcquired { get; }

    /// <summary>
    /// Release the lock, if it still exists and returns true, otherwise false.
    /// </summary>
    bool Release();

    Task<bool> ReleaseAsync();
}
