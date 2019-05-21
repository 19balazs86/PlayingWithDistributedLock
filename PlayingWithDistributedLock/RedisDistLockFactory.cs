using System;
using System.Collections.Generic;
using Polly;
using StackExchange.Redis;

namespace PlayingWithDistributedLock
{
  public class RedisDistLockFactory : ILockFactory
  {
    private readonly Lazy<IDatabase> _lazyDatabase;

    private IDatabase _database => _lazyDatabase.Value;

    public RedisDistLockFactory(string connString = "localhost:6379")
    {
      _lazyDatabase = new Lazy<IDatabase>(()
        => ConnectionMultiplexer.Connect(connString).GetDatabase());
    }

    public ILockObject AcquireLock(string key, TimeSpan expiration, int retryCount = 0, TimeSpan sleepDuration = default)
    {
      if (string.IsNullOrWhiteSpace(key))
        throw new ArgumentException("Key can not be null or empty.");

      if (expiration <= TimeSpan.Zero)
        throw new ArgumentOutOfRangeException(nameof(expiration), "Value must be greater than zero.");

      if (retryCount < 0)
        throw new ArgumentOutOfRangeException(nameof(retryCount), "Value must be greater than or equal to zero.");

      Policy<bool> waitAndRetryPolicy = Policy
        .HandleResult<bool>(x => x == false)
        .WaitAndRetry(retryCount, _ => sleepDuration);

      string value = Guid.NewGuid().ToString();

      bool isSuccess;

      try
      {
        isSuccess = waitAndRetryPolicy.Execute(() => _database.StringSet(key, value, expiration, When.NotExists));
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
      // Use Lua script to execute GET and DEL command at a time.
      // https://redis.io/commands/eval
      string script = @"
        if (redis.call('GET', KEYS[1]) == ARGV[1])
        then
          redis.call('DEL', KEYS[1]);
          return true;
        else
          return false;
        end";

      try
      {
        return (bool) _database.ScriptEvaluate(script, new RedisKey[] { key }, new RedisValue[] { value });
      }
      catch (Exception ex)
      {
        throw new LockFactoryException($"Failed to delete the key('{key}') to release the lock.", ex);
      }
    }

    /// <summary>
    /// LockObject (internal)
    /// </summary>
    internal class LockObject : ILockObject
    {
      public bool IsAcquired => _lockFactory != null;

      private RedisDistLockFactory _lockFactory;
      private readonly KeyValuePair<string, string> _keyValue;

      internal LockObject() { /* We did not get a lock in this case. */ }

      internal LockObject(RedisDistLockFactory lockFactory, string key, string value)
      {
        _lockFactory = lockFactory;
        _keyValue    = KeyValuePair.Create(key, value);
      }

      public bool Release()
      {
        if (!IsAcquired) return false;

        try
        {
          return _lockFactory.releaseLock(_keyValue.Key, _keyValue.Value);
        }
        catch (Exception ex)
        {
          throw new LockFactoryException($"Failed to release the lock for the key('{_keyValue.Key}').", ex);
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
          // Do log.
          Console.WriteLine(ex.Message);
        }
      }
    }
  }
}
