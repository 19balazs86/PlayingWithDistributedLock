﻿using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using System.Net;

namespace PlayingWithDistributedLock
{
    public class RedLockEating : IDisposable
    {
        private readonly TimeSpan _expiryTime = TimeSpan.FromSeconds(2);
        private readonly TimeSpan _waitTime   = TimeSpan.FromSeconds(15);
        private readonly TimeSpan _retryTime  = TimeSpan.FromSeconds(1);

        private const string _lockKey = "RedLockEating";

        private readonly RedLockFactory _redLockFactory;

        public RedLockEating()
        {
            var redisEndpoints = new List<RedLockEndPoint> { new DnsEndPoint("localhost", 6379) };

            _redLockFactory = RedLockFactory.Create(redisEndpoints);
        }

        public async Task Start()
        {
            await Parallel.ForEachAsync(Enumerable.Range(1, 9), (personId, ct) => personEat(personId));
        }

        private async ValueTask personEat(int personId)
        {
            string person = $"Person({personId})";

            using IRedLock lockObject = await _redLockFactory.CreateLockAsync(_lockKey, _expiryTime, _waitTime, _retryTime);

            if (lockObject.IsAcquired)
            {
                // Note: _expiryTime is 2 seconds. It will be extended automatically if needed.
                int millisecondsDelay = Random.Shared.Next(1_000, 3_000);

                Console.WriteLine($"{person} begin to eat and wait milliseconds ({millisecondsDelay:N0}).");

                await Task.Delay(millisecondsDelay);

                Console.WriteLine($"{person} is done.");
            }
            else Console.WriteLine($"{person} did not get any food.");
        }

        public void Dispose() => _redLockFactory.Dispose();
    }
}
