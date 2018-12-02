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

    public ILockObject AcquireLock(string key, TimeSpan expiration)
    {
      string guid = Guid.NewGuid().ToString();

      bool isSuccess;

      try
      {
        isSuccess = _database.StringSet(key, guid, expiration, When.NotExists);
      }
      catch (Exception ex)
      {
        throw new LockFactoryException("Failed to acquire a lock.", ex);
      }

      return isSuccess ? new LockObject(this, KeyValuePair.Create(key, guid)) : new LockObject();
    }

    public ILockObject AcquireLock(string key)
    {
      return AcquireLock(key, TimeSpan.FromSeconds(5));
    }

    private bool releaseLock(string key)
    {
      try
      {
        _database.KeyDelete(key);

        return true;
      }
      catch (Exception ex)
      {
        throw new LockFactoryException("Failed to release the lock.", ex);
      }
    }

    /// <summary>
    /// We do not need this in our case. LockObject responsible to release the lock, which is internally.
    /// </summary>
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

      RedisResult result;

      try
      {
        result = _database.ScriptEvaluate(script, new RedisKey[] { key }, new RedisValue[] { value });
      }
      catch (Exception ex)
      {
        throw new LockFactoryException("Failed to release the lock.", ex);
      }

      return (bool) result;
    }

    /// <summary>
    /// LockObject (internal)
    /// </summary>
    internal class LockObject : ILockObject
    {
      public bool IsAcquired { get; internal set; }

      private readonly RedisDistLockFactory _lockFactory;
      private readonly KeyValuePair<string, string> _keyValue;

      internal LockObject()
      {
        IsAcquired = false;
      }

      internal LockObject(RedisDistLockFactory lockFactory, KeyValuePair<string, string> keyValue)
      {
        _lockFactory = lockFactory;
        _keyValue    = keyValue;
        IsAcquired   = true;
      }

      public void Dispose()
      {
        if (!IsAcquired) return;

        //_lockFactory.releaseLock(_keyValue.Key, _keyValue.Value);
        _lockFactory.releaseLock(_keyValue.Key);
      }
    }
  }
}
