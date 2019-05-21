using System;
using System.Collections.Generic;
using StackExchange.Redis;

namespace PlayingWithDistributedLock
{
  public class RedisDistLockFactory : ILockFactory
  {
    private readonly Lazy<IDatabase> _lazyDatabase = new Lazy<IDatabase>(()
      => ConnectionMultiplexer.Connect("localhost:6379").GetDatabase());

    private IDatabase _database => _lazyDatabase.Value;

    /// <summary>
    /// Acquire a lock object with the given key for a given time.
    /// </summary>
    /// <param name="key">Key to lock.</param>
    /// <param name="expiration">Expiration time.</param>
    /// <returns>Return an object either the lock is acquired or not.</returns>
    /// <exception cref="LockFactoryException"></exception>
    public ILockObject AcquireLock(string key, TimeSpan expiration)
    {
      if (expiration <= TimeSpan.Zero)
        throw new ArgumentOutOfRangeException("The expiration has to be bigger than zero.");

      string value = Guid.NewGuid().ToString();

      bool isSuccess;

      try
      {
        isSuccess = _database.StringSet(key, value, expiration, When.NotExists);
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
