﻿using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace PlayingWithDistributedLock.Implementation;

public sealed class RedisDistLockFactory : ILockFactory
{
    #region Fields
    // Use Lua script to execute GET and DEL command at a time.
    // https://redis.io/commands/eval
    private const string _RELEASE_LOCK_SCRIPT = @"
        if (redis.call('GET', KEYS[1]) == ARGV[1])
        then
        redis.call('DEL', KEYS[1]);
        return true;
        else
        return false;
        end";

    private readonly Lazy<IDatabase> _lazyDatabase;

    private IDatabase _database => _lazyDatabase.Value;
    #endregion

    public RedisDistLockFactory(string connString = "127.0.0.1:6379")
    {
        _lazyDatabase = new Lazy<IDatabase>(() => ConnectionMultiplexer.Connect(connString).GetDatabase());
    }

    public ILockObject AcquireLock(string key, TimeSpan expiration, int retryCount = 0, TimeSpan sleepDuration = default)
    {
        validateFields(key, expiration, retryCount);

        Policy<bool> waitAndRetryPolicy = Policy
            .HandleResult<bool>(x => x == false)
            .WaitAndRetry(retryCount, _ => sleepDuration);

        string value = Guid.NewGuid().ToString();

        bool isSuccess;

        try
        {
            isSuccess = waitAndRetryPolicy.Execute(() => _database.LockTake(key, value, expiration));

            //isSuccess = waitAndRetryPolicy.Execute(() => _database.StringSet(key, value, expiration, When.NotExists));
        }
        catch (Exception ex)
        {
            throw new LockFactoryException($"Failed to set the key('{key}') to acquiring a lock.", ex);
        }

        return isSuccess ? new LockObject(this, key, value) : new LockObject();
    }

    public async Task<ILockObject> AcquireLockAsync(
        string key,
        TimeSpan expiration,
        int retryCount = 0,
        TimeSpan sleepDuration = default,
        CancellationToken cancelToken = default)
    {
        validateFields(key, expiration, retryCount);

        AsyncRetryPolicy<bool> waitAndRetryPolicy = Policy
            .HandleResult<bool>(x => x == false)
            .WaitAndRetryAsync(retryCount, _ => sleepDuration);

        string value = Guid.NewGuid().ToString();

        bool isSuccess;

        try
        {
            isSuccess = await waitAndRetryPolicy.ExecuteAsync(_ => _database.LockTakeAsync(key, value, expiration), cancelToken);

            //isSuccess = await waitAndRetryPolicy.ExecuteAsync(
            //  _ => _database.StringSetAsync(key, value, expiration, When.NotExists), cancelToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new LockFactoryException($"Failed to set the key('{key}') to acquiring a lock.", ex);
        }

        return isSuccess ? new LockObject(this, key, value) : new LockObject();
    }

    // This is not a proper release. We can release a lock, which is using by someone else.
    //private bool releaseLock(string key)
    //{
    //  _database.KeyDelete(key);

    //  return true;
    //}

    private bool releaseLock(string key, string value)
    {
        try
        {
            return _database.LockRelease(key, value);

            //return (bool) _database.ScriptEvaluate(_RELEASE_LOCK_SCRIPT, new RedisKey[] { key }, new RedisValue[] { value });
        }
        catch (Exception ex)
        {
            throw new LockFactoryException($"Failed to delete the key('{key}') to release the lock.", ex);
        }
    }

    private async Task<bool> releaseLockAsync(string key, string value)
    {
        try
        {
            return await _database.LockReleaseAsync(key, value);

            //return (bool) await _database.ScriptEvaluateAsync(_RELEASE_LOCK_SCRIPT, new RedisKey[] { key }, new RedisValue[] { value });
        }
        catch (Exception ex)
        {
            throw new LockFactoryException($"Failed to delete the key('{key}') to release the lock.", ex);
        }
    }

    private static void validateFields(string key, TimeSpan expiration, int retryCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiration, TimeSpan.Zero, nameof(expiration));

        ArgumentOutOfRangeException.ThrowIfLessThan(retryCount, 0, nameof(retryCount));
    }

    /// <summary>
    /// LockObject (internal)
    /// </summary>
    internal class LockObject : ILockObject
    {
        public bool IsAcquired => _lockFactory is not null;

        private RedisDistLockFactory _lockFactory;

        private readonly KeyValuePair<string, string> _keyValue;

        internal LockObject() { /* We did not get a lock in this case. */ }

        internal LockObject(RedisDistLockFactory lockFactory, string key, string value)
        {
            _lockFactory = lockFactory;
            _keyValue = KeyValuePair.Create(key, value);
        }

        public bool Release()
        {
            if (!IsAcquired) return false;

            try
            {
                return _lockFactory.releaseLock(_keyValue.Key, _keyValue.Value);
            }
            finally
            {
                _lockFactory = null; // No need to release it more times.
            }
        }

        public async Task<bool> ReleaseAsync()
        {
            if (!IsAcquired) return false;

            try
            {
                return await _lockFactory.releaseLockAsync(_keyValue.Key, _keyValue.Value);
            }
            finally
            {
                _lockFactory = null; // No need to release it more times.
            }
        }

        public void Dispose()
        {
            try
            {
                Release();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
