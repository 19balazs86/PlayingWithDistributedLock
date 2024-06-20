using PlayingWithDistributedLock.Implementation;
using Polly;
using Polly.Retry;

namespace PlayingWithDistributedLock.EatingExemple;

public sealed class EatingWithRedisDistLock
{
    private const string _lockKey = "lock:eat";

    private readonly ILockFactory _lockFactory;

    private static readonly ResiliencePropertyKey<string> _personResiliencePropertyKey = new ResiliencePropertyKey<string>("person");

    private static readonly ResiliencePipeline<ILockObject> _resiliencePipeline = buildResiliencePipeline();

    private EatingWithRedisDistLock(ILockFactory lockFactory)
    {
        _lockFactory = lockFactory;
    }

    public static EatingWithRedisDistLock Create(ILockFactory lockFactory)
    {
        return new EatingWithRedisDistLock(lockFactory);
    }

    public async Task Start()
    {
        await Parallel.ForEachAsync(Enumerable.Range(1, 4), (personId, ct) => personEat(personId));
    }

    private async ValueTask personEat(int personId)
    {
        string person = $"Person({personId})";

        ResilienceContext context = getResilienceContext(person);

        // Try to acquire a lock
        ILockObject lockObject = await _resiliencePipeline.ExecuteAsync(async ctx =>
        {
            // AcquireLock has a built-in retry. This is just to try out the ResiliencePipeline
            return await _lockFactory.AcquireLockAsync(_lockKey, TimeSpan.FromSeconds(5));
        }, context);

        if (!lockObject.IsAcquired)
        {
            Console.WriteLine($"{person} did not get any food.");
            return;
        }

        // We got a lock
        Console.WriteLine($"{person} begin to eat.");

        await Task.Delay(1_000);

        // Try to release the lock.
        if (Random.Shared.NextDouble() < 0.8)
        {
            await lockObject.ReleaseAsync();

            Console.WriteLine($"{person} released the lock.");
        }
        else Console.WriteLine($"{person} did not release lock.");
    }

    private static ResilienceContext getResilienceContext(string person)
    {
        ResilienceContext context = ResilienceContextPool.Shared.Get();

        context.Properties.Set(_personResiliencePropertyKey, person);

        return context;
    }

    private static ResiliencePipeline<ILockObject> buildResiliencePipeline()
    {
        var retryOptions = new RetryStrategyOptions<ILockObject>
        {
            ShouldHandle = new PredicateBuilder<ILockObject>().HandleResult(lockObject => lockObject.IsAcquired is false),
            Delay = TimeSpan.FromSeconds(1),
            MaxRetryAttempts = 4, // 1 + 4 times retry = 5
            BackoffType = DelayBackoffType.Constant,
            UseJitter = true, // Random value between -25% and +25% of the calculated Delay
            OnRetry = arg =>
            {
                string person = arg.Context.Properties.GetValue(_personResiliencePropertyKey, "no-person");

                Console.WriteLine($"{person} is waiting {arg.RetryDelay.TotalMilliseconds:N0} ms for retry.");

                return ValueTask.CompletedTask;
            }
        };

        return new ResiliencePipelineBuilder<ILockObject>()
            .AddRetry(retryOptions)
            .Build();
    }
}
