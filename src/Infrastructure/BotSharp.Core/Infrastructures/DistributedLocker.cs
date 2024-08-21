using Medallion.Threading.Redis;
using StackExchange.Redis;

namespace BotSharp.Core.Infrastructures;

public class DistributedLocker
{
    private readonly BotSharpDatabaseSettings _settings;
    private ConnectionMultiplexer connection;

    public DistributedLocker(BotSharpDatabaseSettings settings)
    {
        _settings = settings;
    }

    public async Task<T> Lock<T>(string resource, Func<Task<T>> action, int timeoutInSeconds = 30)
    {
        var timeout = TimeSpan.FromSeconds(timeoutInSeconds);

        if (connection == null)
        {
            connection = await ConnectionMultiplexer.ConnectAsync(_settings.Redis);
        }
        
        var @lock = new RedisDistributedLock(resource, connection.GetDatabase());
        await using (var handle = await @lock.TryAcquireAsync(timeout))
        {
            if (handle == null) 
            {
                Serilog.Log.Logger.Error($"Acquire lock for {resource} failed due to after {timeout}s timeout.");
            }
            
            return await action();
        }
    }

    public async Task<T> Lock<T>(string resource, Func<T> action, int timeoutInSeconds = 30)
    {
        var timeout = TimeSpan.FromSeconds(timeoutInSeconds);

        if (connection == null)
        {
            connection = await ConnectionMultiplexer.ConnectAsync(_settings.Redis);
        }

        var @lock = new RedisDistributedLock(resource, connection.GetDatabase());
        await using (var handle = await @lock.TryAcquireAsync(timeout))
        {
            if (handle == null)
            {
                Serilog.Log.Logger.Error($"Acquire lock for {resource} failed due to after {timeout}s timeout.");
                
            }
            return action();
        }
    }
}
